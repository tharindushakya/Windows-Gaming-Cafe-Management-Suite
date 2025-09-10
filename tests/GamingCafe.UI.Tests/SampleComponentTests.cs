using Xunit;

namespace GamingCafe.UI.Tests;

public class SampleComponentTests
{
    [Fact]
    public void Sample_BasicAssertion_Works()
    {
        var greeting = "hello";
        Assert.Equal("hello", greeting);
    }
}
