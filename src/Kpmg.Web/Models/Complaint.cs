using System.ComponentModel.DataAnnotations;

namespace Kpmg.Web.Models;

/// <summary>
/// A civic issue reported by a citizen (pothole, garbage, broken road, street light, ...).
/// </summary>
public class Complaint
{
    public int Id { get; set; }

    /// <summary>Human friendly tracking number handed back to the citizen.</summary>
    [MaxLength(32)]
    public string RequestNumber { get; set; } = string.Empty;

    public ComplaintCategory Category { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ReporterName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ReporterEmail { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? ReporterPhone { get; set; }

    /// <summary>Geo location captured by the browser at submission time.</summary>
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    [MaxLength(300)]
    public string? LocationDescription { get; set; }

    /// <summary>Council resolved from the geo location.</summary>
    [MaxLength(150)]
    public string CouncilName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CouncilEmail { get; set; } = string.Empty;

    public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;

    [MaxLength(2000)]
    public string? StaffNotes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>True once the routing email was handed to the mail transport.</summary>
    public bool CouncilNotified { get; set; }

    public List<ComplaintPhoto> Photos { get; set; } = new();
}
