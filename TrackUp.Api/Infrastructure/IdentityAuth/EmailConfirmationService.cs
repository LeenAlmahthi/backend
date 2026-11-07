
using Microsoft.AspNetCore.Identity;
using Resend;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using TrackUp.Api_.Application.Interfaces;
using TrackUp.Api_.Domain.Model;

namespace TrackUp.Api_.Infrastructure.IdentityAuth;

public class EmailConfirmationService(
    UserManager<ApplicationUser> userManager,
    IResend resend,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger EmailConfirmationService logger) : IEmailService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IResend _resend = resend;
    private readonly IConfiguration _configuration = configuration;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger EmailConfirmationService _logger = logger;

    private const int TokenExpirationHours = 24;

    public async Task IdentityResult SendConfirmationEmailAsync(string email)
    {
        _logger.LogInformation(
            "EMAIL STEP 1: Starting confirmation email process.");

        try
        {
            _logger.LogInformation("EMAIL STEP 2: Searching for user.");

            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                _logger.LogWarning("EMAIL ERROR: User was not found.");
                return IdentityResult.Failed();
            }

            _logger.LogInformation(
                "EMAIL STEP 3: User found. EmailConfirmed = {EmailConfirmed}",
                user.EmailConfirmed);

            if (user.EmailConfirmed)
            {
                _logger.LogWarning("EMAIL ERROR: User is already confirmed.");
                return IdentityResult.Failed();
            }

            _logger.LogInformation(
                "EMAIL STEP 4: Generating confirmation token.");

            var token = await _userManager
                .GenerateEmailConfirmationTokenAsync(user);

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("EMAIL ERROR: Generated token is empty.");
                return IdentityResult.Failed();
            }

            _logger.LogInformation(
                "EMAIL STEP 5: Token generated successfully.");

            user.EmailConfirmationTokenHash = HashToken(token);
            user.EmailConfirmationTokenExpiresAt =
                DateTime.UtcNow.AddHours(TokenExpirationHours);

            _logger.LogInformation(
                "EMAIL STEP 6: Saving confirmation token hash.");

            var saveResult = await _userManager.UpdateAsync(user);

            if (!saveResult.Succeeded)
            {
                LogIdentityErrors("Saving confirmation token", saveResult);
                return IdentityResult.Failed(saveResult.Errors.ToArray());
            }

            _logger.LogInformation(
                "EMAIL STEP 7: Token hash saved successfully.");

            _logger.LogInformation(
                "EMAIL STEP 8: Building confirmation email.");

            await BuildAndSendConfirmationEmailAsync(email, token);

            _logger.LogInformation(
                "EMAIL STEP 9: Confirmation email sent successfully.");

            user.LastConfirmationEmailSentAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                LogIdentityErrors("Updating last email sent time", updateResult);
                return IdentityResult.Failed(updateResult.Errors.ToArray());
            }

            _logger.LogInformation(
                "EMAIL STEP 10: Confirmation email process completed.");

            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "EMAIL FAILED: Confirmation email process failed.");

            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "EmailConfirmationFailed",
                    Description = "Failed to send confirmation email."
                });
        }
    }

    private async Task BuildAndSendConfirmationEmailAsync(
        string email,
        string token)
    {
        _logger.LogInformation(
            "EMAIL BUILD STEP 1: Validating email address.");

        if (!MailAddress.TryCreate(email, out _))
            throw new ArgumentException("Invalid email address.");

        _logger.LogInformation(
            "EMAIL BUILD STEP 2: Reading frontend URL.");

        var frontendUrl = _configuration["Frontend:BaseUrl"];

        if (string.IsNullOrWhiteSpace(frontendUrl))
            throw new InvalidOperationException(
                "Frontend:BaseUrl configuration is missing.");

        _logger.LogInformation(
            "EMAIL BUILD STEP 3: Frontend URL is configured.");

        var confirmationUrl =
            $"{frontendUrl.TrimEnd('/')}/confirm-email" +
            $"?email={Uri.EscapeDataString(email)}" +
            $"&token={Uri.EscapeDataString(token)}";

        var templatePath = Path.Combine(
            _environment.ContentRootPath,
            "EmailTemplates",
            "ConfirmEmail.html");

        _logger.LogInformation(
            "EMAIL BUILD STEP 4: Checking email template.");

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException(
                "Email confirmation template was not found.",
                templatePath);
        }

        var htmlBody = await File.ReadAllTextAsync(templatePath);

        _logger.LogInformation(
            "EMAIL BUILD STEP 5: Email template loaded successfully.");

        htmlBody = htmlBody
            .Replace("{{Email}}",
                WebUtility.HtmlEncode(email.Split('@')[0]))
            .Replace("{{ConfirmationUrl}}",
                WebUtility.HtmlEncode(confirmationUrl));

        _logger.LogInformation(
            "EMAIL BUILD STEP 6: Email content prepared.");

        await SendEmailAsync(email, "Confirm your email", htmlBody);
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody)
    {
        _logger.LogInformation(
            "RESEND STEP 1: Preparing email request.");

        var apiKey = _configuration["Resend:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Resend API key is missing.");

        var fromEmail = _configuration["Resend:FromEmail"]
            ?? "onboarding@resend.dev";

        var fromName = _configuration["Resend:FromName"]
            ?? "TrackUp";

        _logger.LogInformation(
            "RESEND STEP 2: Configuration found. Sender = {Sender}",
            fromEmail);

        var message = new EmailMessage
        {
            From = $"{fromName} <{fromEmail}>",
            Subject = subject,
            HtmlBody = htmlBody
        };

        message.To.Add(to);

        _logger.LogInformation(
            "RESEND STEP 3: Sending email through Resend.");

        try
        {
            await _resend.EmailSendAsync(message);

            _logger.LogInformation(
                "RESEND STEP 4: Resend request completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "RESEND FAILED: Resend rejected or failed the email request.");

            throw;
        }
    }

    public async Task InvalidateConfirmationTokenAsync(
        ApplicationUser user)
    {
        _logger.LogInformation(
            "EMAIL: Invalidating confirmation token.");

        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationTokenExpiresAt = null;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            LogIdentityErrors("Invalidating confirmation token", result);
    }

    public string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    private void LogIdentityErrors(
        string operation,
        IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            _logger.LogError(
                "IDENTITY ERROR during {Operation}: {Code} - {Description}",
                operation,
                error.Code,
                error.Description);
        }
    }
}
