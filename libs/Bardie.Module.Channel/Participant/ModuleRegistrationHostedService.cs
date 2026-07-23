using Bardie.Module.Channel.Manifest;
using Bardie.Modules.V1;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bardie.Module.Channel.Participant;

/// <summary>
/// Registers with the host from the module manifest, persists mesh certs, then heartbeats on mTLS.
/// </summary>
public sealed class ModuleRegistrationHostedService : BackgroundService
{
    private readonly ModuleManifest _manifest;
    private readonly ModuleParticipantOptions _options;
    private readonly IModuleParticipantCertificateStore _certificateStore;
    private readonly IModuleParticipantChannelFactory _channelFactory;
    private readonly IReadOnlyList<IModuleRegisterRequestCustomizer> _customizers;
    private readonly ILogger<ModuleRegistrationHostedService> _logger;

    public ModuleRegistrationHostedService(
        ModuleManifest manifest,
        IOptions<ModuleParticipantOptions> options,
        IModuleParticipantCertificateStore certificateStore,
        IModuleParticipantChannelFactory channelFactory,
        ILogger<ModuleRegistrationHostedService> logger,
        IEnumerable<IModuleRegisterRequestCustomizer> customizers)
    {
        _manifest = manifest;
        _options = options.Value;
        _certificateStore = certificateStore;
        _channelFactory = channelFactory;
        _customizers = customizers.ToArray();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableRegistration)
        {
            _logger.LogInformation("Module registration hosted service disabled.");
            return;
        }

        ValidateOptions();

        await _certificateStore.EnsureServerCertificateAsync(
            _options.ServerDnsNames,
            stoppingToken).ConfigureAwait(false);

        var registered = false;
        while (!registered && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RegisterAsync(stoppingToken).ConfigureAwait(false);
                registered = true;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Register failed for {Slug}; retrying in 3s.", _manifest.Slug);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        if (!registered)
        {
            return;
        }

        var nextDelay = TimeSpan.FromSeconds(30);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(nextDelay, stoppingToken).ConfigureAwait(false);
                nextDelay = await HeartbeatAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Heartbeat failed for module {Slug}; retrying.", _manifest.Slug);
                nextDelay = TimeSpan.FromSeconds(5);
            }
        }
    }

    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var request = _manifest.BuildRegisterRequest(
            _options.JoinSecret,
            _options.GrpcAdvertiseAddress,
            _customizers);

        _logger.LogInformation(
            "Registering module {Slug} ({Kind}) with host at {Address}",
            request.Slug,
            request.Kind,
            _options.HostGrpcAddress);

        using var channel = _channelFactory.CreateBootstrapChannel(_options.HostGrpcAddress);
        var client = new ModuleRegistry.ModuleRegistryClient(channel);

        RegisterResponse response;
        try
        {
            response = await client.RegisterAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Register failed for module {Slug}: {Status}", request.Slug, ex.Status);
            throw;
        }

        await _certificateStore.ApplyRegisterResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Registered module {Slug}; CA thumbprint={Thumbprint}",
            request.Slug,
            response.CaThumbprint);
    }

    private async Task<TimeSpan> HeartbeatAsync(CancellationToken cancellationToken)
    {
        using var channel = _channelFactory.CreateMtlsChannel(_options.HostGrpcAddress);
        var client = new ModuleRegistry.ModuleRegistryClient(channel);
        var response = await client.HeartbeatAsync(
                new HeartbeatRequest { Slug = _manifest.Slug },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var seconds = response.NextHeartbeatAfterSeconds > 0
            ? response.NextHeartbeatAfterSeconds
            : 30;
        _logger.LogDebug(
            "Heartbeat ok={Ok} for {Slug}; next in {Seconds}s",
            response.Ok,
            _manifest.Slug,
            seconds);
        return TimeSpan.FromSeconds(seconds);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.HostGrpcAddress))
        {
            throw new InvalidOperationException(
                "ModuleParticipant:HostGrpcAddress / MODULE_HOST_GRPC_ADDRESS is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.JoinSecret))
        {
            throw new InvalidOperationException("ModuleParticipant:JoinSecret / JOIN_SECRET is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.GrpcAdvertiseAddress))
        {
            throw new InvalidOperationException(
                "ModuleParticipant:GrpcAdvertiseAddress / GRPC_ADVERTISE_ADDRESS is required.");
        }
    }
}
