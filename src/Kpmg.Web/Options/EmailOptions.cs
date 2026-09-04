namespace Kpmg.Web.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "no-reply@citizen-services.example.gov";

    public string FromDisplayName { get; set; } = "Citizen Services";

    /// <summary>
    /// Directory where outgoing messages are written as .eml files. The sample uses a
    /// pickup directory so the app runs without SMTP credentials; point a real SMTP
    /// relay at these files (or replace <c>IEmailSender</c>) for production use.
    /// </summary>
    public string OutboxPath { get; set; } = "App_Data/outbox";
}
