using Microsoft.AspNetCore.Identity;
using TrackUp.Api_.Application.DTOS;

namespace TrackUp.Api_.Application.Interfaces
{
    public interface AuthIinterfaces
    {
        public Task<IdentityResult> RegisterUserAsync(SignUpRequest request);
        public Task<GoogleSignUpResult> RegisterGoogleUserAsync(GoogleSignUpRequest request);
        public Task<AuthResult> GoogleLoginAsync(GoogleSignUpRequest request, string deviceName);
        public Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request);
        public Task<IdentityResult> ResendConfirmationEmailAsync(string email);
        public Task<AuthResult> LoginAsync(LoginRequest request);
        public Task<AuthResult> RefreshAsync(string refreshToken);
        public Task<bool> RevokeSessionAsync(string userId, int sessionId);
        public Task LogoutAsync(string refreshToken);
    }
}