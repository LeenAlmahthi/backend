namespace TrackUp.Api_.Application.DTOS;

public sealed record GoogleIdTokenClaims
{
    public string Subject { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool EmailVerified { get; init; }
    public string? GivenName { get; init; }
    public string? FamilyName { get; init; }
    public string? Nonce { get; init; }
}
