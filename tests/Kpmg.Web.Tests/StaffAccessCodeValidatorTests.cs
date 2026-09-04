using Kpmg.Web.Options;
using Kpmg.Web.Services;
using Microsoft.Extensions.Options;

namespace Kpmg.Web.Tests;

public class StaffAccessCodeValidatorTests
{
    private static StaffAccessCodeValidator CreateValidator(string? configuredCode) =>
        new(new StaticOptionsMonitor<StaffPortalOptions>(new StaffPortalOptions { AccessCode = configuredCode }));

    [Fact]
    public void IsValid_AcceptsTheConfiguredCode()
    {
        Assert.True(CreateValidator("team-code").IsValid("team-code"));
    }

    [Theory]
    [InlineData("Team-Code")]
    [InlineData("team-code-longer")]
    [InlineData("team")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_RejectsAnythingElse(string? supplied)
    {
        Assert.False(CreateValidator("team-code").IsValid(supplied));
    }

    [Fact]
    public void IsValid_RejectsEveryCodeWhenNoneIsConfigured()
    {
        Assert.False(CreateValidator(null).IsValid("team-code"));
    }
}
