using Bardie.Auth.Orchestrator.Catalog;
using Bardie.ModuleChannel;
using Bardie.ModuleChannel.Certificates;
using Bardie.ModuleChannel.Hosting;
using Bardie.Modules.V1;
using Bardie.Source.Orchestrator.Catalog;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Kithara.Features.Modules;

public sealed class ModuleRegistryService : ModuleRegistry.ModuleRegistryBase
{
    private readonly InMemoryModuleRegistry _registry;
    private readonly IAuthModuleCatalog _authCatalog;
    private readonly ISourceModuleCatalog _sourceCatalog;
    private readonly IModuleCertificateStore _certificateStore;
    private readonly IModuleCertificateIssuer _certificateIssuer;
    private readonly ModuleRegistryOptions _registryOptions;
    private readonly ModuleChannelOptions _channelOptions;
    private readonly ILogger<ModuleRegistryService> _logger;

    public ModuleRegistryService(
        InMemoryModuleRegistry registry,
        IAuthModuleCatalog authCatalog,
        ISourceModuleCatalog sourceCatalog,
        IModuleCertificateStore certificateStore,
        IModuleCertificateIssuer certificateIssuer,
        IOptions<ModuleRegistryOptions> registryOptions,
        IOptions<ModuleChannelOptions> channelOptions,
        ILogger<ModuleRegistryService> logger)
    {
        _registry = registry;
        _authCatalog = authCatalog;
        _sourceCatalog = sourceCatalog;
        _certificateStore = certificateStore;
        _certificateIssuer = certificateIssuer;
        _registryOptions = registryOptions.Value;
        _channelOptions = channelOptions.Value;
        _logger = logger;
    }

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        var slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "slug is required."));
        }

        var kind = WellKnownModuleKinds.Normalize(request.Kind);
        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "kind is required."));
        }

        if (!JoinSecretsConfiguration.Validate(_registryOptions.JoinSecrets, slug, request.JoinSecret ?? string.Empty))
        {
            _logger.LogWarning("Register denied for slug {Slug}: join secret mismatch", slug);
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid join secret."));
        }

        if (string.IsNullOrWhiteSpace(request.GrpcAdvertiseAddress))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "grpc_advertise_address is required."));
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(_registryOptions.HeartbeatTtl);
        var response = new RegisterResponse
        {
            CaThumbprint = _certificateStore.IsLoaded ? _certificateStore.CaThumbprint : string.Empty,
        };

        switch (_channelOptions.BootstrapMode)
        {
            case ModuleChannelBootstrapMode.Auto:
            {
                var issued = _certificateIssuer.IssueClientCertificate(slug);
                response.ClientCertificatePem = issued.ClientCertificatePem;
                response.ClientPrivateKeyPem = issued.ClientPrivateKeyPem;
                response.CaCertificatePem = issued.CaCertificatePem;
                response.CaThumbprint = issued.CaThumbprint;
                response.CertificateExpiresUnix = issued.ExpiresAt.ToUnixTimeSeconds();
                break;
            }
            case ModuleChannelBootstrapMode.Preshared:
            {
                if (!_certificateStore.TryGetPresharedClientExpiry(slug, out var expires))
                {
                    _logger.LogWarning(
                        "Register denied for slug {Slug}: preshared client cert material missing",
                        slug);
                    throw new RpcException(new Status(
                        StatusCode.FailedPrecondition,
                        "Preshared client certificate material is missing for this slug."));
                }

                // Optional: if the module already presented a client cert, it must match the slug.
                if (context.UserState.TryGetValue(ModuleChannelBootstrapInterceptor.ModuleSlugUserStateKey, out var presented)
                    && presented is string presentedSlug
                    && !string.Equals(presentedSlug, slug, StringComparison.OrdinalIgnoreCase))
                {
                    throw new RpcException(new Status(
                        StatusCode.PermissionDenied,
                        "Presented client certificate slug does not match Register slug."));
                }

                response.ClientCertificatePem = string.Empty;
                response.ClientPrivateKeyPem = string.Empty;
                response.CaCertificatePem = _certificateStore.CaCertificatePem;
                response.CaThumbprint = _certificateStore.CaThumbprint;
                response.CertificateExpiresUnix = expires.ToUnixTimeSeconds();
                break;
            }
            default:
                throw new RpcException(new Status(StatusCode.Internal, "Unknown mTLS bootstrap mode."));
        }

        var capabilities = request.Capabilities.ToArray();
        _registry.Upsert(new ModuleRegistrationRecord
        {
            Slug = slug,
            Kind = kind,
            GrpcAdvertiseAddress = request.GrpcAdvertiseAddress,
            Capabilities = capabilities,
            RegisteredAt = now,
            LastHeartbeatAt = now,
            ExpiresAt = expiresAt,
        });

        ProjectToOrchestratorCatalogs(request, slug, kind, capabilities, now, expiresAt);

        _logger.LogInformation(
            "Module {Slug} ({Kind}) registered; bootstrap={Bootstrap}; well_known={WellKnown}",
            slug,
            kind,
            _channelOptions.BootstrapMode,
            WellKnownModuleKinds.IsWellKnown(kind));

        return Task.FromResult(response);
    }

    public override Task<HeartbeatResponse> Heartbeat(HeartbeatRequest request, ServerCallContext context)
    {
        var slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "slug is required."));
        }

        if (_channelOptions.UseMtls)
        {
            if (!context.UserState.TryGetValue(ModuleChannelBootstrapInterceptor.ModuleSlugUserStateKey, out var state)
                || state is not string certSlug
                || !string.Equals(certSlug, slug, StringComparison.OrdinalIgnoreCase))
            {
                throw new RpcException(new Status(
                    StatusCode.Unauthenticated,
                    "Heartbeat requires an mTLS client certificate matching the slug."));
            }
        }

        if (!_registry.TryGet(slug, out _))
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Module '{slug}' is not registered."));
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(_registryOptions.HeartbeatTtl);
        _registry.TouchHeartbeat(slug, now, expiresAt);
        _authCatalog.TouchHeartbeatSafe(slug, now, expiresAt);
        _sourceCatalog.TouchHeartbeatSafe(slug, now, expiresAt);

        return Task.FromResult(new HeartbeatResponse
        {
            Ok = true,
            NextHeartbeatAfterSeconds = _registryOptions.NextHeartbeatAfterSeconds,
        });
    }

    private void ProjectToOrchestratorCatalogs(
        RegisterRequest request,
        string slug,
        string kind,
        IReadOnlyList<string> capabilities,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        // Only Bardie well-known kinds project into orch catalogs. Any other kind stays registry-only
        // (ModuleChannel mTLS still applies). Hosts beyond Bardie can branch on custom kind strings here.
        switch (kind)
        {
            case WellKnownModuleKinds.Auth:
                _authCatalog.Upsert(new AuthModuleRegistration
                {
                    Slug = slug,
                    GrpcAdvertiseAddress = request.GrpcAdvertiseAddress,
                    Capabilities = capabilities,
                    JwksUri = request.Auth?.JwksUri,
                    JwksJson = request.Auth?.JwksJson,
                    RegisteredAt = now,
                    LastHeartbeatAt = now,
                    ExpiresAt = expiresAt,
                });
                break;

            case WellKnownModuleKinds.Source:
                var fields = request.Source?.SearchFields
                    .Select(f => new Bardie.Source.Orchestrator.Catalog.SearchFieldDescriptor
                    {
                        Name = f.Name,
                        Required = f.Required,
                    })
                    .ToArray()
                    ?? [];
                _sourceCatalog.Upsert(new SourceModuleRegistration
                {
                    Slug = slug,
                    GrpcAdvertiseAddress = request.GrpcAdvertiseAddress,
                    Capabilities = capabilities,
                    SearchFields = fields,
                    RegisteredAt = now,
                    LastHeartbeatAt = now,
                    ExpiresAt = expiresAt,
                });
                break;

            case WellKnownModuleKinds.Client:
                // Client stays registry-only in Phase 1.
                break;

            default:
                _logger.LogInformation(
                    "Module {Slug} registered with host-unknown kind {Kind}; registry only (no orch catalog)",
                    slug,
                    kind);
                break;
        }
    }
}

internal static class CatalogHeartbeatExtensions
{
    public static void TouchHeartbeatSafe(
        this IAuthModuleCatalog catalog,
        string slug,
        DateTimeOffset lastHeartbeatAt,
        DateTimeOffset expiresAt)
    {
        if (catalog.TryGet(slug, out _))
        {
            catalog.TouchHeartbeat(slug, lastHeartbeatAt, expiresAt);
        }
    }

    public static void TouchHeartbeatSafe(
        this ISourceModuleCatalog catalog,
        string slug,
        DateTimeOffset lastHeartbeatAt,
        DateTimeOffset expiresAt)
    {
        if (catalog.TryGet(slug, out _))
        {
            catalog.TouchHeartbeat(slug, lastHeartbeatAt, expiresAt);
        }
    }
}
