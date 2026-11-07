using TrackUp.Api_.Application.DTOS;

namespace TrackUp.Api_.Application.validation.AuthValidation
{
    public class AuthValidateGoogleSignUpRequest
    {
        public bool ValidateGoogleSignUpRequest(GoogleSignUpRequest request)
        {
            if (request is null)
                return false;

            if (string.IsNullOrWhiteSpace(request.IdToken))
                return false;

           return true;
        }
    }
}
