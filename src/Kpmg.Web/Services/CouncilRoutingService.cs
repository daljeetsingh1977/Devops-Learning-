using Kpmg.Web.Options;
using Microsoft.Extensions.Options;

namespace Kpmg.Web.Services;

public record CouncilRoute(string CouncilName, string CouncilEmail, bool IsFallback);

public interface ICouncilRoutingService
{
    /// <summary>Resolves the council mailbox responsible for the supplied geo location.</summary>
    CouncilRoute ResolveCouncil(double latitude, double longitude);
}

public class CouncilRoutingService : ICouncilRoutingService
{
    private readonly IOptionsMonitor<CouncilRoutingOptions> _options;

    public CouncilRoutingService(IOptionsMonitor<CouncilRoutingOptions> options)
    {
        _options = options;
    }

    public CouncilRoute ResolveCouncil(double latitude, double longitude)
    {
        var options = _options.CurrentValue;

        var match = options.Councils
            .Where(c => !string.IsNullOrWhiteSpace(c.Email) && c.Contains(latitude, longitude))
            // When bounding boxes overlap, the smallest (most specific) one wins.
            .OrderBy(c => c.AreaSize)
            .FirstOrDefault();

        return match is null
            ? new CouncilRoute(options.FallbackCouncilName, options.FallbackCouncilEmail, true)
            : new CouncilRoute(match.Name, match.Email, false);
    }
}
