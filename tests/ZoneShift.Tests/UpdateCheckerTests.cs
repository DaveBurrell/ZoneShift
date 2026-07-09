using TimezoneConverter.Services;
using Xunit;

namespace ZoneShift.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [InlineData("1.3.0", "1.2.0", 1)]   // a newer
    [InlineData("1.2.0", "1.3.0", -1)]  // a older
    [InlineData("1.3.0", "1.3.0", 0)]
    [InlineData("v1.3.0", "1.3.0", 0)]
    public void CompareVersions_orders_semver(string a, string b, int expectedSign)
    {
        var cmp = UpdateChecker.CompareVersions(a, b);
        Assert.Equal(Math.Sign(expectedSign), Math.Sign(cmp));
    }
}
