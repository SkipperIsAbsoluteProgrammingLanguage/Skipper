using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Abstractions;
using Skipper.Runtime.Values;

namespace Skipper.VM.Execution;

// Общий контракт исполнения, который используют и интерпретатор, и JIT.
// Здесь только то, что нужно интерпретатору байткода и рантайму.
public interface IInterpreterContext : IVirtualMachine, IRootProvider
{
    // Исходная программа с байткодом и метаданными.
    BytecodeProgram Program { get; }
    // Доступ к рантайму (куча, нативные функции, GC).
    RuntimeContext Runtime { get; }
    // Флаг трассировки шагов исполнения.
    bool Trace { get; }

    // Операции со стеком вычислений.
    bool HasStack();
    // Загрузка константы из пула.
    Value LoadConst(int index);
    // Доступ к локальным и глобальным переменным.
    Value LoadLocal(int slot);
    void StoreLocal(int slot, Value value);
    Value LoadGlobal(int slot);
    void StoreGlobal(int slot, Value value);

    // Вызовы функций/методов/нативных функций.
    void CallFunction(int functionId);
    void CallMethod(int classId, int methodId);
    void CallNative(int nativeId);
    // Получение описания класса по ID.
    BytecodeClass GetClassById(int classId);
}
