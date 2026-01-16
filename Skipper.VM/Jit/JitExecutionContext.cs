using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Abstractions;
using Skipper.Runtime.Values;

namespace Skipper.VM.Jit;

internal sealed class JitExecutionContext : IVirtualMachine, IRootProvider
{
    private const int DefaultStackCapacity = 256;
    private const int MinLocalSlots = 64;

    internal readonly RuntimeContext Runtime;
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
    private int _sp;

    private readonly Stack<JitFrame> _callStack = new();
    private BytecodeFunction? _currentFunc;
    private Value[]? _currentLocals;

    private readonly Value[] _globals;

    internal int StackCount => _sp;
    internal int JittedFunctionCount => _jittedFunctions.Count;
    internal IReadOnlyCollection<int> JittedFunctionIds => _jittedFunctions;

    internal bool HasStack()
    {
        return _sp > 0;
    }

    internal JitExecutionContext(
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
        _sp = 0;

        _globals = new Value[program.Globals.Count];
    }

    public Value PopStack()
    {
        if (_sp == 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }

        _sp--;
        return _evalStack[_sp];
    }

    public void PushStack(Value v)
    {
        EnsureStackCapacity(_sp + 1);
        _evalStack[_sp] = v;
        _sp++;
    }

    public Value PeekStack()
    {
        if (_sp == 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }

        return _evalStack[_sp - 1];
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
        var locals = CreateLocals(func);
        var argCount = func.ParameterTypes.Count;
        for (var i = argCount - 1; i >= 0; i--)
        {
            locals[i] = PopStack();
        }

        if (hasReceiver)
        {
            var receiver = PopStack();
            JitOps.CheckNull(receiver);
        }

        if (_currentFunc != null && _currentLocals != null)
        {
            _callStack.Push(new JitFrame(_currentFunc, _currentLocals));
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
        var code = func.Code;
        var ip = 0;

        while (ip < code.Count)
        {
            var instr = code[ip];
            if (_trace)
            {
                Console.WriteLine($"[STEP] Func: {func.Name}, IP: {ip} (Total: {code.Count}), Op: {instr.OpCode}");
            }
            switch (instr.OpCode)
            {
                case BaitCode.Objects.Instructions.OpCode.PUSH:
                {
                    var constId = Convert.ToInt32(instr.Operands[0]);
                    PushStack(LoadConst(constId));
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.POP:
                {
                    if (HasStack())
                    {
                        _ = PopStack();
                    }

                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.DUP:
                    PushStack(PeekStack());
                    ip++;
                    break;

                case BaitCode.Objects.Instructions.OpCode.SWAP:
                {
                    var top = PopStack();
                    var below = PopStack();
                    PushStack(top);
                    PushStack(below);
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.LOAD_LOCAL:
                {
                    var slot = Convert.ToInt32(instr.Operands[1]);
                    PushStack(LoadLocal(slot));
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.STORE_LOCAL:
                {
                    var slot = Convert.ToInt32(instr.Operands[1]);
                    StoreLocal(slot, PopStack());
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.LOAD_GLOBAL:
                {
                    var slot = Convert.ToInt32(instr.Operands[0]);
                    PushStack(LoadGlobal(slot));
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.STORE_GLOBAL:
                {
                    var slot = Convert.ToInt32(instr.Operands[0]);
                    StoreGlobal(slot, PopStack());
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.ADD:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.Add(this, a, b));
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.SUB:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.Sub(a, b));
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.MUL:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.Mul(a, b));
                    ip++;
                }
                break;

                case BaitCode.Objects.Instructions.OpCode.DIV:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.Div(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.MOD:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.Mod(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.NEG:
                {
                    var v = PopStack();
                    PushStack(JitOps.Neg(v));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CMP_EQ:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.CmpEq(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CMP_NE:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.CmpNe(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CMP_LT:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.CmpLt(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CMP_GT:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.CmpGt(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CMP_LE:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.CmpLe(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CMP_GE:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.CmpGe(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.AND:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.And(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.OR:
                {
                    var b = PopStack();
                    var a = PopStack();
                    PushStack(JitOps.Or(a, b));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.NOT:
                {
                    var v = PopStack();
                    PushStack(JitOps.Not(v));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.JUMP:
                    ip = Convert.ToInt32(instr.Operands[0]);
                    break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.JUMP_IF_TRUE:
                {
                    var cond = PopStack();
                    if (JitOps.IsTrue(cond))
                    {
                        ip = Convert.ToInt32(instr.Operands[0]);
                    }
                    else
                    {
                        ip++;
                    }
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.JUMP_IF_FALSE:
                {
                    var cond = PopStack();
                    if (!JitOps.IsTrue(cond))
                    {
                        ip = Convert.ToInt32(instr.Operands[0]);
                    }
                    else
                    {
                        ip++;
                    }
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CALL:
                {
                    var funcId = Convert.ToInt32(instr.Operands[0]);
                    CallFunction(funcId);
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CALL_METHOD:
                {
                    var classId = Convert.ToInt32(instr.Operands[0]);
                    var methodId = Convert.ToInt32(instr.Operands[1]);
                    CallMethod(classId, methodId);
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.RETURN:
                    return;

                case Skipper.BaitCode.Objects.Instructions.OpCode.NEW_OBJECT:
                {
                    var classId = Convert.ToInt32(instr.Operands[0]);
                    PushStack(JitOps.NewObject(this, classId));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.NEW_ARRAY:
                {
                    var lengthValue = PopStack();
                    PushStack(JitOps.NewArray(this, lengthValue));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.GET_FIELD:
                {
                    var fieldId = Convert.ToInt32(instr.Operands[1]);
                    var objRef = PopStack();
                    PushStack(JitOps.GetField(this, objRef, fieldId));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.SET_FIELD:
                {
                    var fieldId = Convert.ToInt32(instr.Operands[1]);
                    var value = PopStack();
                    var objRef = PopStack();
                    JitOps.SetField(this, objRef, fieldId, value);
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.GET_ELEMENT:
                {
                    var index = PopStack();
                    var arrRef = PopStack();
                    PushStack(JitOps.GetElement(this, arrRef, index));
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.SET_ELEMENT:
                {
                    var value = PopStack();
                    var index = PopStack();
                    var arrRef = PopStack();
                    JitOps.SetElement(this, arrRef, index, value);
                    ip++;
                }
                break;

                case Skipper.BaitCode.Objects.Instructions.OpCode.CALL_NATIVE:
                {
                    var nativeId = Convert.ToInt32(instr.Operands[0]);
                    CallNative(nativeId);
                    ip++;
                }
                break;

                default:
                    throw new NotSupportedException($"Unsupported opcode {instr.OpCode}");
            }
        }
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
        for (var i = 0; i < _sp; i++)
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

    private static Value[] CreateLocals(BytecodeFunction func)
    {
        var totalCount = func.ParameterTypes.Count + func.Locals.Count;
        var safeSize = Math.Max(totalCount, MinLocalSlots);
        return new Value[safeSize];
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
}
