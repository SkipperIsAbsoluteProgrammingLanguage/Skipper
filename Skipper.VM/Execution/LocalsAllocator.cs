using Skipper.BaitCode.Objects;
using Skipper.Runtime.Values;

namespace Skipper.VM.Execution;

public static class LocalsAllocator
{
    // Минимальный размер локального массива, чтобы избежать частых аллокаций на мелких функциях.
    private const int MinLocalSlots = 64;

    public static Value[] Create(BytecodeFunction func)
    {
        // Локалы включают параметры и локальные переменные функции.
        var totalCount = func.ParameterTypes.Count + func.Locals.Count;
        var safeSize = Math.Max(totalCount, MinLocalSlots);
        return new Value[safeSize];
    }
}
