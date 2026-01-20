using System.Reflection;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Skipper.VM.Jit.Optimisations;
using Xunit;

namespace Skipper.VM.Tests.Jit.Optimizations;

public class BranchSimplificationTests
{
    private static List<Instruction> Simplify(BytecodeFunction func, BytecodeProgram program)
    {
        var method = typeof(SimplifyBranchOptimisation)
            .GetMethod("SimplifyBranches", BindingFlags.Public | BindingFlags.Static);
        return (List<Instruction>)method!.Invoke(null, [func, program])!;
    }

    [Fact]
    public void BranchSimplification_RewritesTakenJump()
    {
        // Arrange
        // if (true) { return 2; } else { return 1; }
        var program = new BytecodeProgram();
        program.ConstantPool.Add(true);
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(2);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("int"), [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.JUMP_IF_TRUE, 4),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.RETURN),
                new Instruction(OpCode.PUSH, 2),
                new Instruction(OpCode.RETURN)
            ]
        };

        // Act
        var simplified = Simplify(func, program);

        // Assert
        Assert.Equal(5, simplified.Count);
        Assert.Equal(OpCode.JUMP, simplified[0].OpCode);
        Assert.Equal(3, Convert.ToInt32(simplified[0].Operands[0]));
        Assert.Equal(OpCode.PUSH, simplified[1].OpCode);
        Assert.Equal(OpCode.RETURN, simplified[2].OpCode);
        Assert.Equal(OpCode.PUSH, simplified[3].OpCode);
        Assert.Equal(OpCode.RETURN, simplified[4].OpCode);
    }

    [Fact]
    public void BranchSimplification_DropsUntakenJump()
    {
        // Arrange
        // if (true) { return 1; }
        // return 1;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(true);
        program.ConstantPool.Add(1);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("int"), [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.JUMP_IF_FALSE, 3),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.RETURN)
            ]
        };

        // Act
        var simplified = Simplify(func, program);

        // Assert
        Assert.Equal(2, simplified.Count);
        Assert.Equal(OpCode.PUSH, simplified[0].OpCode);
        Assert.Equal(OpCode.RETURN, simplified[1].OpCode);
    }

    [Fact]
    public void BranchSimplification_PreservesNonConst()
    {
        // Arrange
        // if (x) { return 1; }
        // return 1;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(new object());
        program.ConstantPool.Add(1);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("int"), [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.JUMP_IF_TRUE, 3),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.RETURN)
            ]
        };

        // Act
        var simplified = Simplify(func, program);

        // Assert
        Assert.Equal(func.Code.Count, simplified.Count);
        for (var i = 0; i < func.Code.Count; i++)
        {
            Assert.Equal(func.Code[i].OpCode, simplified[i].OpCode);
            Assert.Equal(func.Code[i].Operands.Count, simplified[i].Operands.Count);
            for (var op = 0; op < func.Code[i].Operands.Count; op++)
            {
                Assert.Equal(func.Code[i].Operands[op], simplified[i].Operands[op]);
            }
        }
    }

    [Fact]
    public void BranchSimplification_FixesJumpTargets_AfterRemoval()
    {
        // Arrange
        // if (true) { /* no-op */ }
        // if (cond) { return 10; }
        // return 1;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(true);
        program.ConstantPool.Add(10);
        program.ConstantPool.Add(99);
        program.ConstantPool.Add(1);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("int"), [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.JUMP_IF_FALSE, 5),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.JUMP, 6),
                new Instruction(OpCode.PUSH, 2),
                new Instruction(OpCode.PUSH, 3),
                new Instruction(OpCode.RETURN)
            ]
        };

        // Act
        var simplified = Simplify(func, program);

        // Assert
        Assert.Equal(5, simplified.Count);
        Assert.Equal(OpCode.JUMP, simplified[1].OpCode);
        Assert.Equal(4, Convert.ToInt32(simplified[1].Operands[0]));
    }

    [Fact]
    public void BranchSimplification_Folds_ConstCompare_IntoJump()
    {
        // Arrange
        // if (1 < 2) { return 1; } else { return 0; }
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(2);
        program.ConstantPool.Add(0);
        program.ConstantPool.Add(1);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("int"), [])
        {
            Code =
            [
                new(OpCode.PUSH, 0),
                new(OpCode.PUSH, 1),
                new(OpCode.CMP_LT),
                new(OpCode.JUMP_IF_TRUE, 6),
                new(OpCode.PUSH, 2),
                new(OpCode.RETURN),
                new(OpCode.PUSH, 3),
                new(OpCode.RETURN)
            ]
        };

        // Act
        var simplified = Simplify(func, program);

        // Assert
        Assert.Equal(OpCode.JUMP, simplified[0].OpCode);
        Assert.Equal(3, Convert.ToInt32(simplified[0].Operands[0]));
        Assert.Equal(OpCode.PUSH, simplified[1].OpCode);
        Assert.Equal(OpCode.RETURN, simplified[2].OpCode);
        Assert.Equal(OpCode.PUSH, simplified[3].OpCode);
        Assert.Equal(OpCode.RETURN, simplified[4].OpCode);
    }

    [Fact]
    public void BranchSimplification_Removes_ConstCompare_FalseJump()
    {
        // Arrange
        // if (2 < 1) { return 1; }
        // return 0;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(2);
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(0);
        program.ConstantPool.Add(1);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("int"), [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.CMP_LT),
                new Instruction(OpCode.JUMP_IF_TRUE, 6),
                new Instruction(OpCode.PUSH, 2),
                new Instruction(OpCode.RETURN),
                new Instruction(OpCode.PUSH, 3),
                new Instruction(OpCode.RETURN)
            ]
        };

        // Act
        var simplified = Simplify(func, program);

        // Assert
        Assert.Equal(4, simplified.Count);
        Assert.Equal(OpCode.PUSH, simplified[0].OpCode);
        Assert.Equal(2, Convert.ToInt32(simplified[0].Operands[0]));
        Assert.Equal(OpCode.RETURN, simplified[1].OpCode);
        Assert.Equal(OpCode.PUSH, simplified[2].OpCode);
        Assert.Equal(3, Convert.ToInt32(simplified[2].Operands[0]));
        Assert.Equal(OpCode.RETURN, simplified[3].OpCode);
    }
}
