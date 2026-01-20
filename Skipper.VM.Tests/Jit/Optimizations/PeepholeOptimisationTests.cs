using System.Reflection;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.VM.Jit.Optimisations;
using Xunit;

namespace Skipper.VM.Tests.Jit.Optimizations;

public class PeepholeOptimisationTests
{
    private static List<Instruction> Peephole(List<Instruction> code, BytecodeProgram program)
    {
        var method = typeof(PeepholeOptimisation)
            .GetMethod("PeepholeOptimize", BindingFlags.Public | BindingFlags.Static);
        return (List<Instruction>)method!.Invoke(null, [code, program])!;
    }

    [Fact]
    public void Peephole_Removes_PushPop()
    {
        // Arrange
        // 1;
        // return 1;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.POP),
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
    }

    [Fact]
    public void Peephole_Removes_DupPop()
    {
        // Arrange
        // return 1; // DUP/POP не меняет стек
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.DUP),
            new(OpCode.POP),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
    }

    [Fact]
    public void Peephole_Folds_Int_Add()
    {
        // Arrange
        // return 1 + 2;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(2);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.ADD),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
        Assert.Equal(3, program.ConstantPool[^1]);
    }

    [Fact]
    public void Peephole_Folds_Long_Mul()
    {
        // Arrange
        // return 2L * 3L;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(2L);
        program.ConstantPool.Add(3L);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.MUL),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
        Assert.Equal(6L, program.ConstantPool[^1]);
    }

    [Fact]
    public void Peephole_Folds_Bool_And()
    {
        // Arrange
        // return true && false;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(true);
        program.ConstantPool.Add(false);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.AND),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
        Assert.Equal(false, program.ConstantPool[^1]);
    }

    [Fact]
    public void Peephole_Folds_Bool_Or()
    {
        // Arrange
        // return false || true;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(false);
        program.ConstantPool.Add(true);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.OR),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
        Assert.Equal(true, program.ConstantPool[^1]);
    }

    [Fact]
    public void Peephole_Folds_Char_Add()
    {
        // Arrange
        // return 'a' + 1;
        var program = new BytecodeProgram();
        program.ConstantPool.Add('a');
        program.ConstantPool.Add((char)1);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.ADD),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
        Assert.Equal('b', program.ConstantPool[^1]);
    }

    [Fact]
    public void Peephole_Folds_Double_Mod()
    {
        // Arrange
        // return 5.5 % 2.0;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(5.5);
        program.ConstantPool.Add(2.0);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.MOD),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
        Assert.Equal(1.5, program.ConstantPool[^1]);
    }

    [Fact]
    public void Peephole_DoesNotFold_MixedTypes()
    {
        // Arrange
        // return 1 + 2L; // разные типы
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(2L);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.ADD),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(4, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.PUSH, optimized[1].OpCode);
        Assert.Equal(OpCode.ADD, optimized[2].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[3].OpCode);
    }

    [Fact]
    public void Peephole_DoesNotFold_DivideByZero()
    {
        // Arrange
        // return 1 / 0;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(0);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.DIV),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(4, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.PUSH, optimized[1].OpCode);
        Assert.Equal(OpCode.DIV, optimized[2].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[3].OpCode);
    }

    [Fact]
    public void Peephole_Removes_LoadStore_SameLocal()
    {
        // Arrange
        // x; // чтение и тут же запись в тот же слот
        // return 1;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        var code = new List<Instruction>
        {
            new(OpCode.LOAD_LOCAL, 0, 0),
            new(OpCode.STORE_LOCAL, 0, 0),
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(2, optimized.Count);
        Assert.Equal(OpCode.PUSH, optimized[0].OpCode);
        Assert.Equal(OpCode.RETURN, optimized[1].OpCode);
    }

    [Fact]
    public void Peephole_Rewrites_JumpToJump()
    {
        // Arrange
        // goto L1;
        // L1: goto L2;
        // return 1;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        var code = new List<Instruction>
        {
            new(OpCode.JUMP, 1),
            new(OpCode.JUMP, 3),
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(OpCode.JUMP, optimized[0].OpCode);
        Assert.Equal(3, Convert.ToInt32(optimized[0].Operands[0]));
    }

    [Fact]
    public void Peephole_FixesJumpTargets_AfterRemoval()
    {
        // Arrange
        // 1;
        // goto L1;
        // return 1;
        // L1: return 2;
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(2);
        var code = new List<Instruction>
        {
            new(OpCode.PUSH, 0),
            new(OpCode.POP),
            new(OpCode.JUMP, 5),
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN),
            new(OpCode.PUSH, 1),
            new(OpCode.RETURN)
        };

        // Act
        var optimized = Peephole(code, program);

        // Assert
        Assert.Equal(OpCode.JUMP, optimized[0].OpCode);
        Assert.Equal(3, Convert.ToInt32(optimized[0].Operands[0]));
    }
}
