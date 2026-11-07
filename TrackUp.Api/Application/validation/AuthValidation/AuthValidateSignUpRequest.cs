using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TrackUp.Api_.Application.DTOS;

namespace TrackUp.Api_.Application.validation.AuthValidation
{
    public class AuthValidateSignUpRequest
    {
        public bool ValidateSignUpRequest(SignUpRequest request)
        {
            if (request == null)
                return false;

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return false;
            }

            if (request.FirstName.Trim().Length < 1 ||
                request.FirstName.Trim().Length > 80)
                return false;

            if (request.LastName.Trim().Length < 1 ||
                request.LastName.Trim().Length > 80)
                return false;

            if (!new EmailAddressAttribute().IsValid(request.Email) ||  request.Email.Length > 254)
                return false;

            //if (request.Password.Length < 8 || request.Password.Length > 64)
            //    return false;

            if (!request.Password.Any(char.IsUpper))
                return false;

            //if (!request.Password.Any(char.IsLower))
            //    return false;

            if (!request.Password.Any(char.IsDigit))
                return false;

            if (!request.Password.Any(c => "!@#$%^&*".Contains(c)))
                return false;

            if (request.Password != request.ConfirmPassword)
                return false;

            return true;
        }
    }
}

