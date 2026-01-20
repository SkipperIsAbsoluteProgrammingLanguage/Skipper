using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.VM.Jit.Optimisations;
using Xunit;

namespace Skipper.VM.Tests.Jit.Optimizations;

public class OptimisationToolsTests
{
    [Fact]
    public void TryGetConstBool_ParsesNumericAndNull()
    {
        // Arrange
        var program = new BytecodeProgram();
        program.ConstantPool.Add(null!);
        program.ConstantPool.Add(0);
        program.ConstantPool.Add(2);
        program.ConstantPool.Add(0L);
        program.ConstantPool.Add(3L);
        program.ConstantPool.Add(0.0);
        program.ConstantPool.Add(0.25);

        // Act
        var nullResult = OptimisationTools.TryGetConstBool(program, 0, out var nullVal);
        var intZero = OptimisationTools.TryGetConstBool(program, 1, out var intZeroVal);
        var intNonZero = OptimisationTools.TryGetConstBool(program, 2, out var intNonZeroVal);
        var longZero = OptimisationTools.TryGetConstBool(program, 3, out var longZeroVal);
        var longNonZero = OptimisationTools.TryGetConstBool(program, 4, out var longNonZeroVal);
        var doubleZero = OptimisationTools.TryGetConstBool(program, 5, out var doubleZeroVal);
        var doubleNonZero = OptimisationTools.TryGetConstBool(program, 6, out var doubleNonZeroVal);

        // Assert
        Assert.True(nullResult);
        Assert.False(nullVal);
        Assert.True(intZero);
        Assert.False(intZeroVal);
        Assert.True(intNonZero);
        Assert.True(intNonZeroVal);
        Assert.True(longZero);
        Assert.False(longZeroVal);
        Assert.True(longNonZero);
        Assert.True(longNonZeroVal);
        Assert.True(doubleZero);
        Assert.False(doubleZeroVal);
        Assert.True(doubleNonZero);
        Assert.True(doubleNonZeroVal);
    }

    [Fact]
    public void TryFoldCmp_DoubleEpsilon_IsFalseForEqual()
    {
        // Arrange
        var left = 1.0;
        var right = 1.0 + (double.Epsilon / 2.0);

        // Act
        var ok = OptimisationTools.TryFoldCmp(OpCode.CMP_EQ, left, right, out var result);

        // Assert
        Assert.True(ok);
        Assert.True(result);
    }

    [Fact]
    public void IsJump_DetectsJumpOpcodes()
    {
        // Arrange & Act
        var isJump = OptimisationTools.IsJump(OpCode.JUMP);
        var isJumpTrue = OptimisationTools.IsJump(OpCode.JUMP_IF_TRUE);
        var isJumpFalse = OptimisationTools.IsJump(OpCode.JUMP_IF_FALSE);
        var isNotJump = OptimisationTools.IsJump(OpCode.ADD);

        // Assert
        Assert.True(isJump);
        Assert.True(isJumpTrue);
        Assert.True(isJumpFalse);
        Assert.False(isNotJump);
    }
}
