using Bardie.Module.Channel.Manifest;
using Bardie.Source.V1;
using Grpc.Core;

namespace Bardie.Module.Source;

/// <summary>
/// Thin SourceModule base: health, default Pause/Resume when <c>pause</c> is absent, job-not-found helpers.
/// Concrete Search / StartTrack / StopTrack stay in the module.
/// </summary>
public abstract class SourceModuleBase : SourceModule.SourceModuleBase
{
    public const string PauseCapability = "pause";

    protected ModuleManifest Manifest { get; }

    protected SourceModuleBase(ModuleManifest manifest)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    public override Task<HealthResponse> Health(HealthRequest request, ServerCallContext context) =>
        Task.FromResult(new HealthResponse { Ok = true });

    public override Task<PauseTrackResponse> PauseTrack(PauseTrackRequest request, ServerCallContext context)
    {
        if (!HasCapability(PauseCapability))
        {
            throw new RpcException(new Status(
                StatusCode.FailedPrecondition,
                $"Module '{Manifest.Slug}' does not advertise '{PauseCapability}'."));
        }

        return PauseTrackCoreAsync(request, context);
    }

    public override Task<ResumeTrackResponse> ResumeTrack(ResumeTrackRequest request, ServerCallContext context)
    {
        if (!HasCapability(PauseCapability))
        {
            throw new RpcException(new Status(
                StatusCode.FailedPrecondition,
                $"Module '{Manifest.Slug}' does not advertise '{PauseCapability}'."));
        }

        return ResumeTrackCoreAsync(request, context);
    }

    /// <summary>Override when the module advertises <c>pause</c>. Default is Unimplemented.</summary>
    protected virtual Task<PauseTrackResponse> PauseTrackCoreAsync(
        PauseTrackRequest request,
        ServerCallContext context) =>
        throw new RpcException(new Status(StatusCode.Unimplemented, "PauseTrack is not implemented."));

    /// <summary>Override when the module advertises <c>pause</c>. Default is Unimplemented.</summary>
    protected virtual Task<ResumeTrackResponse> ResumeTrackCoreAsync(
        ResumeTrackRequest request,
        ServerCallContext context) =>
        throw new RpcException(new Status(StatusCode.Unimplemented, "ResumeTrack is not implemented."));

    protected bool HasCapability(string capability) =>
        Manifest.Capabilities.Any(c =>
            string.Equals(c, capability, StringComparison.OrdinalIgnoreCase));

    protected static RpcException JobNotFound(string trackJobId) =>
        new(new Status(StatusCode.NotFound, $"Track job '{trackJobId}' was not found."));
}
