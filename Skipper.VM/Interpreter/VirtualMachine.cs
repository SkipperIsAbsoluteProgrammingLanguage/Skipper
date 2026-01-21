using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Execution;

namespace Skipper.VM.Interpreter;

public sealed class VirtualMachine : ExecutionContextBase
{
    private readonly Stack<Value> _evalStack = new();

    public VirtualMachine(BytecodeProgram program, RuntimeContext runtime, bool trace = false)
        : base(program, runtime, trace)
    { }

    public Value Run(string entryPointName)
    {
        var mainFunc = Program.Functions.FirstOrDefault(f => f.Name == entryPointName);
        if (mainFunc == null)
        {
            throw new InvalidOperationException($"Function '{entryPointName}' not found");
        }

        ExecuteFunction(mainFunc, hasReceiver: false);

        return _evalStack.Count > 0 ? _evalStack.Pop() : Value.Null();
    }

    public override Value PopStack() => _evalStack.Pop();
    public override void PushStack(Value v) => _evalStack.Push(v);
    public override Value PeekStack() => _evalStack.Peek();

    protected override int StackSize => _evalStack.Count;

    protected override IEnumerable<Value> EnumerateStackValues() => _evalStack;

    protected override Value LoadConstCore(int index)
    {
        var c = Program.ConstantPool[index];
        return ValueFromConst(c);
    }

    protected override void ExecuteFunction(BytecodeFunction func, bool hasReceiver)
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

        EnterFunctionFrame(func, locals);

        try
        {
            BytecodeInterpreter.Execute(this, func);
        } finally
        {
            ExitFunctionFrame();
        }
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
        var ptr = Runtime.AllocateArray(s.Length);

        for (var i = 0; i < s.Length; i++)
        {
            Runtime.WriteArrayElement(ptr, i, Value.FromChar(s[i]));
        }

        return Value.FromObject(ptr);
    }
}
