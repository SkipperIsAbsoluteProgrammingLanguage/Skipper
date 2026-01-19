using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Types;
using Skipper.Runtime;
using Skipper.Runtime.Values;

namespace Skipper.VM.Execution;

// Общая база для контекстов исполнения (интерпретатор и JIT).
// Содержит общую логику: фреймы вызовов, локалы, глобалы, резолв функций/классов.
public abstract class ExecutionContextBase : IInterpreterContext
{
    // Программа и рантайм, с которыми работает VM.
    public BytecodeProgram Program { get; }
    public RuntimeContext Runtime { get; }
    public bool Trace { get; }

    // Быстрый доступ к функциям и классам по ID.
    private readonly Dictionary<int, BytecodeFunction> _functions;
    private readonly Dictionary<int, BytecodeClass> _classes;

    // Стек вызовов и текущий фрейм.
    protected readonly Stack<CallFrame> CallStack = new();
    protected BytecodeFunction? CurrentFunc;
    protected Value[]? CurrentLocals;

    // Глобальные переменные программы.
    protected readonly Value[] Globals;

    protected ExecutionContextBase(BytecodeProgram program, RuntimeContext runtime, bool trace)
    {
        Program = program;
        Runtime = runtime;
        Trace = trace;

        _functions = program.Functions.ToDictionary(f => f.FunctionId, f => f);
        _classes = program.Classes.ToDictionary(c => c.ClassId, c => c);
        Globals = new Value[program.Globals.Count];
    }

    public Value LoadLocal(int slot)
    {
        // Чтение локальной переменной из текущего фрейма.
        if (CurrentLocals == null)
        {
            throw new InvalidOperationException("No current locals in scope");
        }

        return CurrentLocals[slot];
    }

    public void StoreLocal(int slot, Value value)
    {
        // Запись локальной переменной с приведением к типу слота.
        if (CurrentLocals == null)
        {
            throw new InvalidOperationException("No current locals in scope");
        }

        CurrentLocals[slot] = CoerceToLocalType(slot, value);
    }

    public Value LoadGlobal(int slot)
    {
        // Чтение глобальной переменной.
        return Globals[slot];
    }

    public void StoreGlobal(int slot, Value value)
    {
        // Запись глобальной переменной с приведением к объявленному типу.
        Globals[slot] = CoerceToType(Program.Globals[slot].Type, value);
    }

    public void CallFunction(int functionId)
    {
        // Вызов обычной функции по ID.
        var func = GetFunctionById(functionId);
        ExecuteFunction(func, hasReceiver: false);
    }

    public void CallMethod(int classId, int methodId)
    {
        // Вызов метода: receiver уже лежит на стеке.
        _ = classId;
        var func = GetFunctionById(methodId);
        ExecuteFunction(func, hasReceiver: true);
    }

    public void CallNative(int nativeId)
    {
        // Нативные функции вызываются через Runtime.
        Runtime.InvokeNative(nativeId, this);
    }

    public BytecodeClass GetClassById(int classId)
    {
        // Поиск класса по ID из словаря метаданных.
        if (_classes.TryGetValue(classId, out var cls))
        {
            return cls;
        }

        throw new InvalidOperationException($"Class ID {classId} not found");
    }

    protected BytecodeFunction GetFunctionById(int functionId)
    {
        // Поиск функции по ID из словаря метаданных.
        if (_functions.TryGetValue(functionId, out var func))
        {
            return func;
        }

        throw new InvalidOperationException($"Func ID {functionId} not found");
    }

    protected void EnterFunctionFrame(BytecodeFunction func, Value[] locals)
    {
        // Сохраняем предыдущий фрейм и переключаемся на новый.
        if (CurrentFunc != null && CurrentLocals != null)
        {
            CallStack.Push(new CallFrame(CurrentFunc, CurrentLocals));
        }

        CurrentFunc = func;
        CurrentLocals = locals;
    }

    protected void ExitFunctionFrame()
    {
        // Возврат к предыдущему фрейму после завершения функции.
        if (CallStack.Count == 0)
        {
            CurrentFunc = null;
            CurrentLocals = null;
            return;
        }

        var frame = CallStack.Pop();
        CurrentFunc = frame.Function;
        CurrentLocals = frame.Locals;
    }

    protected static Value CoerceToType(BytecodeType type, Value value)
    {
        // Приведение значения к типу (сейчас важно для long).
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

    protected Value CoerceToLocalType(int slot, Value value)
    {
        // Приведение для локального слота (если известен тип локала).
        if (CurrentFunc != null && slot >= 0 && slot < CurrentFunc.Locals.Count)
        {
            return CoerceToType(CurrentFunc.Locals[slot].Type, value);
        }

        return value;
    }

    public IEnumerable<nint> EnumerateRoots()
    {
        // Сбор корневых ссылок для GC: стек, локалы, фреймы, глобалы.
        foreach (var val in EnumerateStackValues())
        {
            if (val.Kind == ValueKind.ObjectRef && val.Raw != 0)
            {
                yield return (nint)val.Raw;
            }
        }

        if (CurrentLocals != null)
        {
            foreach (var local in CurrentLocals)
            {
                if (local.Kind == ValueKind.ObjectRef && local.Raw != 0)
                {
                    yield return (nint)local.Raw;
                }
            }
        }

        foreach (var frame in CallStack)
        {
            foreach (var local in frame.Locals)
            {
                if (local.Kind == ValueKind.ObjectRef && local.Raw != 0)
                {
                    yield return (nint)local.Raw;
                }
            }
        }

        foreach (var global in Globals)
        {
            if (global.Kind == ValueKind.ObjectRef && global.Raw != 0)
            {
                yield return (nint)global.Raw;
            }
        }
    }

    public bool HasStack() => StackSize > 0;
    public Value LoadConst(int index) => LoadConstCore(index);

    // Стек вычислений у разных VM реализован по-разному.
    public abstract Value PopStack();
    public abstract void PushStack(Value v);
    public abstract Value PeekStack();

    // Внутренние абстракции для конкретной VM.
    protected abstract int StackSize { get; }
    protected abstract IEnumerable<Value> EnumerateStackValues();
    protected abstract Value LoadConstCore(int index);
    protected abstract void ExecuteFunction(BytecodeFunction func, bool hasReceiver);
}
