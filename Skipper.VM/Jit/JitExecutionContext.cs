using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Interpreter;

namespace Skipper.VM.Jit;

public sealed class JitExecutionContext : IInterpreterContext
{
    private const int DefaultStackCapacity = 256;
    public readonly RuntimeContext Runtime;
    private readonly BytecodeProgram _program;

    private readonly BytecodeJitCompiler _compiler;
    private readonly Dictionary<int, BytecodeFunction> _functions;
    private readonly Dictionary<int, BytecodeClass> _classes;
    private readonly bool _forceJit;
    private readonly int _hotThreshold;
    private readonly bool _trace;
    private readonly Dictionary<int, int> _callCounts = new();
    private readonly HashSet<int> _jittedFunctions = [];

    private Value[] _evalStack;

    private readonly Stack<CallFrame> _callStack = new();
    private BytecodeFunction? _currentFunc;
    private Value[]? _currentLocals;

    private readonly Value[] _globals;

    public int StackCount { get; private set; }

    public int JittedFunctionCount => _jittedFunctions.Count;
    public IReadOnlyCollection<int> JittedFunctionIds => _jittedFunctions;

    internal bool HasStack()
    {
        return StackCount > 0;
    }

    public JitExecutionContext(
        BytecodeProgram program,
        RuntimeContext runtime,
        BytecodeJitCompiler compiler,
        bool forceJit,
        int hotThreshold,
        bool trace)
    {
        _program = program;
        Runtime = runtime;
        _compiler = compiler;
        _forceJit = forceJit;
        _hotThreshold = Math.Max(hotThreshold, 1);
        _trace = trace;

        _functions = program.Functions.ToDictionary(f => f.FunctionId, f => f);
        _classes = program.Classes.ToDictionary(c => c.ClassId, c => c);

        _evalStack = new Value[DefaultStackCapacity];
        StackCount = 0;

        _globals = new Value[program.Globals.Count];
    }

    public Value PopStack()
    {
        if (StackCount == 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }

        StackCount--;
        return _evalStack[StackCount];
    }

    public void PushStack(Value v)
    {
        EnsureStackCapacity(StackCount + 1);
        _evalStack[StackCount] = v;
        StackCount++;
    }

    public Value PeekStack()
    {
        if (StackCount == 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }

        return _evalStack[StackCount - 1];
    }

    internal Value LoadConst(int index)
    {
        var c = _program.ConstantPool[index];
        return JitOps.FromConst(this, c);
    }

    internal Value LoadLocal(int slot)
    {
        if (_currentLocals == null)
        {
            throw new InvalidOperationException("No current locals in scope");
        }

        return _currentLocals[slot];
    }

    internal void StoreLocal(int slot, Value value)
    {
        if (_currentLocals == null)
        {
            throw new InvalidOperationException("No current locals in scope");
        }

        _currentLocals[slot] = value;
    }

    internal Value LoadGlobal(int slot)
    {
        return _globals[slot];
    }

    internal void StoreGlobal(int slot, Value value)
    {
        _globals[slot] = value;
    }

    internal void CallFunction(int functionId)
    {
        if (!_functions.TryGetValue(functionId, out var func))
        {
            throw new InvalidOperationException($"Func ID {functionId} not found");
        }

        ExecuteFunction(func, hasReceiver: false);
    }

    internal void CallMethod(int classId, int methodId)
    {
        _ = classId;
        if (!_functions.TryGetValue(methodId, out var func))
        {
            throw new InvalidOperationException($"Method ID {methodId} not found");
        }

        ExecuteFunction(func, hasReceiver: true);
    }

    internal void CallNative(int nativeId)
    {
        Runtime.InvokeNative(nativeId, this);
    }

    private void ExecuteFunction(BytecodeFunction func, bool hasReceiver)
    {
        var locals = LocalsAllocator.Create(func);
        var argCount = func.ParameterTypes.Count;
        for (var i = argCount - 1; i >= 0; i--)
        {
            locals[i] = PopStack();
        }

        if (hasReceiver)
        {
            var receiver = PopStack();
            VmChecks.CheckNull(receiver);
        }

        if (_currentFunc != null && _currentLocals != null)
        {
            _callStack.Push(new CallFrame(_currentFunc, _currentLocals));
        }

        _currentFunc = func;
        _currentLocals = locals;

        try
        {
            if (ShouldJit(func.FunctionId))
            {
                var isNewJit = _jittedFunctions.Add(func.FunctionId);
                if (isNewJit && _trace)
                {
                    Console.WriteLine($"[JIT] Compiling: {func.Name} ({func.FunctionId})");
                }

                if (_trace)
                {
                    Console.WriteLine($"[JIT] Execute: {func.Name} ({func.FunctionId})");
                }

                var method = _compiler.GetOrCompile(func);
                method(this);
            }
            else
            {
                ExecuteInterpreted(func);
            }
        }
        finally
        {
            RestoreCallerFrame();
        }
    }

    private bool ShouldJit(int functionId)
    {
        if (_forceJit || _jittedFunctions.Contains(functionId))
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

    internal BytecodeClass GetClassById(int classId)
    {
        if (_classes.TryGetValue(classId, out var cls))
        {
            return cls;
        }

        throw new InvalidOperationException($"Class ID {classId} not found");
    }

    public IEnumerable<nint> EnumerateRoots()
    {
        for (var i = 0; i < StackCount; i++)
        {
            var val = _evalStack[i];
            if (val.Kind == ValueKind.ObjectRef && val.Raw != 0)
            {
                yield return (nint)val.Raw;
            }
        }

        if (_currentLocals != null)
        {
            foreach (var local in _currentLocals)
            {
                if (local.Kind == ValueKind.ObjectRef && local.Raw != 0)
                {
                    yield return (nint)local.Raw;
                }
            }
        }

        foreach (var frame in _callStack)
        {
            foreach (var local in frame.Locals)
            {
                if (local.Kind == ValueKind.ObjectRef && local.Raw != 0)
                {
                    yield return (nint)local.Raw;
                }
            }
        }

        foreach (var global in _globals)
        {
            if (global.Kind == ValueKind.ObjectRef && global.Raw != 0)
            {
                yield return (nint)global.Raw;
            }
        }
    }

    private void RestoreCallerFrame()
    {
        if (_callStack.Count == 0)
        {
            _currentFunc = null;
            _currentLocals = null;
            return;
        }

        var frame = _callStack.Pop();
        _currentFunc = frame.Function;
        _currentLocals = frame.Locals;
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

    BytecodeProgram IInterpreterContext.Program => _program;
    RuntimeContext IInterpreterContext.Runtime => Runtime;
    bool IInterpreterContext.Trace => _trace;
    bool IInterpreterContext.HasStack() => HasStack();
    Value IInterpreterContext.LoadConst(int index) => LoadConst(index);
    Value IInterpreterContext.LoadLocal(int slot) => LoadLocal(slot);
    void IInterpreterContext.StoreLocal(int slot, Value value) => StoreLocal(slot, value);
    Value IInterpreterContext.LoadGlobal(int slot) => LoadGlobal(slot);
    void IInterpreterContext.StoreGlobal(int slot, Value value) => StoreGlobal(slot, value);
    void IInterpreterContext.CallFunction(int functionId) => CallFunction(functionId);
    void IInterpreterContext.CallMethod(int classId, int methodId) => CallMethod(classId, methodId);
    void IInterpreterContext.CallNative(int nativeId) => CallNative(nativeId);
    BytecodeClass IInterpreterContext.GetClassById(int classId) => GetClassById(classId);
}
