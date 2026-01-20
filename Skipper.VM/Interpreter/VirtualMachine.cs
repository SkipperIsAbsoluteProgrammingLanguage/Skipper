using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Types;
using Skipper.Runtime;
using Skipper.Runtime.Abstractions;
using Skipper.Runtime.Values;
using Skipper.VM.Execution;

namespace Skipper.VM.Interpreter;

// Интерпретируемая VM: исполняет байткод напрямую.
public sealed class VirtualMachine : IInterpreterContext, IRootProvider
{
    // Стек вычислений для интерпретатора.
    private readonly Stack<Value> _evalStack = new();

    public VirtualMachine(BytecodeProgram program, RuntimeContext runtime, bool trace = false)
        : base(program, runtime, trace)
    { }

    public Value Run(string entryPointName)
    {
        // Находим точку входа и запускаем выполнение.
        var mainFunc = Program.Functions.FirstOrDefault(f => f.Name == entryPointName);
        if (mainFunc == null)
        {
            throw new InvalidOperationException($"Function '{entryPointName}' not found");
        }

        ExecuteFunction(mainFunc, hasReceiver: false);

        // Возвращаем верх стека как результат выполнения.
        return _evalStack.Count > 0 ? _evalStack.Pop() : Value.Null();
    }

    // Примитивные операции со стеком вычислений.
    public override Value PopStack() => _evalStack.Pop();
    public override void PushStack(Value v) => _evalStack.Push(v);
    public override Value PeekStack() => _evalStack.Peek();

    protected override int StackSize => _evalStack.Count;

    protected override IEnumerable<Value> EnumerateStackValues() => _evalStack;

    protected override Value LoadConstCore(int index)
    {
        // Константы в интерпретаторе материализуются в Value сразу.
        var c = Program.ConstantPool[index];
        return ValueFromConst(c);
    }

    protected override void ExecuteFunction(BytecodeFunction func, bool hasReceiver)
    {
        // Создаём фрейм локалов и раскладываем аргументы со стека.
        var locals = LocalsAllocator.Create(func);
        var argCount = func.ParameterTypes.Count;

        var argOffset = hasReceiver ? 1 : 0;

        for (var i = argCount - 1; i >= 0; i--)
        {
            if (_evalStack.Count == 0)
            {
                throw new InvalidOperationException("Stack underflow on args");
            }

            var value = _evalStack.Pop();
            locals[i + argOffset] = CoerceToType(func.ParameterTypes[i].Type, value);
        }

        if (hasReceiver)
        {
            var receiver = _evalStack.Pop();
            CheckNull(receiver);
            locals[0] = receiver;
        }

        EnterFunctionFrame(func, locals);

        try
        {
            // Основной цикл исполнения байткода.
            BytecodeInterpreter.Execute(this, func);
        } finally
        {
            ExitFunctionFrame();
        }
    }

    private Value ValueFromConst(object c)
    {
        // Преобразуем объект из пула констант в Value VM.
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

    internal Value AllocateString(string s)
    {
        // Строка в VM — это массив char на куче.
        var ptr = Runtime.AllocateArray(s.Length);

        if (!_runtime.CanAllocate(payloadSize))
        {
            _runtime.Collect(this);
            if (!_runtime.CanAllocate(payloadSize))
            {
                throw new OutOfMemoryException("Heap full (String allocation)");
            }
        }

        var ptr = _runtime.AllocateArray(length);
        for (var i = 0; i < length; i++)
        {
            Runtime.WriteArrayElement(ptr, i, Value.FromChar(s[i]));
        }

        return Value.FromObject(ptr);
    }

    internal Value GetDefaultValueForType(BytecodeType type)
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

    private static void CheckNull(Value refVal)
    {
        if (refVal.Kind == ValueKind.Null || (refVal.Kind == ValueKind.ObjectRef && refVal.Raw == 0))
        {
            throw new NullReferenceException("Object reference not set to an instance of an object.");
        }
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
    Value IInterpreterContext.AllocateString(string s) => AllocateString(s);
    Value IInterpreterContext.GetDefaultValueForType(BytecodeType type) => GetDefaultValueForType(type);
}
