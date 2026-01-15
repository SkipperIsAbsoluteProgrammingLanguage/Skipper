using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class VmJitTests
{
    [Fact]
    public void Run_Jit_Add_TwoNumbers_ReturnsSum()
    {
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.ADD),
            new(OpCode.RETURN)
        ];

        var program = CreateProgram(code, [10, 20]);
        var (interp, jit) = RunBoth(program);

        Assert.Equal(30, interp.AsInt());
        Assert.Equal(30, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_JumpIfFalse_Branching()
    {
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.JUMP_IF_FALSE, 4),
            new(OpCode.PUSH, 1),
            new(OpCode.RETURN),
            new(OpCode.PUSH, 2),
            new(OpCode.RETURN)
        ];

        var program = CreateProgram(code, [false, 100, 200]);
        var (interp, jit) = RunBoth(program);

        Assert.Equal(200, interp.AsInt());
        Assert.Equal(200, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_Factorial_RecursionWorks()
    {
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

        var (interp, jit) = RunBoth(program);

        Assert.Equal(120, interp.AsInt());
        Assert.Equal(120, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_Array_ReadWrite()
    {
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

        var (interp, jit) = RunBoth(program);

        Assert.Equal(42, interp.AsInt());
        Assert.Equal(42, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_ObjectFields_ReadWrite()
    {
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

        var (interp, jit) = RunBoth(program);

        Assert.Equal(30, interp.AsInt());
        Assert.Equal(30, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_NativeRandom_ReturnsDeterministicValue()
    {
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.CALL_NATIVE, 2),
            new(OpCode.RETURN)
        ];

        var program = CreateProgram(code, [1]);
        var jit = RunJit(program);

        Assert.Equal(0, jit.AsInt());
    }

    [Fact]
    public void Run_Jit_Global_LoadStore()
    {
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

        var (interp, jit) = RunBoth(program);

        Assert.Equal(77, interp.AsInt());
        Assert.Equal(77, jit.AsInt());
    }

    private static (Value interp, Value jit) RunBoth(BytecodeProgram program)
    {
        var interp = new VirtualMachine(program, new RuntimeContext()).Run("main");
        var jit = new JitVirtualMachine(program, new RuntimeContext()).Run("main");
        return (interp, jit);
    }

    private static Value RunJit(BytecodeProgram program)
    {
        return new JitVirtualMachine(program, new RuntimeContext()).Run("main");
    }

    private static BytecodeProgram CreateProgram(List<Instruction> code, List<object>? constants = null)
    {
        BytecodeProgram program = new();
        if (constants != null)
        {
            program.ConstantPool.AddRange(constants);
        }

        BytecodeFunction func = new(0, "main", null!, [])
        {
            Code = code
        };

        program.Functions.Add(func);
        return program;
    }
}
