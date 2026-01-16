using Skipper.BaitCode.Objects;
using Skipper.Runtime.Values;

namespace Skipper.VM.Interpreter;

public static class LocalsAllocator
{
    private const int MinLocalSlots = 64;

    public static Value[] Create(BytecodeFunction func)
    {
        var totalCount = func.ParameterTypes.Count + func.Locals.Count;
        var safeSize = Math.Max(totalCount, MinLocalSlots);
        return new Value[safeSize];
    }
}
