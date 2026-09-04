using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kpmg.Web.Tests;

public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<HealthStatus>();
        Assert.NotNull(health);
        Assert.Equal("Healthy", health!.Status);
        Assert.Equal("Kpmg.Web", health.Application);
    }

    [Fact]
    public async Task About_endpoint_describes_the_kpmg_application()
    {
        var client = _factory.CreateClient();

        var about = await client.GetFromJsonAsync<AboutInfo>("/api/about");

        Assert.NotNull(about);
        Assert.Equal("KPMG", about!.Organization);
        Assert.Equal("Kpmg.Web", about.Application);
        Assert.False(string.IsNullOrWhiteSpace(about.Environment));
    }
}
