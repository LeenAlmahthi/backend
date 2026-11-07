namespace TrackUp.Api_.Application.DTOS;

public enum AuthResultStatus
{
    Success,
    InvalidCredentials,
    EmailNotConfirmed,
    LockedOut,
    AccountSuspended,
    AccountDeleted,
    ExternalAccountOnly,
    InvalidRefreshToken,
    Failed
}

public sealed class AuthResult
{
    public AuthResultStatus Status { get; init; }
    public TokenResponse? Tokens { get; init; }
    public int? SessionId { get; init; }

    public static AuthResult Failure(AuthResultStatus status) => new() { Status = status };
}