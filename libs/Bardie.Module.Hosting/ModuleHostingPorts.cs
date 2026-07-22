using Microsoft.Extensions.Configuration;

namespace Bardie.Module.Hosting;

/// <summary>Resolves module participant listen ports from Bardie / generic env aliases.</summary>
public static class ModuleHostingPorts
{
    public const int DefaultWorkGrpcPort = 5001;
    public const int DefaultHttpPort = 8080;

    public static int ResolveWorkPort(IConfiguration configuration)
    {
        var raw = configuration["BARDIE_WORK_GRPC_PORT"]
            ?? configuration["MODULE_WORK_GRPC_PORT"]
            ?? configuration["ModuleParticipant:WorkGrpcPort"];
        return int.TryParse(raw, out var port) && port > 0 ? port : DefaultWorkGrpcPort;
    }

    public static int ResolveHttpPort(IConfiguration configuration)
    {
        var raw = configuration["BARDIE_HTTP_PORT"]
            ?? configuration["MODULE_HTTP_PORT"]
            ?? configuration["ModuleParticipant:HttpPort"];
        return int.TryParse(raw, out var port) && port > 0 ? port : DefaultHttpPort;
    }
}
