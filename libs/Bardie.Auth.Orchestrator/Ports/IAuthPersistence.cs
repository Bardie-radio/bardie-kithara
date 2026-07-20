namespace Bardie.Auth.Orchestrator.Ports;

/// <summary>
/// Host persistence port for auth-orchestrator user/binding storage.
/// Phase 1: thin stubs; Phase 2 fills Authenticate / SeedAdmin behaviour.
/// </summary>
public interface IAuthPersistence
{
    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default);

    Task<int> CountUsersAsync(CancellationToken cancellationToken = default);
}
