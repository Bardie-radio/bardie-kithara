using Bardie.Orchestrator.Auth.Ports;
using Kithara.Infrastructure.Persistence.Entities;

namespace Kithara.Features.Streams;

/// <summary>
/// Struna listen / control ACL helpers.
/// Owner + grant checks are live; managed-ceiling and grant-CRUD deepen later — see Phase 6 comments.
/// </summary>
public static class StrunaAccess
{
    /// <summary>
    /// True when the principal may DJ this Struna (play / queue / skip / pause / delete).
    /// Full auth on Phase 6: managed permission ceiling, grant management API.
    /// </summary>
    public static bool CanControl(Struna struna, AuthUserRecord principal)
    {
        if (struna.OwnerUserId == principal.UserId)
        {
            return true;
        }

        if (struna.ControlGrants.Any(g => g.UserId == principal.UserId))
        {
            return true;
        }

        if (string.Equals(principal.Kind, nameof(UserKind.EphemeralGuest), StringComparison.OrdinalIgnoreCase)
            && principal.GuestStrunaId == struna.Id
            && struna.ControlAccess == ControlAccess.Protected)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// True when the principal may discover this Struna as listenable via REST.
    /// Actual <c>/stream/{slug}</c> gates (listen token) are Stream Server / Phase 5.
    /// Full auth on Phase 6 / Phase 5: private playback session rules; protected listen-token holders.
    /// </summary>
    public static bool CanListen(Struna struna, Guid principalUserId) =>
        struna.PlaybackAccess switch
        {
            PlaybackAccess.Public => true,
            PlaybackAccess.Protected =>
                struna.OwnerUserId == principalUserId
                || struna.ControlGrants.Any(g => g.UserId == principalUserId),
            PlaybackAccess.Private =>
                struna.OwnerUserId == principalUserId
                || struna.ControlGrants.Any(g => g.UserId == principalUserId),
            _ => false,
        };
}
