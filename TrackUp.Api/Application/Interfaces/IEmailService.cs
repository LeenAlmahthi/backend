namespace TrackUp.Api_.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody);
    }
}