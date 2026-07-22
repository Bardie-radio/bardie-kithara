using Bardie.Auth.V1;
using Bardie.Module.Channel.Manifest;
using Bardie.Modules.V1;
using Grpc.Core;
using Xunit;

namespace Bardie.Module.Auth.Tests;

public class AuthAdapterModuleBaseTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("bes", true)]
    [InlineData("BES", true)]
    [InlineData("other", false)]
    public void MatchesProviderId_honours_slug(string? providerId, bool expected)
    {
        var adapter = new StubAdapter(new ModuleManifest { Slug = "bes", Kind = "auth" });
        Assert.Equal(expected, adapter.PublicMatches(providerId));
    }

    [Fact]
    public void Denied_returns_bearer_not_allowed()
    {
        var denied = StubAdapter.PublicDenied();
        Assert.False(denied.Allowed);
        Assert.Equal("Bearer", denied.TokenType);
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var adapter = new StubAdapter(new ModuleManifest { Slug = "bes", Kind = "auth" });
        var response = await adapter.Health(new HealthRequest(), context: null!);
        Assert.True(response.Ok);
    }

    [Fact]
    public async Task SeedAdmin_default_is_unimplemented()
    {
        var adapter = new StubAdapter(new ModuleManifest { Slug = "bes", Kind = "auth" });
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => adapter.SeedAdmin(new SeedAdminRequest(), context: null!));
        Assert.Equal(StatusCode.Unimplemented, ex.StatusCode);
    }

    [Fact]
    public void AuthJwksRegisterRequestCustomizer_sets_auth_jwks()
    {
        var keyDir = Path.Combine(Path.GetTempPath(), "bardie-jwks-customizer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(keyDir);
        try
        {
            var options = Microsoft.Extensions.Options.Options.Create(new AuthModuleJwtOptions
            {
                SigningKeyPath = Path.Combine(keyDir, "jwt.pem"),
            });
            var manifest = new ModuleManifest { Slug = "bes", Kind = "auth", OtelServiceName = "bardie.auth.bes" };
            using var tokens = new AuthModuleJwtService(options, manifest);
            var customizer = new AuthJwksRegisterRequestCustomizer(tokens);
            var request = new RegisterRequest();

            customizer.Customize(request, manifest);

            Assert.Equal(RegisterRequest.DetailsOneofCase.Auth, request.DetailsCase);
            Assert.False(string.IsNullOrWhiteSpace(request.Auth.JwksJson));
            Assert.Contains("\"keys\"", request.Auth.JwksJson, StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                Directory.Delete(keyDir, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private sealed class StubAdapter : AuthAdapterModuleBase
    {
        public StubAdapter(ModuleManifest manifest)
            : base(manifest)
        {
        }

        public bool PublicMatches(string? providerId) => MatchesProviderId(providerId);

        public static AuthenticateResponse PublicDenied() => Denied();
    }
}
