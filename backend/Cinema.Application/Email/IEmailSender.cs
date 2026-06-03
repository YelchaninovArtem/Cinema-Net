namespace Cinema.Application.Email;

public sealed record EmailAttachment(string FileName, byte[] Data, string MimeType = "image/png");

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken ct = default);
}
