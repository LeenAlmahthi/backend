using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using TrackUp.Api_.Application.DTOS;
using TrackUp.Api_.Application.Interfaces;
using TrackUp.Api_.Application.validation.AuthValidation;
using TrackUp.Api_.Domain.Model;

namespace TrackUp.Api_.Infrastructure.IdentityAuth
{
    public class IdentityAuth(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    EmailConfirmationService emailConfirmationService,
    IGoogleIdTokenVerifier googleIdTokenVerifier,
    ITokenService tokenService) : AuthIinterfaces
    {
        public IdentityAuth(
            UserManager<ApplicationUser> userManager,
            EmailConfirmationService emailConfirmationService,
            IGoogleIdTokenVerifier googleIdTokenVerifier)
            : this(userManager, null!, emailConfirmationService, googleIdTokenVerifier, null!)
        {
        }

        public const string GoogleLoginProvider = "Google";

        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly EmailConfirmationService _emailConfirmationService = emailConfirmationService;
        private readonly IGoogleIdTokenVerifier _googleIdTokenVerifier = googleIdTokenVerifier;
        private readonly ITokenService _tokenService = tokenService;
        public async Task<IdentityResult> RegisterUserAsync(SignUpRequest User)
        {
            var applicationudUser = new ApplicationUser
            {
                UserName = User.Email,
                Email = User.Email,
                FirstName = User.FirstName,
                LastName = User.LastName
            };
            IdentityResult re = await _userManager.CreateAsync(applicationudUser, User.Password);
            return re;
        }
        //public async Task<GoogleSignUpResult> RegisterGoogleUserAsync(GoogleSignUpRequest request)
        //{
        //var verification = await _googleIdTokenVerifier.VerifyAsync(request.IdToken);
        //if (verification.Status != GoogleIdTokenVerificationStatus.Valid || verification.Claims is null)
        //return GoogleSignUpResult.From(MapVerificationStatus(verification.Status));
        //var payload = verification.Claims;
        //if (!payload.EmailVerified)
        //return GoogleSignUpResult.From(GoogleSignUpStatus.UnverifiedGoogleEmail);
        //if (!string.IsNullOrWhiteSpace(request.Nonce) && payload.Nonce != request.Nonce)
        //return GoogleSignUpResult.From(GoogleSignUpStatus.InvalidToken);

        //var existingGoogleUser = await _userManager.FindByLoginAsync(GoogleLoginProvider, payload.Subject);

        //if (existingGoogleUser is null)
        //return GoogleSignUpResult.From(GoogleSignUpStatus.AlreadyRegistered);

        //var existingEmailUser = await _userManager.FindByEmailAsync(payload.Email);

        //if (existingEmailUser is null || !existingEmailUser.EmailConfirmed)
        //return GoogleSignUpResult.From(GoogleSignUpStatus.EmailVerificationRequired);

        //try
        //{
        //var linkResult = await _userManager.AddLoginAsync(
        //existingEmailUser,
        //new UserLoginInfo(GoogleLoginProvider, payload.Subject, GoogleLoginProvider));
        //if (linkResult is null )
        //return GoogleSignUpResult.From(GoogleSignUpStatus.ConfigurationError);
        ////return linkResult.Succeeded
        ////? GoogleSignUpResult.From(GoogleSignUpStatus.LinkedToExistingAccount)
        ////: GoogleSignUpResult.From(GoogleSignUpStatus.PersistenceFailed);
        //}
        //catch
        //{
        //return GoogleSignUpResult.From(GoogleSignUpStatus.PersistenceFailed);
        //}

        //var firstName = string.IsNullOrWhiteSpace(payload.GivenName)
        //? request.FirstName?.Trim()
        //: payload.GivenName.Trim();
        //var lastName = string.IsNullOrWhiteSpace(payload.FamilyName)
        //? request.LastName?.Trim()
        //: payload.FamilyName.Trim();

        //if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        //return GoogleSignUpResult.From(GoogleSignUpStatus.MissingName);

        //var user = new ApplicationUser
        //{
        //UserName = payload.Email,
        //Email = payload.Email,
        //EmailConfirmed = true,
        //FirstName = firstName,
        //LastName = lastName
        //};

        //var createResult = await _userManager.CreateAsync(user);

        //if (!createResult.Succeeded)
        //return GoogleSignUpResult.From(GoogleSignUpStatus.PersistenceFailed);

        //IdentityResult loginResult;
        //try
        //{
        //loginResult = await _userManager.AddLoginAsync(
        //user,
        //new UserLoginInfo(GoogleLoginProvider, payload.Subject, GoogleLoginProvider));
        //}
        //catch
        //{
        //await _userManager.DeleteAsync(user);
        //return GoogleSignUpResult.From(GoogleSignUpStatus.PersistenceFailed);
        //}

        //if (!loginResult.Succeeded)
        //{
        //await _userManager.DeleteAsync(user);
        //return GoogleSignUpResult.From(GoogleSignUpStatus.PersistenceFailed);
        //}
        //return GoogleSignUpResult.From(GoogleSignUpStatus.Created);
        //}

        public async Task<GoogleSignUpResult> RegisterGoogleUserAsync(
            GoogleSignUpRequest request)
        {
            var verification =
                await _googleIdTokenVerifier.VerifyAsync(request.IdToken);

            if (verification.Status != GoogleIdTokenVerificationStatus.Valid ||
                verification.Claims is null)
            {
                return GoogleSignUpResult.From(
                    MapVerificationStatus(verification.Status));
            }

            var payload = verification.Claims;

            if (!payload.EmailVerified)
                return GoogleSignUpResult.From(
                    GoogleSignUpStatus.UnverifiedGoogleEmail);

            if (!string.IsNullOrWhiteSpace(request.Nonce) &&
                payload.Nonce != request.Nonce)
            {
                return GoogleSignUpResult.From(
                    GoogleSignUpStatus.InvalidToken);
            }

            // 1. Check whether this Google account is already linked.
            var existingGoogleUser = await _userManager.FindByLoginAsync(
                GoogleLoginProvider,
                payload.Subject);

            if (existingGoogleUser is not null)
                return GoogleSignUpResult.From(
                    GoogleSignUpStatus.AlreadyRegistered);

            // 2. Check whether this email belongs to an existing user.
            var existingEmailUser =
                await _userManager.FindByEmailAsync(payload.Email);

            if (existingEmailUser is not null)
            {
                if (!existingEmailUser.EmailConfirmed)
                    return GoogleSignUpResult.From(
                        GoogleSignUpStatus.EmailVerificationRequired);

                var linkResult = await _userManager.AddLoginAsync(
                    existingEmailUser,
                    new UserLoginInfo(
                        GoogleLoginProvider,
                        payload.Subject,
                        GoogleLoginProvider));

                return linkResult.Succeeded
                    ? GoogleSignUpResult.From(
                        GoogleSignUpStatus.LinkedToExistingAccount)
                    : GoogleSignUpResult.From(
                        GoogleSignUpStatus.PersistenceFailed);
            }

            // 3. Create a new Google user.
            var firstName = string.IsNullOrWhiteSpace(payload.GivenName)
                ? request.FirstName?.Trim()
                : payload.GivenName.Trim();

            var lastName = string.IsNullOrWhiteSpace(payload.FamilyName)
                ? request.LastName?.Trim()
                : payload.FamilyName.Trim();

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName))
            {
                return GoogleSignUpResult.From(
                    GoogleSignUpStatus.MissingName);
            }

            var user = new ApplicationUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
                return GoogleSignUpResult.From(
                    GoogleSignUpStatus.PersistenceFailed);

            var loginResult = await _userManager.AddLoginAsync(
                user,
                new UserLoginInfo(
                    GoogleLoginProvider,
                    payload.Subject,
                    GoogleLoginProvider));

            if (!loginResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return GoogleSignUpResult.From(
                    GoogleSignUpStatus.PersistenceFailed);
            }

            return GoogleSignUpResult.From(GoogleSignUpStatus.Created);
        }

        public async Task<AuthResult> GoogleLoginAsync(GoogleSignUpRequest request, string deviceName)
        {
            var verification = await _googleIdTokenVerifier.VerifyAsync(request.IdToken);
            if (verification.Status != GoogleIdTokenVerificationStatus.Valid || verification.Claims is null)
                return AuthResult.Failure(AuthResultStatus.InvalidCredentials);
            var payload = verification.Claims;
            if (!string.IsNullOrWhiteSpace(request.Nonce) && payload.Nonce != request.Nonce)
                return AuthResult.Failure(AuthResultStatus.InvalidCredentials);

            var user = await _userManager.FindByLoginAsync(GoogleLoginProvider, payload.Subject);
            if (user is null)
                return AuthResult.Failure(AuthResultStatus.InvalidCredentials);

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                return AuthResult.Failure(AuthResultStatus.LockedOut);

            return new AuthResult
            {
                Status = AuthResultStatus.Success,
                Tokens = await _tokenService.IssueAsync(user, deviceName)
            };
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return AuthResult.Failure(AuthResultStatus.InvalidCredentials);
            if (!user.EmailConfirmed)
                return AuthResult.Failure(AuthResultStatus.EmailNotConfirmed);
            if (user.PasswordHash is null)
                return AuthResult.Failure(AuthResultStatus.ExternalAccountOnly);

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
            
            if (result.IsLockedOut)
                return AuthResult.Failure(AuthResultStatus.LockedOut);
            if (!result.Succeeded)
                return AuthResult.Failure(AuthResultStatus.InvalidCredentials);

            return new AuthResult
            {
                Status = AuthResultStatus.Success,
                Tokens = await _tokenService.IssueAsync(user, "Password login")
            };
        }

        public async Task<AuthResult> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return AuthResult.Failure(AuthResultStatus.InvalidRefreshToken);

            var (user, session) =
                await _tokenService.FindSessionAsync(refreshToken);

            if (user is null ||
                session is null ||
                session.RevokedAt.HasValue ||
                session.ExpiresAt <= DateTime.UtcNow)
            {
                return AuthResult.Failure(AuthResultStatus.InvalidRefreshToken);
            }

            var tokens = await _tokenService.RotateAsync(user, session);

            return new AuthResult
            {
                Status = AuthResultStatus.Success,
                Tokens = tokens
            };
        }
        public async Task LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var (_, session) = await _tokenService.FindSessionAsync(refreshToken);

            if (session is null || session.RevokedAt.HasValue)
                return;

            await _tokenService.RevokeAsync(session);
        }
        public async Task<bool> RevokeSessionAsync(string userId, int sessionId)
        {
            var session = await _tokenService.GetSessionAsync(sessionId, userId);
            if (session is null)
                return false;
            await _tokenService.RevokeAsync(session);
            return true;
        }

        private static GoogleSignUpStatus MapVerificationStatus(GoogleIdTokenVerificationStatus status) =>
            status switch
            {
                GoogleIdTokenVerificationStatus.Expired => GoogleSignUpStatus.ExpiredToken,
                GoogleIdTokenVerificationStatus.WrongAudience => GoogleSignUpStatus.WrongAudience,
                GoogleIdTokenVerificationStatus.UnverifiedEmail => GoogleSignUpStatus.UnverifiedGoogleEmail,
                _ => GoogleSignUpStatus.InvalidToken
            };
        public async Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            var applicationUser = await _userManager.FindByEmailAsync(request.Email);
            if (applicationUser == null)
                return false;
            if (applicationUser.EmailConfirmed)
                return false;
            if (string.IsNullOrEmpty(applicationUser.EmailConfirmationTokenHash))
                return false;
            if (!applicationUser.EmailConfirmationTokenExpiresAt.HasValue)
                return false;
            if (DateTime.UtcNow >= applicationUser.EmailConfirmationTokenExpiresAt.Value)
                return false;
            var incomingTokenHash = _emailConfirmationService.HashToken(request.Token);
            byte[] storedHashBytes;
            try
            {
                storedHashBytes = Convert.FromHexString(applicationUser.EmailConfirmationTokenHash);
            }
            catch (FormatException)
            {
                return false;
            }
            var incomingHashBytes = Convert.FromHexString(incomingTokenHash);
            if (!CryptographicOperations.FixedTimeEquals(storedHashBytes, incomingHashBytes))
                return false;
            var result = await _userManager.ConfirmEmailAsync(applicationUser, request.Token);
            if (!result.Succeeded)
                return false;

            applicationUser.EmailConfirmationTokenHash = null;
            applicationUser.EmailConfirmationTokenExpiresAt = null;
            var updateResult = await _userManager.UpdateAsync(applicationUser);
            if (!updateResult.Succeeded)
                return false;
            return true;
        }
        public async Task<IdentityResult> ResendConfirmationEmailAsync(string email)
        {
            var applicationUser = await _userManager.FindByEmailAsync(email);

            if (applicationUser == null || applicationUser.EmailConfirmed)
                return IdentityResult.Failed();

            if (applicationUser.LastConfirmationEmailSentAt.HasValue &&
                DateTime.UtcNow - applicationUser.LastConfirmationEmailSentAt.Value
                    < TimeSpan.FromSeconds(60))
            {
                return IdentityResult.Failed();
            }
            return await _emailConfirmationService.SendConfirmationEmailAsync(email);
        }
    }
}
