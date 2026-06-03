using Cinema.Application.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Cinema.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> opts, ILogger<SmtpEmailSender> logger)
    {
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.Host))
            throw new InvalidOperationException("Email SMTP host is not configured. Set Email:Host or Email__Host.");

        if (string.IsNullOrWhiteSpace(_opts.From))
            throw new InvalidOperationException("Email sender address is not configured. Set Email:From or Email__From.");

        var attachmentList = attachments?.ToList() ?? [];
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opts.SenderName, _opts.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        foreach (var a in attachmentList)
            builder.Attachments.Add(a.FileName, a.Data, ContentType.Parse(a.MimeType));

        message.Body = builder.ToMessageBody();

        _logger.LogInformation(
            "Sending email via SMTP {Host}:{Port} From={From} To={To} Attachments={AttachmentCount}",
            _opts.Host, _opts.Port, _opts.From, to, attachmentList.Count);

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_opts.Host, _opts.Port, SecureSocketOptions.StartTlsWhenAvailable, ct);
            if (!string.IsNullOrWhiteSpace(_opts.User))
            {
                if (string.IsNullOrWhiteSpace(_opts.Password))
                    throw new InvalidOperationException("Email SMTP password is not configured. Set Email:Password or Email__Password.");
                await client.AuthenticateAsync(_opts.User, _opts.Password, ct);
            }
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email via SMTP {Host}:{Port} From={From} To={To}",
                _opts.Host, _opts.Port, _opts.From, to);
            throw;
        }
    }
}
