namespace Bardie.Module.Channel;

/// <summary>
/// How module client certificates appear on disk / on the Register wire.
/// </summary>
public enum ModuleChannelBootstrapMode
{
    /// <summary>
    /// Private mesh default: host issues a client cert and may return PEMs (including the private key) on Register.
    /// Not for public / untrusted networks.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Operator pre-places CA + module client cert/key offline. Register never returns private keys.
    /// </summary>
    Preshared = 1,
}
