using System.Reflection;
using Skipper.BaitCode.Objects;
using Xunit;

namespace Skipper.VM.Tests.Jit;

public class GlobalVariableJitTests
{
    [Fact]
    public void Run_Jit_ExecutesGlobalInitializers()
    {
        // Arrange
        const string code = """
                            int g = 5;
                            fn main() -> int {
                                return g + 1;
                            }
                            """;

        var compile = typeof(TestsHelpers).GetMethod("Compile", BindingFlags.NonPublic | BindingFlags.Static);
        var program = (BytecodeProgram)compile!.Invoke(null, new object[] { code })!;

        // Act
        var (interp, jit) = TestsHelpers.RunInterpretedAndJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(interp.AsInt(), jit.AsInt());
        Assert.Equal(6, jit.AsInt());
    }
}
