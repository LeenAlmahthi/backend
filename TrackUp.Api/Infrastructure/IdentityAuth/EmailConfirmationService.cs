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
    IWebHostEnvironment environment) : IEmailService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IResend _resend = resend;
    private readonly IConfiguration _configuration = configuration;
    private readonly IWebHostEnvironment _environment = environment;

    private const int TokenExpirationHours = 24;

    public async Task<IdentityResult> SendConfirmationEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || user.EmailConfirmed)
            return IdentityResult.Failed();

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        Console.WriteLine($"\n\n\n\n\n\nGenerated token: {token}\n\n\n\n\n"); // Debugging line
        if (string.IsNullOrEmpty(token))
            return IdentityResult.Failed();
        user.EmailConfirmationTokenHash = HashToken(token);
        user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(TokenExpirationHours);

        var saveResult = await _userManager.UpdateAsync(user);

        if (!saveResult.Succeeded)
            return IdentityResult.Failed();

        try
        {
            await BuildAndSendConfirmationEmailAsync(email, token);
        }
        catch
        {
            await InvalidateConfirmationTokenAsync(user);
            return IdentityResult.Failed();
        }

        user.LastConfirmationEmailSentAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);

        return updateResult.Succeeded
            ? IdentityResult.Success
            : IdentityResult.Failed();
    }

    private async Task BuildAndSendConfirmationEmailAsync(string email,string token)
    {
        if (!MailAddress.TryCreate(email, out _))
            throw new ArgumentException("Invalid email address.", nameof(email));

        var frontendUrl = _configuration["Frontend:BaseUrl"];

        if (string.IsNullOrWhiteSpace(frontendUrl))
            throw new InvalidOperationException("Frontend:BaseUrl configuration is missing.");

        var confirmationUrl =
            $"{frontendUrl.TrimEnd('/')}/confirm-email" +
            $"?email={Uri.EscapeDataString(email)}" +
            $"&token={Uri.EscapeDataString(token)}";

        var templatePath = Path.Combine(
            _environment.ContentRootPath,
            "EmailTemplates",
            "ConfirmEmail.html");

        var htmlBody = await File.ReadAllTextAsync(templatePath);

        htmlBody = htmlBody
            .Replace("{{Email}}", WebUtility.HtmlEncode(email.Split('@')[0]))
            .Replace("{{ConfirmationUrl}}", WebUtility.HtmlEncode(confirmationUrl));
        await SendEmailAsync(email, "Confirm your email", htmlBody);
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody)
    {
        var fromEmail =
            _configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";

        var fromName = _configuration["Resend:FromName"] ?? "TrackUp";

        var message = new EmailMessage
        {
            From = $"{fromName} <{fromEmail}>",
            Subject = subject,
            HtmlBody = htmlBody
        };

        message.To.Add(to);

        await _resend.EmailSendAsync(message);
    }

    public async Task InvalidateConfirmationTokenAsync(ApplicationUser user)
    {
        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationTokenExpiresAt = null;

        await _userManager.UpdateAsync(user);
    }

    public string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}