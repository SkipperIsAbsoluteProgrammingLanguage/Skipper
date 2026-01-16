using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class VmMemoryTests
{
    [Fact]
    public void Run_NewObject_AllocatesInHeap()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.NEW_OBJECT, 0), // Создание объекта класса 0
            new(OpCode.RETURN)
        ];

        var program = TestsHelpers.CreateProgram(code, [0]);

        var cls = new BytecodeClass(0, "User");
        program.Classes.Add(cls);

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(ValueKind.ObjectRef, result.Kind);
        Assert.NotEqual(0, result.AsObject());
    }

    [Fact]
    public void Run_CallFunction_PassesArguments()
    {
        // Arrange
        List<Instruction> mainCode =
        [
            new(OpCode.PUSH, 0), // 10
            new(OpCode.PUSH, 1), // 5
            new(OpCode.CALL, 1), // Вызов add
            new(OpCode.RETURN)
        ];

        var program = TestsHelpers.CreateProgram(mainCode, [10, 5]);

        // Функция add(a, b) { return a + b }
        var paramsAdd = new List<BytecodeFunctionParameter>
        {
            new("a", null!),
            new("b", null!)
        };

        var addFunc = new BytecodeFunction(1, "add", null!, paramsAdd)
        {
            Code =
            [
                new Instruction(OpCode.LOAD_LOCAL, 1, 0), // a (funcId=1, slot=0)
                new Instruction(OpCode.LOAD_LOCAL, 1, 1), // b (funcId=1, slot=1)
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(addFunc);

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(15, result.AsInt());
    }

    [Fact]
    public void Vm_Integration_GcCollectsUnusedObjects()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.NEW_OBJECT, 0),
            new(OpCode.POP),
            new(OpCode.RETURN)
        ];

        var program = TestsHelpers.CreateProgram(code);

        var cls = new BytecodeClass(0, "Temp");
        program.Classes.Add(cls);

        var runtime = new RuntimeContext();
        var vm = new VirtualMachine(program, runtime);

        // Act
        _ = vm.Run("main");
        runtime.Collect(vm);

        // Assert
        Assert.Equal(0, runtime.GetAliveObjectCount());
    }
}