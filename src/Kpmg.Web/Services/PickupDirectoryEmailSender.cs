using System.Net.Mail;
using Kpmg.Web.Models;
using Kpmg.Web.Options;
using Microsoft.Extensions.Options;

namespace Kpmg.Web.Services;

public record EmailMessage(string To, string Subject, string Body, IReadOnlyList<string> AttachmentFileNames);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Writes outgoing mail to a pickup directory as .eml files so the sample runs without
/// SMTP credentials. Replace this implementation (or relay the pickup folder) in production.
/// </summary>
public class PickupDirectoryEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly IPhotoStorage _photoStorage;
    private readonly ILogger<PickupDirectoryEmailSender> _logger;
    private readonly string _outboxPath;

    public PickupDirectoryEmailSender(
        IOptions<EmailOptions> options,
        IPhotoStorage photoStorage,
        IHostEnvironment environment,
        ILogger<PickupDirectoryEmailSender> logger)
    {
        _options = options.Value;
        _photoStorage = photoStorage;
        _logger = logger;
        _outboxPath = Path.IsPathRooted(_options.OutboxPath)
            ? _options.OutboxPath
            : Path.Combine(environment.ContentRootPath, _options.OutboxPath);
        Directory.CreateDirectory(_outboxPath);
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false
        };
        mail.To.Add(message.To);

        var openedStreams = new List<Stream>();
        try
        {
            foreach (var storedFileName in message.AttachmentFileNames)
            {
                var stream = _photoStorage.OpenRead(storedFileName);
                if (stream is null)
                {
                    _logger.LogWarning("Photo {StoredFileName} could not be attached because it was not found.", storedFileName);
                    continue;
                }

                openedStreams.Add(stream);
                mail.Attachments.Add(new Attachment(stream, storedFileName));
            }

            using var client = new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = _outboxPath
            };
            client.Send(mail);
        }
        finally
        {
            foreach (var stream in openedStreams)
            {
                stream.Dispose();
            }
        }

        _logger.LogInformation("Queued email to {Recipient} in {Outbox}.", message.To, _outboxPath);
        return Task.CompletedTask;
    }
}

public static class ComplaintEmailFactory
{
    public static EmailMessage CreateCouncilNotification(Complaint complaint)
    {
        var body = $"""
            A new citizen complaint has been routed to your council.

            Service request number : {complaint.RequestNumber}
            Category               : {complaint.Category}
            Reported at (UTC)      : {complaint.CreatedAtUtc.UtcDateTime:yyyy-MM-dd HH:mm}
            Geo location           : {complaint.Latitude.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}, {complaint.Longitude.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}
            Map                    : https://www.openstreetmap.org/?mlat={complaint.Latitude.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}&mlon={complaint.Longitude.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}
            Location notes         : {complaint.LocationDescription ?? "(none)"}
            Reported by            : {complaint.ReporterName} <{complaint.ReporterEmail}> {complaint.ReporterPhone}
            Photographs attached   : {complaint.Photos.Count}

            Description
            -----------
            {complaint.Description}
            """;

        return new EmailMessage(
            complaint.CouncilEmail,
            $"[{complaint.RequestNumber}] {complaint.Category} reported by a citizen",
            body,
            complaint.Photos.Select(p => p.StoredFileName).ToList());
    }

    public static EmailMessage CreateCitizenAcknowledgement(Complaint complaint)
    {
        var body = $"""
            Thank you for reporting an issue to your local council.

            Your service request number is {complaint.RequestNumber}.
            Keep this number to track progress on the "Track a request" page.

            Category : {complaint.Category}
            Routed to: {complaint.CouncilName} ({complaint.CouncilEmail})
            Status   : {complaint.Status}
            """;

        return new EmailMessage(
            complaint.ReporterEmail,
            $"We received your report - {complaint.RequestNumber}",
            body,
            Array.Empty<string>());
    }
}
