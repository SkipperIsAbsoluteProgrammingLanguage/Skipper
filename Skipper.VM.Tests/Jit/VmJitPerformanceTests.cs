using System.Diagnostics;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.VM.Jit;
using Xunit;

namespace Skipper.VM.Tests.Jit;

public class VmJitPerformanceTests
{
    [Fact]
    public void Run_Jit_FasterThanInterpreted_OnHotLoop()
    {
        // Arrange
        var program = BuildLoopProgram(1_000_000);

        // Act
        var interpRuntime = new RuntimeContext();
        var interpVm = new JitVirtualMachine(program, interpRuntime, int.MaxValue, trace: false);
        _ = interpVm.Run("main");
        var interpTicks = MeasureBestTicks(() => interpVm.Run("main"));

        var jitRuntime = new RuntimeContext();
        var jitVm = new JitVirtualMachine(program, jitRuntime, hotThreshold: 1, trace: false);
        _ = jitVm.Run("main");
        var jitTicks = MeasureBestTicks(() => jitVm.Run("main"));

        // Assert
        Assert.True(jitTicks < interpTicks, $"Expected JIT to be faster. interp={interpTicks}, jit={jitTicks}");
    }

    private static long MeasureBestTicks(Action action, int iterations = 3)
    {
        var best = long.MaxValue;
        for (var i = 0; i < iterations; i++)
        {
            var start = Stopwatch.GetTimestamp();
            action();
            var end = Stopwatch.GetTimestamp();
            var elapsed = end - start;
            if (elapsed < best)
            {
                best = elapsed;
            }
        }

        return best;
    }

    private static BytecodeProgram BuildLoopProgram(int n)
    {
        BytecodeProgram program = new();
        program.ConstantPool.Add(0);
        program.ConstantPool.Add(1);
        program.ConstantPool.Add(n);

        BytecodeFunction loop = new(0, "loop", null!, [new BytecodeFunctionParameter("n", null!)])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.STORE_LOCAL, 0, 1),
                new Instruction(OpCode.PUSH, 0),
                new Instruction(OpCode.STORE_LOCAL, 0, 2),

                new Instruction(OpCode.LOAD_LOCAL, 0, 2),
                new Instruction(OpCode.LOAD_LOCAL, 0, 0),
                new Instruction(OpCode.CMP_LT),
                new Instruction(OpCode.JUMP_IF_FALSE, 17),

                new Instruction(OpCode.LOAD_LOCAL, 0, 1),
                new Instruction(OpCode.LOAD_LOCAL, 0, 2),
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.STORE_LOCAL, 0, 1),

                new Instruction(OpCode.LOAD_LOCAL, 0, 2),
                new Instruction(OpCode.PUSH, 1),
                new Instruction(OpCode.ADD),
                new Instruction(OpCode.STORE_LOCAL, 0, 2),

                new Instruction(OpCode.JUMP, 4),

                new Instruction(OpCode.LOAD_LOCAL, 0, 1),
                new Instruction(OpCode.RETURN)
            ]
        };

        BytecodeFunction main = new(1, "main", null!, [])
        {
            Code =
            [
                new Instruction(OpCode.PUSH, 2),
                new Instruction(OpCode.CALL, 0),
                new Instruction(OpCode.RETURN)
            ]
        };

        program.Functions.Add(loop);
        program.Functions.Add(main);
        return program;
    }
}
