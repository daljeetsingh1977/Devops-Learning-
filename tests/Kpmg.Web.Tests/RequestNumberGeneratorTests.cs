using System.Text.RegularExpressions;
using Kpmg.Web.Services;

namespace Kpmg.Web.Tests;

public class RequestNumberGeneratorTests
{
    [Fact]
    public void Generate_UsesTrackableFormatContainingTheSubmissionDate()
    {
        var generator = new RequestNumberGenerator();

        var number = generator.Generate(new DateTimeOffset(2026, 9, 4, 10, 30, 0, TimeSpan.Zero));

        Assert.Matches(new Regex("^CSR-20260904-[A-Z2-9]{6}$"), number);
    }

    [Fact]
    public void Generate_ProducesDistinctNumbers()
    {
        var generator = new RequestNumberGenerator();
        var timestamp = DateTimeOffset.UtcNow;

        var numbers = Enumerable.Range(0, 200).Select(_ => generator.Generate(timestamp)).ToHashSet();

        Assert.True(numbers.Count > 190, "Request numbers should be practically unique.");
    }
}
