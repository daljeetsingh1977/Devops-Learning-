namespace Kpmg.Web.Options;

/// <summary>
/// Configuration that maps a geographical area to the local council mailbox
/// that should receive complaints raised inside that area.
/// </summary>
public class CouncilRoutingOptions
{
    public const string SectionName = "CouncilRouting";

    /// <summary>Mailbox used when no configured council area covers the reported location.</summary>
    public string FallbackCouncilName { get; set; } = "National Civic Support Desk";

    public string FallbackCouncilEmail { get; set; } = "civic-support@example.gov";

    public List<CouncilArea> Councils { get; set; } = new();
}

/// <summary>A council and the latitude/longitude bounding box it is responsible for.</summary>
public class CouncilArea
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public double MinLatitude { get; set; }

    public double MaxLatitude { get; set; }

    public double MinLongitude { get; set; }

    public double MaxLongitude { get; set; }

    public bool Contains(double latitude, double longitude) =>
        latitude >= MinLatitude && latitude <= MaxLatitude &&
        longitude >= MinLongitude && longitude <= MaxLongitude;

    /// <summary>Bounding box area, used to prefer the most specific council when boxes overlap.</summary>
    public double AreaSize => Math.Abs(MaxLatitude - MinLatitude) * Math.Abs(MaxLongitude - MinLongitude);
}
