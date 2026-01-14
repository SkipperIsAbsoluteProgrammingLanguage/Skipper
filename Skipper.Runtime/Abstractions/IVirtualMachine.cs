using Skipper.Runtime.Values;

namespace Skipper.Runtime.Abstractions;

public interface IVirtualMachine
{
    Value PopStack();
    void PushStack(Value v);
    Value PeekStack();
}