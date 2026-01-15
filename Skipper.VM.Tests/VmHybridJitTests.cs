using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Xunit;

namespace Skipper.VM.Tests;

public class VmHybridJitTests
{
    [Fact]
    public void Run_Hybrid_CompilesHotFunction()
    {
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

        var vm = new HybridVirtualMachine(program, new RuntimeContext(), hotThreshold: 2);
        var result = vm.Run("main");

        Assert.Equal(11, result.AsInt());
        Assert.True(vm.JittedFunctionCount >= 1);
    }
}
