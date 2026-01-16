using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Xunit;

namespace Skipper.VM.Tests.Jit;

public class VmJitThresholdTests
{
    [Fact]
    public void Run_DoesNotJitColdFunction()
    {
        // Arrange
        BytecodeProgram program = new();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(2);

        var hot = new BytecodeFunction(0, "hot", null!, [new BytecodeFunctionParameter("x", null!)])
        {
            Code =
            [
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };

        var cold = new BytecodeFunction(1, "cold", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.RETURN)
            ]
        };

        var main = new BytecodeFunction(2, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.CALL, 1),
                new Instruction(OpCode.POP),

                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.POP),

                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.POP),

                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(hot);
        program.Functions.Add(cold);
        program.Functions.Add(main);

        // Act
        var vm = new JitVirtualMachine(program, new RuntimeContext(), hotThreshold: 3);
        var result = vm.Run("main");

        // Assert
        Assert.Equal(2, result.AsInt());
        Assert.Contains(0, vm.JittedFunctionIds);
        Assert.DoesNotContain(1, vm.JittedFunctionIds);
        Assert.DoesNotContain(2, vm.JittedFunctionIds);
    }

    [Fact]
    public void Run_ThresholdPreventsJit()
    {
        // Arrange
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);

        var hot = new BytecodeFunction(0, "hot", null!, [new BytecodeFunctionParameter("x", null!)])
        {
            Code =
            [
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };

        var main = new BytecodeFunction(1, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.POP),

                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.POP),

                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(hot);
        program.Functions.Add(main);

        // Act
        var vm = new JitVirtualMachine(program, new RuntimeContext(), hotThreshold: 100);
        var result = vm.Run("main");

        // Assert
        Assert.Equal(2, result.AsInt());
        Assert.Empty(vm.JittedFunctionIds);
    }

    [Fact]
    public void Run_ThresholdOne_JitsMain()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [5]);
        var (result, vm) = TestsHelpers.RunJit(program, hotThreshold: 1);

        // Assert
        Assert.Equal(5, result.AsInt());
        Assert.Contains(0, vm.JittedFunctionIds);
    }

    [Fact]
    public void Run_CompilesHotFunction()
    {
        // Arrange
        BytecodeProgram program = new();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(10);

        var addParams = new List<BytecodeFunctionParameter> { new("x", null!) };
        BytecodeFunction addFunc = new(0, "add1", null!, addParams)
        {
            Code =
            [
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };

        BytecodeFunction mainFunc = new(1, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.POP),

                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.POP),

                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(addFunc);
        program.Functions.Add(mainFunc);

        // Act
        var vm = new JitVirtualMachine(program, new RuntimeContext(), hotThreshold: 2);
        var result = vm.Run("main");

        // Assert
        Assert.Equal(11, result.AsInt());
        Assert.True(vm.JittedFunctionCount >= 1);
    }
}