using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Interpreter;
using Skipper.VM.Execution;

namespace Skipper.VM.Jit;

public sealed class JitExecutionContext : ExecutionContextBase
{
    private const int DefaultStackCapacity = 256;

    private readonly BytecodeJitCompiler _compiler;
    private readonly int _hotThreshold;
    private readonly Dictionary<int, int> _callCounts = new();
    private readonly HashSet<int> _jittedFunctions = [];


    private Value[] _evalStack;


    public int StackCount { get; private set; }


    public int JittedFunctionCount => _jittedFunctions.Count;
    public IReadOnlyCollection<int> JittedFunctionIds => _jittedFunctions;

    public JitExecutionContext(
        BytecodeProgram program,
        RuntimeContext runtime,
        BytecodeJitCompiler compiler,
        int hotThreshold,
        bool trace)
        : base(program, runtime, trace)
    {
        _compiler = compiler;
        _hotThreshold = Math.Max(hotThreshold, 1);

        _evalStack = new Value[DefaultStackCapacity];
        StackCount = 0;
    }

    protected override int StackSize => StackCount;

    protected override IEnumerable<Value> EnumerateStackValues()
    {
        for (var i = 0; i < StackCount; i++)
        {
            yield return _evalStack[i];
        }
    }

    protected override Value LoadConstCore(int index)
    {
        var c = Program.ConstantPool[index];
        return JitOps.FromConst(this, c);
    }

    public override Value PopStack()
    {
        if (StackCount == 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }

        StackCount--;
        return _evalStack[StackCount];
    }

    public override void PushStack(Value v)
    {
        EnsureStackCapacity(StackCount + 1);
        _evalStack[StackCount] = v;
        StackCount++;
    }

    public override Value PeekStack()
    {
        if (StackCount == 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }

        return _evalStack[StackCount - 1];
    }

    protected override void ExecuteFunction(BytecodeFunction func, bool hasReceiver)
    {
        var locals = LocalsAllocator.Create(func);
        var argCount = func.ParameterTypes.Count;
        for (var i = argCount - 1; i >= 0; i--)
        {
            var value = PopStack();
            locals[i] = CoerceToType(func.ParameterTypes[i].Type, value);
        }

        if (hasReceiver)
        {
            var receiver = PopStack();
            VmChecks.CheckNull(receiver);
        }

        EnterFunctionFrame(func, locals);

        try
        {
            if (ShouldJit(func.FunctionId))
            {
                var isNewJit = _jittedFunctions.Add(func.FunctionId);
                if (isNewJit && Trace)
                {
                    Console.WriteLine($"[JIT] Compiling: {func.Name} ({func.FunctionId})");
                }

                if (Trace)
                {
                    Console.WriteLine($"[JIT] Execute: {func.Name} ({func.FunctionId})");
                }

                var method = _compiler.GetOrCompile(func, Program);
                method(this);
            }
            else
            {
                ExecuteInterpreted(func);
            }
        }
        finally
        {
            ExitFunctionFrame();
        }
    }

    private bool ShouldJit(int functionId)
    {
        if (_jittedFunctions.Contains(functionId))
        {
            return true;
        }

        var calls = IncrementCallCount(functionId);
        if (calls >= _hotThreshold)
        {
            return true;
        }

        return false;
    }

    private int IncrementCallCount(int functionId)
    {
        _callCounts.TryGetValue(functionId, out var count);
        count++;
        _callCounts[functionId] = count;
        return count;
    }

    private void ExecuteInterpreted(BytecodeFunction func)
    {
        BytecodeInterpreter.Execute(this, func);
    }

    private void EnsureStackCapacity(int needed)
    {
        if (needed <= _evalStack.Length)
        {
            return;
        }

        var newSize = _evalStack.Length * 2;
        if (newSize < needed)
        {
            newSize = needed;
        }

        Array.Resize(ref _evalStack, newSize);
    }
}