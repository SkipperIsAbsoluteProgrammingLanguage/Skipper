using Skipper.BaitCode.Objects;
using Skipper.Runtime;
using Skipper.Runtime.Abstractions;
using Skipper.Runtime.Values;

namespace Skipper.VM.Interpreter;

public interface IInterpreterContext : IVirtualMachine, IRootProvider
{
    BytecodeProgram Program { get; }
    RuntimeContext Runtime { get; }
    bool Trace { get; }

    bool HasStack();
    Value LoadConst(int index);
    Value LoadLocal(int slot);
    void StoreLocal(int slot, Value value);
    Value LoadGlobal(int slot);
    void StoreGlobal(int slot, Value value);

    void CallFunction(int functionId);
    void CallMethod(int classId, int methodId);
    void CallNative(int nativeId);
    BytecodeClass GetClassById(int classId);
}
