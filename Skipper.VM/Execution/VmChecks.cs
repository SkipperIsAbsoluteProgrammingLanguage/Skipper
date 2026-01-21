using Skipper.Runtime.Values;

namespace Skipper.VM.Execution;

public static class VmChecks
{
    public static void CheckNull(Value refVal)
    {
        if (refVal.Kind == ValueKind.Null || (refVal.Kind == ValueKind.ObjectRef && refVal.Raw == 0))
        {
            throw new NullReferenceException("Null pointer exception");
        }
    }
}