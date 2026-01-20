using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Interpreter;
using Skipper.VM.Execution;

namespace Skipper.VM.Jit;

// Контекст исполнения для JIT: решает когда интерпретировать, когда компилировать.
public sealed class JitExecutionContext : ExecutionContextBase
{
    private const int DefaultStackCapacity = 256;
    // Компилятор байткода в IL и параметры горячих функций.
    private readonly BytecodeJitCompiler _compiler;
    private readonly int _hotThreshold;
    private readonly Dictionary<int, int> _callCounts = new();
    private readonly HashSet<int> _jittedFunctions = [];

    // Стек вычислений для JIT (массив быстрее, чем Stack<T>).
    private Value[] _evalStack;

    // Текущий размер стека.
    public int StackCount { get; private set; }

    // Метрики JIT: какие функции были скомпилированы.
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
        // Перечисление значений стека для GC.
        for (var i = 0; i < StackCount; i++)
        {
            yield return _evalStack[i];
        }
    }

    protected override Value LoadConstCore(int index)
    {
        // Константы для JIT проходят через JitOps.
        var c = Program.ConstantPool[index];
        return JitOps.FromConst(this, c);
    }

    public override Value PopStack()
    {
        // Снятие значения со стека.
        if (StackCount == 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }

        StackCount--;
        return _evalStack[StackCount];
    }

    public override void PushStack(Value v)
    {
        // Добавление значения в стек.
        EnsureStackCapacity(StackCount + 1);
        _evalStack[StackCount] = v;
        StackCount++;
    }

    public override Value PeekStack()
    {
        // Просмотр верхушки стека.
        if (StackCount == 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }

        return _evalStack[StackCount - 1];
    }

    protected override void ExecuteFunction(BytecodeFunction func, bool hasReceiver)
    {
        // Создание локалов и извлечение аргументов со стека.
        var locals = LocalsAllocator.Create(func);
        var argCount = func.ParameterTypes.Count;
        var paramOffset = hasReceiver ? 1 : 0;
        for (var i = argCount - 1; i >= 0; i--)
        {
            var value = PopStack();
            locals[i + paramOffset] = CoerceToType(func.ParameterTypes[i].Type, value);
        }

        if (hasReceiver)
        {
            var receiver = PopStack();
            VmChecks.CheckNull(receiver);
            locals[0] = receiver;
        }

        EnterFunctionFrame(func, locals);

        try
        {
            // Решаем: интерпретировать или вызвать JIT-версию.
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
                // Выполняем интерпретатором.
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
        // Решение о JIT по счётчику вызовов.
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
        // Увеличение счётчика вызовов функции.
        _callCounts.TryGetValue(functionId, out var count);
        count++;
        _callCounts[functionId] = count;
        return count;
    }

    private void ExecuteInterpreted(BytecodeFunction func)
    {
        // Переиспользуем интерпретатор, но с этим контекстом.
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

    public Value AllocateString(string s)
    {
        var ptr = Runtime.AllocateArray(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            Runtime.WriteArrayElement(ptr, i, Value.FromChar(s[i]));
        }
        return Value.FromObject(ptr);
    }

    public Value GetDefaultValueForType(BytecodeType type)
    {
        if (type is PrimitiveType prim)
        {
            return prim.Name switch
            {
                "int" => Value.FromInt(0),
                "long" => Value.FromLong(0),
                "double" => Value.FromDouble(0.0),
                "bool" => Value.FromBool(false),
                "char" => Value.FromChar('\0'),
                _ => Value.Null()
            };
        }
        return Value.Null();
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
        // Растим массив стека по мере необходимости.
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
    Value IInterpreterContext.AllocateString(string s) => AllocateString(s);
    Value IInterpreterContext.GetDefaultValueForType(BytecodeType type) => GetDefaultValueForType(type);
}
