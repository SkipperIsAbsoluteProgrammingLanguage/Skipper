using Skipper.BaitCode.Objects;
using Skipper.Runtime.Values;

namespace Skipper.VM.Jit;

internal readonly struct JitFrame
{
    public BytecodeFunction Function { get; }
    public Value[] Locals { get; }

    public JitFrame(BytecodeFunction function, Value[] locals)
    {
        Function = function;
        Locals = locals;
    }
}
