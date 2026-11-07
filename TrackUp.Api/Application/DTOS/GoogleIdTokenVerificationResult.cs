using TrackUp.Api_.Application.Interfaces;

namespace TrackUp.Api_.Application.DTOS;

public sealed record GoogleIdTokenVerificationResult(
    GoogleIdTokenVerificationStatus Status,
    GoogleIdTokenClaims? Claims)
{
    public static GoogleIdTokenVerificationResult Success(GoogleIdTokenClaims claims) =>
        new(GoogleIdTokenVerificationStatus.Valid, claims);

    public static GoogleIdTokenVerificationResult Failed(GoogleIdTokenVerificationStatus status) =>
        new(status, null);
}
