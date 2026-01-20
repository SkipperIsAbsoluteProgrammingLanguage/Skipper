using System.Reflection;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.VM.Jit.Optimisations;
using Xunit;

namespace Skipper.VM.Tests.Jit.Optimizations;

public class EliminateDeadCodeLinearTests
{
    private static List<Instruction> Eliminate(List<Instruction> code)
    {
        var method = typeof(EliminateDeadCodeLinearOptimisation)
            .GetMethod("EliminateDeadCodeLinear", BindingFlags.Public | BindingFlags.Static);
        return (List<Instruction>)method!.Invoke(null, [code])!;
    }

    [Fact]
    public void DeadCodeAfterReturn_IsRemoved()
    {
        // Arrange
        // return 1;
        // return 2; // недостижимо
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN),
            new(OpCode.PUSH, 1),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Eliminate(code);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
    }

    [Fact]
    public void JumpTarget_MakesBlockReachable()
    {
        // Arrange
        // goto L1;
        // return 1;
        // L1: return 2;
        var code = new List<Instruction>
        {
            new(OpCode.JUMP, 3),
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN),
            new(OpCode.PUSH, 1),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Eliminate(code);

        // Assert
        Assert.Equal(3, optimized.Count);
        Assert.Equal(OpCode.JUMP, optimized[0].OpCode);
        Assert.Equal(1, Convert.ToInt32(optimized[0].Operands[0]));
        Assert.Equal(OpCode.PUSH, optimized[1].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[2].OpCode);
    }
}
