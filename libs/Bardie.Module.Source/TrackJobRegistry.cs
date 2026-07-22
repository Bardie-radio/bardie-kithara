using System.Collections.Concurrent;
using Bardie.Source.V1;

namespace Bardie.Module.Source;

/// <summary>In-flight track job tracked by a source module.</summary>
public sealed class TrackJob
{
    public required string TrackJobId { get; init; }
    public required string StrunaId { get; init; }
    public required string TrackRef { get; init; }
    public required string AudioEndpoint { get; init; }
    public TrackState State { get; set; } = TrackState.Running;
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? ErrorMessage { get; set; }
    public CancellationTokenSource Cancellation { get; } = new();
    public bool IsPaused { get; set; }
}

public interface ITrackJobRegistry
{
    TrackJob Create(string strunaId, string trackRef, string audioEndpoint);
    bool TryGet(string trackJobId, out TrackJob? job);
    bool TryRemove(string trackJobId, out TrackJob? job);
    IReadOnlyCollection<TrackJob> List();
}

/// <summary>Concurrent track-job registry for Stop / Pause / Resume / TrackStatus.</summary>
public sealed class TrackJobRegistry : ITrackJobRegistry
{
    private readonly ConcurrentDictionary<string, TrackJob> _jobs = new(StringComparer.Ordinal);

    public TrackJob Create(string strunaId, string trackRef, string audioEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strunaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(audioEndpoint);

        var job = new TrackJob
        {
            TrackJobId = Guid.NewGuid().ToString("N"),
            StrunaId = strunaId,
            TrackRef = trackRef,
            AudioEndpoint = audioEndpoint,
        };

        if (!_jobs.TryAdd(job.TrackJobId, job))
        {
            throw new InvalidOperationException("Failed to register track job id.");
        }

        return job;
    }

    public bool TryGet(string trackJobId, out TrackJob? job) =>
        _jobs.TryGetValue(trackJobId, out job);

    public bool TryRemove(string trackJobId, out TrackJob? job)
    {
        if (_jobs.TryRemove(trackJobId, out var removed))
        {
            job = removed;
            return true;
        }

        job = null;
        return false;
    }

    public IReadOnlyCollection<TrackJob> List() => _jobs.Values.ToArray();
}
