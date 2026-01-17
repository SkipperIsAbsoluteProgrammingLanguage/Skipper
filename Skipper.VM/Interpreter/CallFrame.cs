using Skipper.BaitCode.Objects;
using Skipper.Runtime.Values;

namespace Skipper.VM.Interpreter;

public readonly struct CallFrame
{
    public BytecodeFunction Function { get; }
    public Value[] Locals { get; }

    public CallFrame(BytecodeFunction function, Value[] locals)
    {
        Function = function;
        Locals = locals;
    }
}
