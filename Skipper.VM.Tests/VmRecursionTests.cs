using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Xunit;

namespace Skipper.VM.Tests;

public class VmRecursionTests
{
    [Fact]
    public void Run_Factorial_RecursionWorks()
    {
        // fn fact(n) {
        //    if (n <= 1) return 1;
        //    return n * fact(n - 1);
        // }

        // Arrange
        BytecodeProgram program = new();
        program.ConstantPool.Add(1); // Index 0
        program.ConstantPool.Add(5); // Index 1

        var paramsFact = new List<BytecodeFunctionParameter> { new("n", null!) };

        var factFunc = new BytecodeFunction(0, "fact", null!, paramsFact)
        {
            Code =
            [
                new Instruction(OpCode.LOAD_LOCAL, 0, 0), // 0: n
                new Instruction(OpCode.PUSH, 0), // 1: 1
                new Instruction(OpCode.CMP_LE), // 2: n <= 1
                new Instruction(OpCode.JUMP_IF_FALSE, 6), // 3: else -> 6

                // if
                new Instruction(OpCode.PUSH, 0), // 4: 1
                new Instruction(OpCode.RETURN), // 5

                // else
                new Instruction(OpCode.LOAD_LOCAL, 0, 0), // 6: n
                new Instruction(OpCode.LOAD_LOCAL, 0, 0), // 7: n
                new Instruction(OpCode.PUSH, 0), // 8: 1
                new Instruction(OpCode.SUB), // 9: n-1
                new Instruction(OpCode.CALL, 0), // 10: fact(n-1)
                new Instruction(OpCode.MUL), // 11
                new Instruction(OpCode.RETURN) // 12
            ]
        };

        var mainFunc = new BytecodeFunction(1, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 1), // 5
                new Instruction(OpCode.CALL, 0), // fact(5)
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(factFunc);
        program.Functions.Add(mainFunc);

        // Act
        var vm = new VirtualMachine(program, new RuntimeContext());
        var result = vm.Run("main");

        // Assert: 5! = 120
        Assert.Equal(120, result.AsInt());
    }
}