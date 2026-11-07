namespace TrackUp.Api_.Application.Interfaces;

public enum GoogleIdTokenVerificationStatus
{
    Valid,
    Invalid,
    Expired,
    WrongAudience,
    UnverifiedEmail,
    ConfigurationError
}
