using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class IncrementDecrementTests
{
    [Fact]
    public void PostfixIncrement_ReturnsOldValue()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                int a = 1;
                                int b = a++;
                                return b * 10 + a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(ValueKind.Int, result.Kind);
        Assert.Equal(12, result.AsInt());
    }

    [Fact]
    public void PrefixIncrement_ReturnsNewValue()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                int a = 1;
                                int b = ++a;
                                return b * 10 + a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(22, result.AsInt());
    }

    [Fact]
    public void Postfix_Decrement_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                int a = 3;
                                int b = a--;
                                return b * 10 + a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(32, result.AsInt());
    }

    [Fact]
    public void Increment_Precedence_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                int a = 1;
                                int b = a++ + 2 * 3;
                                return b * 10 + a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(72, result.AsInt());
    }

    [Fact]
    public void Increment_ArrayElement_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                int[] a = new int[1];
                                a[0] = 5;
                                int b = a[0]++;
                                return b * 10 + a[0];
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(56, result.AsInt());
    }

    [Fact]
    public void Increment_Field_Works()
    {
        // Arrange
        const string code = """
                            class Box { int x; }
                            fn main() -> int {
                                Box b = new Box();
                                b.x = 1;
                                ++b.x;
                                return b.x;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(2, result.AsInt());
    }
}
