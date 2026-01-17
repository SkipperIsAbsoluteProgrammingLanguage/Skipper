using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Skipper.Runtime;
using Skipper.VM.Interpreter;
using Xunit;

namespace Skipper.VM.Tests;

public class VmInterpreterCoverageTests
{
    [Fact]
    public void Interpreter_Trace_EmitsSteps()
    {
        // Arrange
        var program = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.RETURN)
            ],
            [1]
        );

        var runtime = new RuntimeContext();
        var vm = new VirtualMachine(program, runtime, trace: true);

        // Act
        var output = TestsHelpers.CaptureOutput(() => vm.Run("main"));

        // Assert
        Assert.Contains("[STEP]", output);
        Assert.Contains("Op: PUSH", output);
    }

    [Fact]
    public void Interpreter_GlobalStoreLoad_Works()
    {
        // Arrange
        var program = new BytecodeProgram();
        program.Globals.Add(new BytecodeVariable(0, "g", new PrimitiveType("int")));
        program.ConstantPool.Add(42);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("int"), [])
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
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(42, result.AsInt());
    }

    [Theory]
    [InlineData(OpCode.SUB, 5.5, 2.0, 3.5)]
    [InlineData(OpCode.MUL, 2.0, 3.0, 6.0)]
    [InlineData(OpCode.DIV, 5.0, 2.0, 2.5)]
    [InlineData(OpCode.MOD, 5.5, 2.0, 1.5)]
    public void Interpreter_DoubleArithmetic_Works(OpCode op, double left, double right, double expected)
    {
        // Arrange
        var program = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(op),
                new Instruction(OpCode.RETURN)
            ],
            [left, right]
        );

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(expected, result.AsDouble(), 10);
    }

    [Fact]
    public void Interpreter_LongSub_Works()
    {
        // Arrange
        var program = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.SUB),
                new Instruction(OpCode.RETURN)
            ],
            [10L, 3L]
        );

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(7L, result.AsLong());
    }

    [Fact]
    public void Interpreter_ModByZero_Throws()
    {
        // Arrange
        var program = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.MOD),
                new Instruction(OpCode.RETURN)
            ],
            [10, 0]
        );

        // Act & Assert
        Assert.Throws<DivideByZeroException>(() => TestsHelpers.Run(program));
    }

    [Fact]
    public void Interpreter_IntModulo_Works()
    {
        // Arrange
        var program = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.MOD),
                new Instruction(OpCode.RETURN)
            ],
            [9, 4]
        );

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(1, result.AsInt());
    }

    [Fact]
    public void Interpreter_Neg_Long_Works()
    {
        // Arrange
        var program = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.NEG),
                new Instruction(OpCode.RETURN)
            ],
            [5L]
        );

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(-5L, result.AsLong());
    }

    [Fact]
    public void Interpreter_Compare_NonNumeric_Works()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 1),
            new(OpCode.CMP_EQ),
            new(OpCode.PUSH, 0),
            new(OpCode.PUSH, 2),
            new(OpCode.CMP_NE),
            new(OpCode.ADD),
            new(OpCode.RETURN)
        ];

        var program = TestsHelpers.CreateProgram(code, [true, true, false]);

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(2, result.AsInt());
    }

    [Fact]
    public void Interpreter_JumpIfTrue_Branching()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.JUMP_IF_TRUE, 4),
            new(OpCode.PUSH, 1),
            new(OpCode.RETURN),
            new(OpCode.PUSH, 2),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [true, 1, 2]);
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(2, result.AsInt());
    }

    [Fact]
    public void Interpreter_JumpIfTrue_False_FallsThrough()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.JUMP_IF_TRUE, 4),
            new(OpCode.PUSH, 1),
            new(OpCode.RETURN),
            new(OpCode.PUSH, 2),
            new(OpCode.RETURN)
        ];

        var program = TestsHelpers.CreateProgram(code, [false, 1, 2]);

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(1, result.AsInt());
    }

    [Fact]
    public void Interpreter_NewObject_OutOfMemory_Throws()
    {
        // Arrange
        var program = new BytecodeProgram();
        var cls = new BytecodeClass(0, "Big");
        for (var i = 0; i < 200000; i++)
        {
            cls.Fields.Add("f" + i, new BytecodeClassField(i, new PrimitiveType("int")));
        }
        program.Classes.Add(cls);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("void"), [])
        {
            Code =
            [
                new Instruction(OpCode.NEW_OBJECT, 0),
                new Instruction(OpCode.RETURN)
            ]
        };
        program.Functions.Add(func);

        // Act & Assert
        Assert.Throws<OutOfMemoryException>(() => TestsHelpers.Run(program));
    }

    [Fact]
    public void Interpreter_NewObject_AfterGc_Succeeds()
    {
        // Arrange
        const int heapSize = 1024 * 1024;
        var runtime = new RuntimeContext(heapSize);
        _ = runtime.AllocateObject(heapSize - sizeof(long), 0);

        var program = new BytecodeProgram();
        var cls = new BytecodeClass(0, "Small");
        cls.Fields.Add("x", new BytecodeClassField(0, new PrimitiveType("int")));
        program.Classes.Add(cls);

        var func = new BytecodeFunction(0, "main", new PrimitiveType("void"), [])
        {
            Code =
            [
                new Instruction(OpCode.NEW_OBJECT, 0),
                new Instruction(OpCode.RETURN)
            ]
        };
        program.Functions.Add(func);

        var vm = new VirtualMachine(program, runtime);

        // Act & Assert
        _ = vm.Run("main");
    }

    [Fact]
    public void Interpreter_NewArray_InvalidAndTooLarge_Throw()
    {
        // Arrange
        var programNeg = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.NEW_ARRAY),
                new Instruction(OpCode.RETURN)
            ],
            [-1]
        );

        var programLarge = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.NEW_ARRAY),
                new Instruction(OpCode.RETURN)
            ],
            [200000]
        );

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => TestsHelpers.Run(programNeg));
        Assert.Throws<OutOfMemoryException>(() => TestsHelpers.Run(programLarge));
    }

    [Fact]
    public void Interpreter_NewArray_AfterGc_Succeeds()
    {
        // Arrange
        const int heapSize = 1024 * 1024;
        var runtime = new RuntimeContext(heapSize);
        _ = runtime.AllocateObject(heapSize - sizeof(long), 0);

        var program = TestsHelpers.CreateProgram(
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.NEW_ARRAY),
                new Instruction(OpCode.RETURN)
            ],
            [1]
        );

        var vm = new VirtualMachine(program, runtime);

        // Act & Assert
        _ = vm.Run("main");
    }

    [Fact]
    public void Interpreter_UnsupportedOpcode_Throws()
    {
        // Arrange
        List<Instruction> code =
        [
            new((OpCode)999),
            new(OpCode.RETURN)
        ];

        var program = TestsHelpers.CreateProgram(code);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => TestsHelpers.Run(program));
    }
}
