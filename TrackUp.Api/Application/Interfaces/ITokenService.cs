using TrackUp.Api_.Application.DTOS;
using TrackUp.Api_.Domain.Model;

namespace TrackUp.Api_.Application.Interfaces;

public interface ITokenService
{
    Task<TokenResponse> IssueAsync(ApplicationUser user, string deviceName, CancellationToken cancellationToken = default);
    Task<(ApplicationUser? User, RefreshToken? Session)> FindSessionAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetSessionAsync(int sessionId, string userId, CancellationToken cancellationToken = default);
    Task RevokeAsync(RefreshToken session, string? replacementHash = null, CancellationToken cancellationToken = default);
    Task<TokenResponse> RotateAsync(
        ApplicationUser user,
        RefreshToken session,
        CancellationToken cancellationToken = default);
}
