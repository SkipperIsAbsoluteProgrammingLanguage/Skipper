using Skipper.BaitCode.Objects.Instructions;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class NativeApiTests
{
    [Fact]
    public void Generator_Print_EmitsCallNative0()
    {
        // Arrange
        const string code = "fn main() { print(\"Hello\"); }";

        // Act
        var program = TestHelpers.Generate(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        // Assert
        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(0, Convert.ToInt32(instr.Operands[0]));
    }

    [Fact]
    public void Generator_Print_NoArgs_EmitsCallNative0()
    {
        // Arrange
        const string code = "fn main() { print(); }";

        // Act
        var program = TestHelpers.Generate(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        // Assert
        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(0, Convert.ToInt32(instr.Operands[0]));
    }

    [Fact]
    public void Generator_Time_EmitsCallNative1()
    {
        // Arrange
        const string code = "fn main() { int t = time(); }";

        // Act
        var program = TestHelpers.Generate(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        // Assert
        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(1, Convert.ToInt32(instr.Operands[0]));
    }

    [Fact]
    public void Generator_Println_EmitsCallNative3()
    {
        // Arrange
        const string code = "fn main() { println(\"Hello\"); }";

        // Act
        var program = TestHelpers.Generate(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        // Assert
        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(3, Convert.ToInt32(instr.Operands[0]));
    }

    [Fact]
    public void Generator_Println_NoArgs_EmitsCallNative3()
    {
        // Arrange
        const string code = "fn main() { println(); }";

        // Act
        var program = TestHelpers.Generate(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        // Assert
        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(3, Convert.ToInt32(instr.Operands[0]));
    }

    [Fact]
    public void Generator_Random_EmitsCallNative2()
    {
        // Arrange
        const string code = "fn main() { int r = random(100); }";

        // Act
        var program = TestHelpers.Generate(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        // Assert
        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(2, Convert.ToInt32(instr.Operands[0]));
    }
}
