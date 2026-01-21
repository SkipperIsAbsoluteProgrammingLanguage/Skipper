using Skipper.Runtime;
using Skipper.VM.Interpreter;
using Skipper.VM.Jit;
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

    [Theory]
    [InlineData("", "true", "x=true")]
    [InlineData("", "1.5", "x=1.5")]
    [InlineData("", "'a'", "x=a")]
    [InlineData("long l = 7;", "l", "x=7")]
    [InlineData("int i = 3;", "i", "x=3")]
    public void StringPlusScalar_PrintsSameResult(string prefix, string expr, string expected)
    {
        // Arrange
        var code = "fn main() {\n"
            + (string.IsNullOrEmpty(prefix) ? string.Empty : prefix + "\n")
            + "println(\"x=\" + " + expr + ");\n"
            + "}\n";
        var program = TestsHelpers.Compile(code);

        // Act
        var interpOutput = TestsHelpers.CaptureOutput(() =>
        {
            var vm = new VirtualMachine(program, new RuntimeContext());
            vm.Run("main");
        });
        var jitOutput = TestsHelpers.CaptureOutput(() =>
        {
            var vm = new JitVirtualMachine(program, new RuntimeContext(), hotThreshold: 1);
            vm.Run("main");
        });

        // Assert Ч нормализуем line endings
        Assert.Equal(expected, interpOutput.TrimEnd());
        Assert.Equal(expected, jitOutput.TrimEnd());
    }
}