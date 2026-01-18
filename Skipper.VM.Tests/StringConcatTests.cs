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

    [Fact]
    public void StringPlusBool_WritesInvariantString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                bool a = true;
                                print("So " + a);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("So True", output);
    }

    [Fact]
    public void BoolPlusString_WritesInvariantString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                bool a = true;
                                print(a + " that");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("True that", output);
    }
    
    [Fact]
    public void StringPlusChar_WritesInvariantString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                char a = 'i';
                                print("Hi" + a);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("Hii", output);
    }

    [Fact]
    public void CharPlusString_WritesInvariantString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                char a = 'H';
                                print(a + "i!");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("Hi!", output);
    }
}
