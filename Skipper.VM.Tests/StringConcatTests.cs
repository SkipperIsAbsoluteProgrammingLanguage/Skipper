using Skipper.Runtime;
using Skipper.VM.Interpreter;
using Skipper.VM.Jit;
using Xunit;

namespace Skipper.VM.Tests;

public class StringConcatTests
{
    [Theory]
    [InlineData("", "true", "x=true\n")]
    [InlineData("", "1.5", "x=1.5\n")]
    [InlineData("", "'a'", "x=a\n")]
    [InlineData("long l = 7;", "l", "x=7\n")]
    [InlineData("int i = 3;", "i", "x=3\n")]
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

        // Assert
        Assert.Equal(expected, interpOutput);
        Assert.Equal(expected, jitOutput);
    }
}
