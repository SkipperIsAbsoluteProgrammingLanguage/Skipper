using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.BaitCode.Types;

namespace Skipper.VM.Interpreter;

public sealed class VirtualMachine : IInterpreterContext
{
    private readonly RuntimeContext _runtime;
    private readonly BytecodeProgram _program;
    private readonly Value[] _globals;
    private readonly Stack<CallFrame> _callStack = new();
    private readonly Stack<Value> _evalStack = new();
    private readonly bool _trace;

    private BytecodeFunction? _currentFunc;
    private Value[]? _currentLocals;

    public VirtualMachine(BytecodeProgram program, RuntimeContext runtime, bool trace = false)
    {
        _program = program;
        _runtime = runtime;
        _trace = trace;
        _globals = new Value[program.Globals.Count];
    }

    public Value Run(string entryPointName)
    {
        var mainFunc = _program.Functions.FirstOrDefault(f => f.Name == entryPointName);
        if (mainFunc == null)
        {
            throw new InvalidOperationException($"Function '{entryPointName}' not found");
        }

        ExecuteFunction(mainFunc, hasReceiver: false);

        return _evalStack.Count > 0 ? _evalStack.Pop() : Value.Null();
    }

    public Value PopStack() => _evalStack.Pop();
    public void PushStack(Value v) => _evalStack.Push(v);
    public Value PeekStack() => _evalStack.Peek();

    internal bool HasStack() => _evalStack.Count > 0;

    private void ExecuteFunction(BytecodeFunction func, bool hasReceiver)
    {
        var locals = LocalsAllocator.Create(func);
        var argCount = func.ParameterTypes.Count;
        for (var i = argCount - 1; i >= 0; i--)
        {
            if (_evalStack.Count == 0)
            {
                throw new InvalidOperationException("Stack underflow on args");
            }

            var value = _evalStack.Pop();
            locals[i] = CoerceToType(func.ParameterTypes[i].Type, value);
        }

        if (hasReceiver)
        {
            var receiver = _evalStack.Pop();
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
            BytecodeInterpreter.Execute(this, func);
        }
        finally
        {
            RestoreCallerFrame();
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

    internal Value LoadConst(int index)
    {
        var c = _program.ConstantPool[index];
        return ValueFromConst(c);
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

        _currentLocals[slot] = CoerceToLocalType(slot, value);
    }

    internal Value LoadGlobal(int slot)
    {
        return _globals[slot];
    }

    internal void StoreGlobal(int slot, Value value)
    {
        _globals[slot] = CoerceToType(_program.Globals[slot].Type, value);
    }

    internal void CallFunction(int functionId)
    {
        var target = _program.Functions.FirstOrDefault(f => f.FunctionId == functionId);
        if (target == null)
        {
            throw new InvalidOperationException($"Func ID {functionId} not found");
        }

        ExecuteFunction(target, hasReceiver: false);
    }

    internal void CallMethod(int classId, int methodId)
    {
        _ = classId;
        var target = _program.Functions.FirstOrDefault(f => f.FunctionId == methodId);
        if (target == null)
        {
            throw new InvalidOperationException($"Method ID {methodId} not found");
        }

        ExecuteFunction(target, hasReceiver: true);
    }

    internal void CallNative(int nativeId)
    {
        _runtime.InvokeNative(nativeId, this);
    }

    internal BytecodeClass GetClassById(int classId)
    {
        var cls = _program.Classes.FirstOrDefault(c => c.ClassId == classId);
        if (cls == null)
        {
            throw new InvalidOperationException($"Class ID {classId} not found");
        }

        return cls;
    }

    private Value ValueFromConst(object c)
    {
        return c switch
        {
            null => Value.Null(),
            int i => Value.FromInt(i),
            long l => Value.FromLong(l),
            double d => Value.FromDouble(d),
            bool b => Value.FromBool(b),
            char ch => Value.FromChar(ch),
            string s => AllocateString(s),
            _ => throw new NotImplementedException($"Const type {c.GetType()} not supported")
        };
    }

    private Value AllocateString(string s)
    {
        var ptr = _runtime.AllocateArray(s.Length);

        for (var i = 0; i < s.Length; i++)
        {
            _runtime.WriteArrayElement(ptr, i, Value.FromChar(s[i]));
        }

        return Value.FromObject(ptr);
    }

    private static Value CoerceToType(BytecodeType type, Value value)
    {
        if (type is PrimitiveType primitive && primitive.Name == "long")
        {
            if (value.Kind == ValueKind.Int)
            {
                return Value.FromLong(value.AsInt());
            }

            if (value.Kind == ValueKind.Long)
            {
                return value;
            }
        }

        return value;
    }

    private Value CoerceToLocalType(int slot, Value value)
    {
        if (_currentFunc != null && slot >= 0 && slot < _currentFunc.Locals.Count)
        {
            return CoerceToType(_currentFunc.Locals[slot].Type, value);
        }

        return value;
    }

    public IEnumerable<nint> EnumerateRoots()
    {
        foreach (var val in _evalStack)
        {
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

    BytecodeProgram IInterpreterContext.Program => _program;
    RuntimeContext IInterpreterContext.Runtime => _runtime;
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
