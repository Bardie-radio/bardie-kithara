using System.Security.Cryptography.X509Certificates;
using Bardie.Modules.V1;
using Grpc.Core;
using Grpc.Net.Client;

// Smoke client for Module Registry Register (auto) + mTLS Heartbeat.
// Env:
//   KITHARA_GRPC_ADDRESS (default https://localhost:5000)
//   MODULE_SLUG (default dummy)
//   JOIN_SECRET (default dummy-join-secret)

var address = Environment.GetEnvironmentVariable("KITHARA_GRPC_ADDRESS") ?? "https://localhost:5000";
var slug = Environment.GetEnvironmentVariable("MODULE_SLUG") ?? "dummy";
var joinSecret = Environment.GetEnvironmentVariable("JOIN_SECRET") ?? "dummy-join-secret";

Console.WriteLine($"DummyRegistrar → {address} as {slug}");

using var insecureHandler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = static (_, _, _, _) => true,
};

using var registerChannel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
{
    HttpHandler = insecureHandler,
});

var registerClient = new ModuleRegistry.ModuleRegistryClient(registerChannel);
RegisterResponse registerResponse;
try
{
    registerResponse = await registerClient.RegisterAsync(new RegisterRequest
    {
        Slug = slug,
        JoinSecret = joinSecret,
        Kind = "client",
        GrpcAdvertiseAddress = "dns:///dummy:5001",
        Client = new ClientRegisterDetails { AuthMode = "static" },
    });
}
catch (RpcException ex)
{
    Console.Error.WriteLine($"Register failed: {ex.Status}");
    return 1;
}

Console.WriteLine($"Register ok; CA thumbprint={registerResponse.CaThumbprint}");
if (string.IsNullOrWhiteSpace(registerResponse.ClientPrivateKeyPem)
    || string.IsNullOrWhiteSpace(registerResponse.ClientCertificatePem))
{
    Console.Error.WriteLine("Expected auto-mode PEMs on RegisterResponse; got empty key/cert.");
    return 2;
}

using var clientCert = X509Certificate2.CreateFromPem(
    registerResponse.ClientCertificatePem,
    registerResponse.ClientPrivateKeyPem);
using var caCert = X509Certificate2.CreateFromPem(registerResponse.CaCertificatePem);

using var mtlsHandler = new HttpClientHandler();
mtlsHandler.ClientCertificates.Add(clientCert);
mtlsHandler.ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
{
    if (cert is null)
    {
        return false;
    }

    using var presented = new X509Certificate2(cert);
    using var chain = new X509Chain();
    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    chain.ChainPolicy.CustomTrustStore.Add(caCert);
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    return chain.Build(presented);
};

using var heartbeatChannel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
{
    HttpHandler = mtlsHandler,
});
var heartbeatClient = new ModuleRegistry.ModuleRegistryClient(heartbeatChannel);

try
{
    var heartbeat = await heartbeatClient.HeartbeatAsync(new HeartbeatRequest { Slug = slug });
    Console.WriteLine($"Heartbeat ok={heartbeat.Ok}; next in {heartbeat.NextHeartbeatAfterSeconds}s");
}
catch (RpcException ex)
{
    Console.Error.WriteLine($"Heartbeat failed: {ex.Status}");
    return 3;
}

// Bare (no client cert) heartbeat must fail.
try
{
    await registerClient.HeartbeatAsync(new HeartbeatRequest { Slug = slug });
    Console.Error.WriteLine("Bare Heartbeat unexpectedly succeeded.");
    return 4;
}
catch (RpcException ex) when (ex.StatusCode is StatusCode.Unauthenticated or StatusCode.Unavailable)
{
    Console.WriteLine($"Bare Heartbeat correctly denied: {ex.StatusCode}");
}

Console.WriteLine("DummyRegistrar smoke passed.");
return 0;
