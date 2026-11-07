using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrackUp.Api_.Application.Interfaces;
using TrackUp.Api_.Application.DTOS;
using TrackUp.Api_.Application.Options;
namespace TrackUp.Api_.Infrastructure.IdentityAuth;

public class GoogleIdTokenVerifier(
    IOptions<GoogleAuthOptions> options,
    ILogger<GoogleIdTokenVerifier> logger) : IGoogleIdTokenVerifier
{
    private readonly GoogleAuthOptions _options = options.Value;

    public async Task<GoogleIdTokenVerificationResult> VerifyAsync(string idToken)
    {
        Console.WriteLine($"client Id \n\n\n\n\n{_options.ClientId}\n\n\n\n");
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            logger.LogError("Google ClientId is not configured.");
            return GoogleIdTokenVerificationResult.Failed(GoogleIdTokenVerificationStatus.ConfigurationError);
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_options.ClientId]
                });
            if (!payload.EmailVerified)
                return GoogleIdTokenVerificationResult.Failed(GoogleIdTokenVerificationStatus.UnverifiedEmail);

            return GoogleIdTokenVerificationResult.Success(new GoogleIdTokenClaims
            {
                Subject = payload.Subject,
                Email = payload.Email,
                EmailVerified = payload.EmailVerified,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName,
                Nonce = payload.Nonce
            });
        }
        catch (InvalidJwtException)
        {
            return GoogleIdTokenVerificationResult.Failed(GoogleIdTokenVerificationStatus.Invalid);
        }
    }
}