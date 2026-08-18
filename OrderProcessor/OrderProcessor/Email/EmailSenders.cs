using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RefactoringExercise.Options;

namespace RefactoringExercise.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public class SmtpEmailSender(IOptions<SmtpOptions> smtpOptions) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var settings = smtpOptions.Value;

        using var smtp = new SmtpClient(settings.Host, settings.Port);
        smtp.Credentials = new NetworkCredential(settings.Username, settings.Password);
        smtp.EnableSsl = true;

        using var mail = new MailMessage(settings.FromAddress, to, subject, body);
        await smtp.SendMailAsync(mail, cancellationToken);
    }
}

public class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email suppressed (non-production). To {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
