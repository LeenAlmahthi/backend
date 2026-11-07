using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TrackUp.Api_.Application.DTOS;
using TrackUp.Api_.Application.Interfaces;
using TrackUp.Api_.Application.Options;
using TrackUp.Api_.Domain.Model;
using TrackUp.Api_.Infrastructure.Data;

namespace TrackUp.Api_.Infrastructure.IdentityAuth;

public sealed class TokenService(
    TrackUpDataContext dataContext,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options) : ITokenService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private readonly JwtOptions _options = options.Value;

    public async Task<TokenResponse> IssueAsync(ApplicationUser user, string deviceName, CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshHash = Hash(refreshToken);
        var now = DateTime.UtcNow;
        var session = new RefreshToken
        {
            UserId = user.Id,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Unknown device" : deviceName.Trim(),
            TokenHash = refreshHash,
            CreatedAt = now,
            ExpiresAt = now.Add(RefreshTokenLifetime)
        };

        dataContext.RefreshTokens.Add(session);
        await dataContext.SaveChangesAsync(cancellationToken);

        return new TokenResponse
        {
            SessionId = session.Id,
            AccessToken = CreateAccessToken(user),
            RefreshToken = refreshToken,
            ExpiresIn = _options.AccessTokenExpirationMinutes * 60
        };
    }

    public async Task<(ApplicationUser? User, RefreshToken? Session)> FindSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var session = await dataContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (session is null)
            return (null, null);

        var user = await userManager.FindByIdAsync(session.UserId);
        return (user, session);
    }

    public Task<RefreshToken?> GetSessionAsync(
        int sessionId,
        string userId,
        CancellationToken cancellationToken = default) =>
        dataContext.RefreshTokens.SingleOrDefaultAsync(
            x => x.Id == sessionId && x.UserId == userId,
            cancellationToken);

    public async Task RevokeAsync(
        RefreshToken session,
        string? replacementHash = null,
        CancellationToken cancellationToken = default)
    {
        if (session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.ReplacedByTokenHash = replacementHash;
            await dataContext.SaveChangesAsync(cancellationToken);
        }
    }

    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private string CreateAccessToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            DateTime.UtcNow,
            expires,
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Issuer) ||
            string.IsNullOrWhiteSpace(_options.Audience) ||
            string.IsNullOrWhiteSpace(_options.SecretKey) ||
            Encoding.UTF8.GetByteCount(_options.SecretKey) < 32)
        {
            throw new InvalidOperationException("JWT configuration is missing or has an insufficient signing key.");
        }
    }
    public async Task<TokenResponse> RotateAsync(
    ApplicationUser user,
    RefreshToken session,
    CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        var newRefreshToken =
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var newHash = Hash(newRefreshToken);

        // Keep the same session ID and original expiry.
        session.TokenHash = newHash;
        session.ReplacedByTokenHash = newHash;

        await dataContext.SaveChangesAsync(cancellationToken);

        return new TokenResponse
        {
            SessionId = session.Id,
            AccessToken = CreateAccessToken(user),
            RefreshToken = newRefreshToken,
            ExpiresIn = _options.AccessTokenExpirationMinutes * 60
        };
    }
}
