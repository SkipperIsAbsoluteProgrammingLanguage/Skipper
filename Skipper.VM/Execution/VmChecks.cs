using Skipper.Runtime.Values;

namespace Skipper.VM.Execution;

// Централизованные проверки времени выполнения (используются интерпретатором и JIT).
public static class VmChecks
{
    // Проверка null-ссылки в стиле VM (null или нулевой указатель).
    public static void CheckNull(Value refVal)
    {
        if (refVal.Kind == ValueKind.Null || (refVal.Kind == ValueKind.ObjectRef && refVal.Raw == 0))
        {
            throw new NullReferenceException("Null pointer exception");
        }
    }
}
