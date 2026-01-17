using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Jit;
using Xunit;

namespace Skipper.VM.Tests.Jit;

public class VmJitTests
{
    [Fact]
    public void Run_Jit_Add_TwoNumbers_ReturnsSum()
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
        var program = TestsHelpers.CreateProgram(code, [10, 20]);
        var (jit, _) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(30, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_Long_Add_ReturnsLong()
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
        var program = TestsHelpers.CreateProgram(code, [9223372036854775807L, 1L]);
        var (jit, _) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(ValueKind.Long, jit.Kind);
        Assert.Equal(-9223372036854775808L, jit.AsLong());
    }

    [Fact]
    public void Run_Jit_JumpIfFalse_Branching()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.JUMP_IF_FALSE, 4),
            new(OpCode.PUSH, 1),
            new(OpCode.RETURN),
            new(OpCode.PUSH, 2),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [false, 100, 200]);
        var (jit, _) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(200, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_Factorial_RecursionWorks()
    {
        // Arrange
        BytecodeProgram program = new();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(5);

        var paramsFact = new List<BytecodeFunctionParameter> { new("n", null!) };

        BytecodeFunction factFunc = new(0, "fact", null!, paramsFact)
        {
            Code =
            [
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.CMP_LE),
                new Instruction(OpCode.JUMP_IF_FALSE, 6),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.RETURN),
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.SUB),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.MUL),
                new Instruction(OpCode.RETURN)
            ]
        };

        BytecodeFunction mainFunc = new(1, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(factFunc);
        program.Functions.Add(mainFunc);

        // Act
        var (jit, _) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(120, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_Array_ReadWrite()
    {
        // Arrange
        BytecodeProgram program = new();
        program.ConstantPool.Add(5);
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(42);

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.NEW_ARRAY),
                new Instruction(OpCode.STORE_LOCAL, 0, 0),

                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.PUSH, 2),
                new Instruction(OpCode.SET_ELEMENT),

                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.GET_ELEMENT),

                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(func);

        // Act
        var (jit, _) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(42, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_ObjectFields_ReadWrite()
    {
        // Arrange
        BytecodeProgram program = new();
        BytecodeClass cls = new(0, "Point");
        cls.Fields.Add("x", new BytecodeClassField(fieldId: 0, type: new PrimitiveType("int")));
        cls.Fields.Add("y", new BytecodeClassField(fieldId: 1, type: new PrimitiveType("int")));
        program.Classes.Add(cls);

        program.ConstantPool.Add(10);
        program.ConstantPool.Add(20);

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.NEW_OBJECT, 0),
                new Instruction(OpCode.STORE_LOCAL, 0, 0),

                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.SET_FIELD, 0, 0),

                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.SET_FIELD, 0, 1),

                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.GET_FIELD, 0, 0),
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.GET_FIELD, 0, 1),

                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(func);

        // Act
        var (jit, _) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(30, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_NativeRandom_ReturnsDeterministicValue()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.CALL_NATIVE, 2),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [1]);
        var (jit, _) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(0, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_Global_LoadStore()
    {
        // Arrange
        BytecodeProgram program = new();
        program.ConstantPool.Add(77);
        program.Globals.Add(new BytecodeVariable(0, "g", new PrimitiveType("int")));

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.STORE_GLOBAL, 0),
                new Instruction(OpCode.LOAD_GLOBAL, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(func);

        // Act
        var (jit, _) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(77, jit.AsInt());
    }
    
    [Fact]
    public void Run_MissingEntryPoint_Throws()
    {
        // Arrange
        var program = new BytecodeProgram();
        var vm = new JitVirtualMachine(program, new RuntimeContext());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => vm.Run("main"));
        Assert.Contains("not found", ex.Message);
    }
}
