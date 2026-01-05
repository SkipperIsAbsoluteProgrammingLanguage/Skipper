using Skipper.Runtime;
using Skipper.Runtime.Abstractions;

namespace Skipper.VM;

public sealed class Vm : IRootProvider
{
    private readonly RuntimeContext _runtime;

    public Vm(RuntimeContext runtime)
    {
        _runtime = runtime;
    }

    public void Collect()
    {
        _runtime.Collect(this);
    }

    public IEnumerable<nint> EnumerateRoots()
    {
        throw new NotImplementedException();
    }
}