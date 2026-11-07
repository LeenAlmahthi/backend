using TrackUp.Api_.Application.DTOS;

namespace TrackUp.Api_.Application.Interfaces
{
    public interface IGoogleIdTokenVerifier
    {
        Task<GoogleIdTokenVerificationResult> VerifyAsync(string idToken);
    }
}