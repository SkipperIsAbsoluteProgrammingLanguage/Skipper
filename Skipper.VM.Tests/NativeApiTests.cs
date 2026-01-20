using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class NativeApiTests
{
    [Fact]
    public void VM_Print_WritesToConsole()
    {
        // Arrange
        const string code = """
                            fn main() {
                                print(12345);
                                print("TestMessage");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("12345", output);
        Assert.Contains("TestMessage", output);
    }

    [Fact]
    public void VM_Print_NoArgs_WritesEmptyString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                print();
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void VM_Println_WritesNewLine()
    {
        // Arrange
        const string code = """
                            fn main() {
                                println("Hello");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Equal("Hello" + Environment.NewLine, output);
    }

    [Fact]
    public void VM_Println_NoArgs_WritesNewLine()
    {
        // Arrange
        const string code = """
                            fn main() {
                                println();
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Equal(Environment.NewLine, output);
    }

    [Fact]
    public void Integration_PrintCalculationResult()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int a = 10;
                                int b = 20;
                                print(a + b);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("30", output);
    }

    [Fact]
    public void VM_Time_ReturnsValidTimestamp()
    {
        // Arrange
        const string code = "fn main() -> int { return time(); }";

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(ValueKind.Int, result.Kind);
        Assert.True(result.AsInt() >= 0);
    }

    [Fact]
    public void VM_Time_IncreasesDuringExecution()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                int start = time();
                                int sum = 0;
                                for (int i = 0; i < 1000; i = i + 1) { sum = sum + 1; }
                                int end = time();
                                return end - start;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.True(result.AsInt() >= 0);
    }

    [Fact]
    public void VM_Random_ReturnsValueInRange()
    {
        // Arrange
        const string code = "fn main() -> int { return random(10); }";

        // Act & Assert
        for (var i = 0; i < 50; i++)
        {
            var result = TestsHelpers.Run(code);
            Assert.InRange(result.AsInt(), 0, 9);
        }
    }

    [Fact]
    public void VM_Print_Booleans()
    {
        // Arrange
        const string code = """
                            fn main() {
                                print(true);
                                print(false);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("True", output);
        Assert.Contains("False", output);
    }

    [Fact]
    public void VM_Print_Doubles_And_Negatives()
    {
        // Arrange
        const string code = """
                            fn main() {
                                print(3.1415);
                                print(-100);
                                print(-0.5);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("3.1415", output);
        Assert.Contains("-100", output);
        Assert.Contains("-0.5", output);
    }

    [Fact]
    public void VM_Print_EmptyString()
    {
        // Arrange
        const string code = "fn main() { print(\"\"); }";

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.NotNull(output);
    }

    [Fact]
    public void VM_NativeCall_StackBalance()
    {
        // Arrange
        const string code = """
                            fn main() {
                                for (int i = 0; i < 100; i = i + 1) {
                                    print(i);
                                }
                                print("Done");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("99", output);
        Assert.Contains("Done", output);
    }

    [Fact]
    public void VM_Print_Object_DoesNotCrash()
    {
        // Arrange
        const string code = """
                            class Box { int x; }
                            fn main() {
                                Box b = new Box();
                                print(b);
                                print("Done");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("Done", output);
    }

    [Fact]
    public void VM_StringConcatenation_PrintsCombinedString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                print("Hello " + "World");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("Hello World", output);
    }

    [Fact]
    public void VM_StringConcat_WithInt_PrintsCombinedString()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int start = 2;
                                int end = 5;
                                print("App time: " + (end - start));
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("App time: 3", output);
    }
}
