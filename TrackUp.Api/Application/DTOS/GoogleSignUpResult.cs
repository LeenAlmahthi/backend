namespace TrackUp.Api_.Application.DTOS
{
    public enum GoogleSignUpStatus
    {
        Created,
        AlreadyRegistered,
        LinkedToExistingAccount,
        InvalidToken,
        ExpiredToken,
        WrongAudience,
        UnverifiedGoogleEmail,
        MissingName,
        EmailVerificationRequired,
        PersistenceFailed,
        ConfigurationError
    }

    public class GoogleSignUpResult
    {
        public GoogleSignUpStatus Status { get; init; }
        public string? Message { get; init; }

        public static GoogleSignUpResult From(GoogleSignUpStatus status) =>
            new() { Status = status };
    }
}
