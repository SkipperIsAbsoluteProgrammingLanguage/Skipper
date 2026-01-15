using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Jit;

namespace Skipper.VM;

public sealed class JitVirtualMachine
{
    private readonly BytecodeProgram _program;
    private readonly RuntimeContext _runtime;
    private readonly BytecodeJitCompiler _compiler = new();

    public JitVirtualMachine(BytecodeProgram program, RuntimeContext runtime)
    {
        _program = program;
        _runtime = runtime;
    }

    public Value Run(string entryPointName)
    {
        var mainFunc = _program.Functions.FirstOrDefault(f => f.Name == entryPointName);
        if (mainFunc == null)
        {
            throw new InvalidOperationException($"Function '{entryPointName}' not found");
        }

        var ctx = new JitExecutionContext(_program, _runtime, _compiler, forceJit: true, hotThreshold: 1);
        ctx.CallFunction(mainFunc.FunctionId);

        return ctx.StackCount > 0 ? ctx.PopStack() : Value.Null();
    }
}
