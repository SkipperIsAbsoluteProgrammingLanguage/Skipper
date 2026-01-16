using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Values;

namespace Skipper.VM.Jit;

public sealed class JitVirtualMachine
{
    private readonly BytecodeProgram _program;
    private readonly RuntimeContext _runtime;
    private readonly BytecodeJitCompiler _compiler = new();
    private readonly int _hotThreshold;
    private readonly bool _trace;

    public int JittedFunctionCount { get; private set; }
    public IReadOnlyCollection<int> JittedFunctionIds { get; private set; } = [];

    public JitVirtualMachine(BytecodeProgram program, RuntimeContext runtime, int hotThreshold = 50, bool trace = true)
    {
        _program = program;
        _runtime = runtime;
        _hotThreshold = Math.Max(hotThreshold, 1);
        _trace = trace;
    }

    public Value Run(string entryPointName)
    {
        var mainFunc = _program.Functions.FirstOrDefault(f => f.Name == entryPointName);
        if (mainFunc == null)
        {
            throw new InvalidOperationException($"Function '{entryPointName}' not found");
        }

        var ctx = new JitExecutionContext(_program, _runtime, _compiler, forceJit: false, hotThreshold: _hotThreshold, trace: _trace);
        ctx.CallFunction(mainFunc.FunctionId);
        JittedFunctionCount = ctx.JittedFunctionCount;
        JittedFunctionIds = new HashSet<int>(ctx.JittedFunctionIds);

        return ctx.StackCount > 0 ? ctx.PopStack() : Value.Null();
    }
}
