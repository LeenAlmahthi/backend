using System.Numerics;
using System.Runtime.Intrinsics.X86;

namespace TrackUp.Api_.Application.DTOS
{
    public class SignUpResponse
    {
        public string code { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;

        public SignUpResponse CustomResponse(int statusCode)
        {
            var code = statusCode switch
            {
                200 => "VERIFICATION_EMAIL_REQUESTED",
                201 => "REGISTRATION_EMAIL_FAILED",
                400 => "INVALID_REQUEST",
                409 => "EMAIL_ALREADY_EXISTS",
                500 => "INTERNAL_ERROR",
                _ => throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "Unsupported response status code.")
            };

            return CustomResponse(code);
        }
        public SignUpResponse CustomResponse(string Code)
        {
            if (Code == "EMAIL_ALREADY_EXISTS")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "An account with this email already exists. Please log in or request a verification email."
                });
            }
            if (Code == "GOOGLE_ACCOUNT_LINKED")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "Your verified Google identity has been linked to your existing account."
                });
            }
            if (Code == "EMAIL_VERIFIED")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "Your email has been verified successfully. You can now log in."
                });

            }
           
            if (Code == "VERIFICATION_FAILED")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "This verification link is invalid, expired, or has already been used."
                });
            }
            if (Code == "REGISTRATION_SUCCESS")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "Your account has been created. Please check your email to verify your account."
                });
            }
            if (Code == "VERIFICATION_EMAIL_REQUESTED")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "If this email address requires verification, a new verification link will be sent."
                });
            }
            if (Code == "INVALID_REQUEST")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "The request is invalid. Please check the submitted data and try again."
                });
            }
            if (Code == "INTERNAL_ERROR")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "Something went wrong. Please try again later."
                });
            }
            if (Code == "REGISTRATION_EMAIL_FAILED")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "Your account was created, but we couldn't send the verification email. Please request a new one."
                });
            }
            if (Code == "GOOGLE_REGISTRATION_SUCCESS")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "Your account has been created with Google. You can now sign in with Google."
                });
            }
            if (Code == "GOOGLE_ACCOUNT_ALREADY_EXISTS")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "A TrackUp account is already linked to this Google identity. Please sign in with Google."
                });
            }
            if (Code == "GOOGLE_REGISTRATION_FAILED")
            {
                return (new SignUpResponse()
                {
                    code = Code,
                    message = "We could not complete Google registration. Please try again or use another sign -in method "
                });
            }
            return (new SignUpResponse());
        } 
    }
}
