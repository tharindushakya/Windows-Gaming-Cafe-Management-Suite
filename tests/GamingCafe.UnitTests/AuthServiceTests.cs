using Xunit;
using FluentAssertions;

namespace GamingCafe.UnitTests;

public class AuthServiceTests
{
    [Fact]
    public void Sample_TwoPlusTwo_ReturnsFour()
    {
        // simple smoke unit test to validate test project wiring
        var result = 2 + 2;
        result.Should().Be(4);
    }
}
