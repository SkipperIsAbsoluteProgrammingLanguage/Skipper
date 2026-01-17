using Xunit;

namespace Skipper.VM.Tests;

public class StringConcatTests
{
    [Fact]
    public void StringPlusDouble_WritesInvariantString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                double a = 1.5;
                                print("v=" + a);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("v=1.5", output);
    }

    [Fact]
    public void DoublePlusString_WritesInvariantString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                double a = 1.5;
                                print(a + "ms");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("1.5ms", output);
    }
}
