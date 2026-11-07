namespace TrackUp.Api_.Application.DTOS;

public sealed class TokenResponse
{
    public int SessionId { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
}