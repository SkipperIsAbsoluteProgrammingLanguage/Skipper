using Xunit;

namespace Skipper.VM.Tests;

public class GlobalVariableTests
{
    [Fact]
    public void GlobalInitializer_RunsBeforeMain()
    {
        // Arrange
        const string code = """
                            int g = 5;
                            fn main() -> int {
                                return g;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(5, result.AsInt());
    }

    [Fact]
    public void GlobalInitializer_UsesPreviousGlobal()
    {
        // Arrange
        const string code = """
                            int a = 2;
                            int b = a + 3;
                            fn main() -> int {
                                return b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(5, result.AsInt());
    }

    [Fact]
    public void GlobalInitializer_CallsFunction()
    {
        // Arrange
        const string code = """
                            fn add(int a, int b) -> int { return a + b; }
                            int g = add(2, 3);
                            fn main() -> int {
                                return g;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(5, result.AsInt());
    }

    [Fact]
    public void GlobalInitializer_CallsFunctionDeclaredLater()
    {
        // Arrange
        const string code = """
                            int g = add(2, 3);
                            fn add(int a, int b) -> int { return a + b; }
                            fn main() -> int {
                                return g;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(5, result.AsInt());
    }
}
