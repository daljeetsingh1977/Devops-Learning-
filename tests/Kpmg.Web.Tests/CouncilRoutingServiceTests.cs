using Kpmg.Web.Options;
using Kpmg.Web.Services;
using Microsoft.Extensions.Options;

namespace Kpmg.Web.Tests;

public class CouncilRoutingServiceTests
{
    private static CouncilRoutingService CreateService(CouncilRoutingOptions options) =>
        new(new StaticOptionsMonitor<CouncilRoutingOptions>(options));

    private static CouncilRoutingOptions SampleOptions() => new()
    {
        FallbackCouncilName = "National Civic Support Desk",
        FallbackCouncilEmail = "civic-support@example.gov",
        Councils = new List<CouncilArea>
        {
            new()
            {
                Name = "Greater London Authority",
                Email = "highways@london.example.gov",
                MinLatitude = 51.28, MaxLatitude = 51.70, MinLongitude = -0.51, MaxLongitude = 0.33
            },
            new()
            {
                Name = "Westminster City Council",
                Email = "street-faults@westminster.example.gov",
                MinLatitude = 51.48, MaxLatitude = 51.54, MinLongitude = -0.19, MaxLongitude = -0.11
            }
        }
    };

    [Fact]
    public void ResolveCouncil_ReturnsCouncilCoveringTheLocation()
    {
        var route = CreateService(SampleOptions()).ResolveCouncil(51.60, 0.10);

        Assert.Equal("Greater London Authority", route.CouncilName);
        Assert.Equal("highways@london.example.gov", route.CouncilEmail);
        Assert.False(route.IsFallback);
    }

    [Fact]
    public void ResolveCouncil_PrefersTheMostSpecificAreaWhenBoundingBoxesOverlap()
    {
        var route = CreateService(SampleOptions()).ResolveCouncil(51.50, -0.14);

        Assert.Equal("Westminster City Council", route.CouncilName);
        Assert.Equal("street-faults@westminster.example.gov", route.CouncilEmail);
    }

    [Fact]
    public void ResolveCouncil_FallsBackWhenNoCouncilCoversTheLocation()
    {
        var route = CreateService(SampleOptions()).ResolveCouncil(-33.87, 151.21);

        Assert.True(route.IsFallback);
        Assert.Equal("civic-support@example.gov", route.CouncilEmail);
    }

    [Fact]
    public void ResolveCouncil_IgnoresCouncilsWithoutAnEmailAddress()
    {
        var options = SampleOptions();
        options.Councils[1].Email = string.Empty;

        var route = CreateService(options).ResolveCouncil(51.50, -0.14);

        Assert.Equal("Greater London Authority", route.CouncilName);
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
