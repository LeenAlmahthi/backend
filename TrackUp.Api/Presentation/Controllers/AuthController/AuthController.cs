using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrackUp.Api_.Application.DTOS;
using TrackUp.Api_.Application.validation.AuthValidation;
using TrackUp.Api_.Infrastructure.IdentityAuth;

namespace TrackUp.Api_.Presentation.Controllers.Auth
{
    [ApiController]
    [Route("Api/[Controller]")]
    public class AuthController : ControllerBase
    {
        
        private readonly SignUpResponse _SignUpResponse;
        private readonly AuthValidateSignUpRequest _AuthValidateSignUpRequest;
        private readonly AuthValidateGoogleSignUpRequest _AuthValidateGoogleSignUpRequest;
        private readonly IdentityAuth _IdentityAuth;
        private readonly EmailConfirmationService _EmailConfirmationService;
        public AuthController(
            AuthValidateSignUpRequest _authValidateSignUpRequest,
            AuthValidateGoogleSignUpRequest authValidateGoogleSignUpRequest,
            EmailConfirmationService emailConfirmationService,
            SignUpResponse signUpResponse,
            IdentityAuth identityAuth)
        {
            _AuthValidateSignUpRequest = _authValidateSignUpRequest;
            _AuthValidateGoogleSignUpRequest = authValidateGoogleSignUpRequest;
            _SignUpResponse = signUpResponse;
            _IdentityAuth = identityAuth;
            _EmailConfirmationService = emailConfirmationService;    
        }
        [HttpPost("Register")]
        public async Task<ActionResult> Register(SignUpRequest request)
        {
            if (!_AuthValidateSignUpRequest.ValidateSignUpRequest(request))
                return BadRequest(_SignUpResponse.CustomResponse(400));

            IdentityResult result = await _IdentityAuth.RegisterUserAsync(request);

            if (!result.Succeeded)
                return Conflict(_SignUpResponse.CustomResponse(409));

            var emailResult =
                await _EmailConfirmationService.SendConfirmationEmailAsync(request.Email);
            if (emailResult.Succeeded)
                return Created(string.Empty, _SignUpResponse.CustomResponse("REGISTRATION_SUCCESS"));
            return Created(string.Empty, _SignUpResponse.CustomResponse(201));
        }

        [HttpPost("GoogleRegister")]
        public async Task<ActionResult> GoogleRegister(GoogleSignUpRequest request)
        {
            if (!_AuthValidateGoogleSignUpRequest.ValidateGoogleSignUpRequest(request))
                return BadRequest(_SignUpResponse.CustomResponse(400));

            var result = await _IdentityAuth.RegisterGoogleUserAsync(request);
            Console.WriteLine($"\n\n\n\n\n\n\nGoogle Registration Status: {result.Status}\n\n\n\n");
            if (result.Status is GoogleSignUpStatus.InvalidToken or GoogleSignUpStatus.UnverifiedGoogleEmail)
                return BadRequest(_SignUpResponse.CustomResponse("GOOGLE_REGISTRATION_FAILED"));
            if (result.Status == GoogleSignUpStatus.AlreadyRegistered)
                return Conflict(_SignUpResponse.CustomResponse("GOOGLE_ACCOUNT_ALREADY_EXISTS"));
            if (result.Status == GoogleSignUpStatus.LinkedToExistingAccount)
                return Ok(_SignUpResponse.CustomResponse("GOOGLE_ACCOUNT_LINKED"));
            if (result.Status is GoogleSignUpStatus.MissingName or GoogleSignUpStatus.EmailVerificationRequired)
                return BadRequest(_SignUpResponse.CustomResponse("GOOGLE_REGISTRATION_FAILED"));
            if (result.Status != GoogleSignUpStatus.Created)
                return BadRequest(_SignUpResponse.CustomResponse("GOOGLE_REGISTRATION_FAILED"));
            return Created(string.Empty, _SignUpResponse.CustomResponse("GOOGLE_REGISTRATION_SUCCESS"));
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _IdentityAuth.LoginAsync(request);
            return AuthResultResponse(result);
        }

        [HttpPost("GoogleLogin")]
        public async Task<IActionResult> GoogleLogin(GoogleSignUpRequest request)
        {
            if (!_AuthValidateGoogleSignUpRequest.ValidateGoogleSignUpRequest(request))
                return BadRequest(_SignUpResponse.CustomResponse(400));

            var deviceName = Request.Headers.UserAgent.ToString();

            var result = await _IdentityAuth.GoogleLoginAsync(request, deviceName);

            return AuthResultResponse(result);
        }


        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized();

            var result = await _IdentityAuth.RefreshAsync(refreshToken);

            return AuthResultResponse(result);
        }
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrWhiteSpace(refreshToken))
                await _IdentityAuth.LogoutAsync(refreshToken);

            ClearAuthCookies();

            return NoContent();
        }
        // This endpoint let u login anther device remotly like a mobile device or a desktop app and revoke the session from the server side.
        [Authorize]
        [HttpPost("RevokeSession")]
        public async Task<IActionResult> RevokeSession(RevokeSessionRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (userId is null)
                return Unauthorized();

            var revoked = await _IdentityAuth.RevokeSessionAsync(userId, request.SessionId);
            return revoked ? NoContent() : NotFound();
        }

        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail(ConfirmEmailRequest request)
        {
            var result = await _IdentityAuth.ConfirmEmailAsync(request);
            if (!result)
                return BadRequest(_SignUpResponse.CustomResponse("VERIFICATION_FAILED"));
            return Ok(_SignUpResponse.CustomResponse("EMAIL_VERIFIED"));
        }

        [HttpPost("SendConfirmation")]
        public async Task<IActionResult> SendConfirmationEmail(
            [FromBody] string email)
        {
            var result =
                await _EmailConfirmationService.SendConfirmationEmailAsync(email);

            if (!result.Succeeded)
                return BadRequest(_SignUpResponse.CustomResponse(400));

            return Ok(_SignUpResponse.CustomResponse(200));
        }

        [HttpPost("ResendEmail")]
        public async Task<IActionResult> ResendConfirmationEmail(
            [FromBody] string email)
        {
            var result =
                await _IdentityAuth.ResendConfirmationEmailAsync(email);
            return Ok(_SignUpResponse.CustomResponse("VERIFICATION_EMAIL_REQUESTED"));
        }
        [HttpGet("test-500")]
        public IActionResult Test500()
        {
            throw new Exception("Test exception for Global Exception Handler");
        }

        private IActionResult AuthResultResponse(AuthResult result)
        {
            if (result.Status == AuthResultStatus.Success && result.Tokens is not null)
            {
                SetAuthCookies(result.Tokens);
                Console.WriteLine($"\n\n\n\n\nNew Access Token: {result.Tokens.AccessToken}\n\n\n\n");

                return Ok(new
                {
                    message = "Authentication successful"
                });
            }

            return result.Status switch
            {
                AuthResultStatus.EmailNotConfirmed =>
                    Unauthorized(new { code = "EMAIL_NOT_CONFIRMED" }),

                AuthResultStatus.LockedOut =>
                    Unauthorized(new { code = "ACCOUNT_LOCKED" }),

                AuthResultStatus.ExternalAccountOnly =>
                    Unauthorized(new { code = "EXTERNAL_ACCOUNT_ONLY" }),

                AuthResultStatus.InvalidRefreshToken =>
                    Unauthorized(new { code = "INVALID_REFRESH_TOKEN" }),

                _ => Unauthorized(new { code = "INVALID_CREDENTIALS" })
            };
        }
        private void SetAuthCookies(TokenResponse tokens)
        {
            Response.Cookies.Append(
                "access_token",
                tokens.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn),
                    Path = "/"
                });

            Response.Cookies.Append(
                "refresh_token",
                tokens.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    Path = "/Api/Auth"
                });
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("access_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

            Response.Cookies.Delete("refresh_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/Api/Auth"
            });
        }
    }
}