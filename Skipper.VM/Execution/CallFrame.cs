using Skipper.BaitCode.Objects;
using Skipper.Runtime.Values;

namespace Skipper.VM.Execution;

public readonly struct CallFrame
{
    // Функция, в рамках которой были локальные переменные.
    public BytecodeFunction Function { get; }
    // Снимок массива локалов на момент вызова.
    public Value[] Locals { get; }

    public CallFrame(BytecodeFunction function, Value[] locals)
    {
        Function = function;
        Locals = locals;
    }
}
