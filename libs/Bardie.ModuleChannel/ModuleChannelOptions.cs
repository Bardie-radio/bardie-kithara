namespace Bardie.ModuleChannel;

/// <summary>
/// Options for Bardie module-mesh mTLS. Defaults favour private Compose/LAN (<see cref="ModuleChannelBootstrapMode.Auto"/>, <see cref="UseMtls"/> = true).
/// </summary>
public sealed class ModuleChannelOptions
{
    public const string SectionName = "ModuleChannel";

    /// <summary>When true (default), server and outbound helpers expect mTLS.</summary>
    public bool UseMtls { get; set; } = true;

    /// <summary>Env: <c>BARDIE_MODULE_MTLS_BOOTSTRAP</c> — <c>auto</c> | <c>preshared</c>.</summary>
    public ModuleChannelBootstrapMode BootstrapMode { get; set; } = ModuleChannelBootstrapMode.Auto;

    /// <summary>Env: <c>BARDIE_GRPC_TLS_DATA_PATH</c> — host CA / server cert directory.</summary>
    public string TlsDataPath { get; set; } = "data/mtls";

    /// <summary>Env: <c>BARDIE_MODULE_MTLS_PRESHARED_DIR</c> — per-slug client certs when bootstrap is <see cref="ModuleChannelBootstrapMode.Preshared"/>.</summary>
    public string? PresharedDir { get; set; }

    /// <summary>Lifetime for auto-issued module client certificates.</summary>
    public TimeSpan ClientCertificateLifetime { get; set; } = TimeSpan.FromDays(365);

    /// <summary>DNS names embedded in the host server certificate SAN.</summary>
    public string[] ServerDnsNames { get; set; } = ["kithara", "localhost"];
}
