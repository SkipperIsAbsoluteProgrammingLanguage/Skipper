using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Skipper.Runtime;
using Xunit;

namespace Skipper.VM.Tests.Jit;

public class VmJitOpcodeTests
{
    [Fact]
    public void Run_Jit_DupSwapPop_Works()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.SWAP),
            new(OpCode.DUP),
            new(OpCode.POP),
            new(OpCode.ADD),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [10, 20]);
        var (interp, jit) = TestsHelpers.RunInterpretedAndJit(program);

        // Assert
        Assert.Equal(30, interp.AsInt());
        Assert.Equal(30, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_LogicalOps_Works()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // true
            new(OpCode.PUSH, 1), // false
            new(OpCode.AND), // false
            new(OpCode.NOT), // true
            new(OpCode.PUSH, 1), // false
            new(OpCode.OR), // true
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [true, false]);
        var (interp, jit) = TestsHelpers.RunInterpretedAndJit(program);

        // Assert
        Assert.True(interp.AsBool());
        Assert.True(jit.AsBool());
    }

    [Fact]
    public void Run_Jit_Comparisons_Work()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // 5
            new(OpCode.PUSH, 0), // 5
            new(OpCode.CMP_EQ), // true
            new(OpCode.PUSH, 1), // 3
            new(OpCode.PUSH, 2), // 4
            new(OpCode.CMP_LT), // true
            new(OpCode.AND),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [5, 3, 4]);
        var (interp, jit) = TestsHelpers.RunInterpretedAndJit(program);

        // Assert
        Assert.True(interp.AsBool());
        Assert.True(jit.AsBool());
    }

    [Fact]
    public void Run_Jit_Jump_Works()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.JUMP, 3),
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN),
            new(OpCode.PUSH, 1),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [10, 99]);
        var (interp, jit) = TestsHelpers.RunInterpretedAndJit(program);

        // Assert
        Assert.Equal(99, interp.AsInt());
        Assert.Equal(99, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_DoubleArithmetic_Works()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.ADD),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [1.5, 2.5]);
        var (interp, jit) = TestsHelpers.RunInterpretedAndJit(program);

        // Assert
        Assert.Equal(4.0, interp.AsDouble(), 4);
        Assert.Equal(4.0, jit.AsDouble(), 4);
    }

    [Fact]
    public void Run_Jit_StringConcat_Works()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.ADD),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, ["a", "b"]);

        var interpRuntime = new RuntimeContext();
        var interpVm = new JitVirtualMachine(program, interpRuntime, int.MaxValue);
        var interpValue = interpVm.Run("main");

        var jitRuntime = new RuntimeContext();
        var jitVm = new JitVirtualMachine(program, jitRuntime, hotThreshold: 1);
        var jitValue = jitVm.Run("main");

        // Assert
        var interpStr = interpRuntime.ReadStringFromMemory(interpValue.AsObject());
        var jitStr = jitRuntime.ReadStringFromMemory(jitValue.AsObject());

        Assert.Equal("ab", interpStr);
        Assert.Equal("ab", jitStr);
    }

    [Fact]
    public void Run_Jit_CallMethod_Works()
    {
        // Arrange
        BytecodeProgram program = new();
        program.ConstantPool.Add(7);

        BytecodeClass cls = new(0, "Point");
        cls.Fields.Add("x", new BytecodeClassField(fieldId: 0, type: new PrimitiveType("int")));
        cls.Methods.Add("get", 0);
        program.Classes.Add(cls);

        BytecodeFunction method = new(0, "get", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        BytecodeFunction main = new(1, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.NEW_OBJECT, 0),
                new Instruction(OpCode.CALL_METHOD, 0, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(method);
        program.Functions.Add(main);

        // Act
        var (interp, jit) = TestsHelpers.RunInterpretedAndJit(program);

        // Assert
        Assert.Equal(7, interp.AsInt());
        Assert.Equal(7, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_DivideByZero_Throws()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.DIV),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [1, 0]);

        // Assert
        Assert.Throws<DivideByZeroException>(()
            => new JitVirtualMachine(program, new RuntimeContext(), hotThreshold: 1).Run("main"));
        Assert.Throws<DivideByZeroException>(()
            => new JitVirtualMachine(program, new RuntimeContext(), int.MaxValue).Run("main"));
    }

    [Fact]
    public void Run_Jit_ArrayOutOfRange_Throws()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.NEW_ARRAY),
            new(OpCode.STORE_LOCAL, 0, 0),

            new(OpCode.LOAD_LOCAL, 0, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.GET_ELEMENT),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [1, 5]);

        // Assert
        Assert.Throws<IndexOutOfRangeException>(()
            => new JitVirtualMachine(program, new RuntimeContext(), hotThreshold: 1).Run("main"));
        Assert.Throws<IndexOutOfRangeException>(()
            => new JitVirtualMachine(program, new RuntimeContext(), int.MaxValue).Run("main"));
    }

    [Fact]
    public void Run_Jit_PopEmpty_DoesNotThrow()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.POP),
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [7]);
        var (interp, jit) = TestsHelpers.RunInterpretedAndJit(program);

        // Assert
        Assert.Equal(7, interp.AsInt());
        Assert.Equal(7, jit.AsInt());
    }
}