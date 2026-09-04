namespace Kpmg.Web.Options;

public class StaffPortalOptions
{
    public const string SectionName = "StaffPortal";

    /// <summary>
    /// Shared access code used by the customer service team to sign in to the portal.
    /// Supply it through configuration (for example the <c>StaffPortal__AccessCode</c>
    /// environment variable or user secrets). When it is not configured, a random code is
    /// generated at start-up and written to the application log.
    /// </summary>
    public string? AccessCode { get; set; }
}
