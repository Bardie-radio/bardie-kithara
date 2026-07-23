using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kithara.Infrastructure.Persistence;
using Kithara.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kithara.Features.Auth;

/// <summary>Mints Kithara-signed JWTs for ephemeral guest users after guest-code exchange.</summary>
public sealed class GuestJwtService
{
    public const string ProviderClaimValue = "kithara.guest";
    public const string GuestStrunaClaim = "bardie_guest_struna";

    private readonly GuestJwtOptions _options;
    private readonly GuestJwtSigningKeyStore _keys;
    private readonly IDbContextFactory<KitharaDbContext> _dbFactory;

    public GuestJwtService(
        IOptions<GuestJwtOptions> options,
        GuestJwtSigningKeyStore keys,
        IDbContextFactory<KitharaDbContext> dbFactory)
    {
        _options = options.Value;
        _keys = keys;
        _dbFactory = dbFactory;
    }

    public async Task<(Guid UserId, string AccessToken, string RefreshToken, long ExpiresIn)?> ExchangeAsync(
        Guid strunaId,
        string guestCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guestCode);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var struna = await db.Strunas.FirstOrDefaultAsync(s => s.Id == strunaId, cancellationToken)
            .ConfigureAwait(false);
        if (struna is null
            || struna.ControlAccess != ControlAccess.Protected
            || string.IsNullOrWhiteSpace(struna.GuestCode)
            || !string.Equals(struna.GuestCode, guestCode.Trim(), StringComparison.Ordinal))
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Kind = UserKind.EphemeralGuest,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = "active",
            GuestStrunaId = strunaId,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var (access, refresh, expiresIn) = MintTokens(user.Id, strunaId);
        return (user.Id, access, refresh, expiresIn);
    }

    private (string Access, string Refresh, long ExpiresIn) MintTokens(Guid userId, Guid strunaId)
    {
        var key = _keys.GetSigningKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(Math.Max(1, _options.AccessTokenMinutes));
        var refreshExpires = now.AddDays(7);

        var access = CreateToken(
            [
                new Claim("sub", userId.ToString("D")),
                new Claim("bardie_provider", ProviderClaimValue),
                new Claim("token_use", "access"),
                new Claim(GuestStrunaClaim, strunaId.ToString("D")),
            ],
            now,
            accessExpires,
            creds);

        var refresh = CreateToken(
            [
                new Claim("sub", userId.ToString("D")),
                new Claim("bardie_provider", ProviderClaimValue),
                new Claim("token_use", "refresh"),
                new Claim(GuestStrunaClaim, strunaId.ToString("D")),
            ],
            now,
            refreshExpires,
            creds);

        return (access, refresh, (long)(accessExpires - now).TotalSeconds);
    }

    private string CreateToken(
        IEnumerable<Claim> claims,
        DateTime notBefore,
        DateTime expires,
        SigningCredentials creds)
    {
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
