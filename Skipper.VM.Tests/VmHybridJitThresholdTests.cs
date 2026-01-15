using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.VM;
using Xunit;

namespace Skipper.VM.Tests;

public class VmHybridJitThresholdTests
{
    [Fact]
    public void Run_Hybrid_DoesNotJitColdFunction()
    {
        BytecodeProgram program = new();
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(2);

        BytecodeFunction hot = new(0, "hot", null!, [new BytecodeFunctionParameter("x", null!)])
        {
            Code =
            [
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };

        BytecodeFunction cold = new(1, "cold", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.RETURN)
            ]
        };

        BytecodeFunction main = new(2, "main", null!, [])
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

        var vm = new HybridVirtualMachine(program, new RuntimeContext(), hotThreshold: 3);
        var result = vm.Run("main");

        Assert.Equal(2, result.AsInt());
        Assert.Contains(0, vm.JittedFunctionIds);
        Assert.DoesNotContain(1, vm.JittedFunctionIds);
        Assert.DoesNotContain(2, vm.JittedFunctionIds);
    }

    [Fact]
    public void Run_Hybrid_ThresholdPreventsJit()
    {
        BytecodeProgram program = new();
        program.ConstantPool.Add(1);

        BytecodeFunction hot = new(0, "hot", null!, [new BytecodeFunctionParameter("x", null!)])
        {
            Code =
            [
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.RETURN)
            ]
        };

        BytecodeFunction main = new(1, "main", null!, [])
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

        var vm = new HybridVirtualMachine(program, new RuntimeContext(), hotThreshold: 100);
        var result = vm.Run("main");

        Assert.Equal(2, result.AsInt());
        Assert.Empty(vm.JittedFunctionIds);
    }

    [Fact]
    public void Run_Hybrid_ThresholdOne_JitsMain()
    {
        BytecodeProgram program = new();
        program.ConstantPool.Add(5);

        BytecodeFunction main = new(0, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(main);

        var vm = new HybridVirtualMachine(program, new RuntimeContext(), hotThreshold: 1);
        var result = vm.Run("main");

        Assert.Equal(5, result.AsInt());
        Assert.Contains(0, vm.JittedFunctionIds);
    }
}
