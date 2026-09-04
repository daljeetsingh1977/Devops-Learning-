using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kpmg.Web.Tests;

public class LandingPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LandingPageTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Root_returns_the_kpmg_landing_page()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("KPMG", html);
        Assert.Contains("Welcome to KPMG", html);
    }

    [Fact]
    public async Task Stylesheet_is_served_from_wwwroot()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/css/site.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unknown_path_returns_not_found()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
