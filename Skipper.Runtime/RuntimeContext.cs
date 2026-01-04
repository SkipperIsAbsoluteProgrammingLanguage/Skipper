using Skipper.Runtime.Abstractions;
using Skipper.Runtime.GC;
using Skipper.Runtime.Memory;

namespace Skipper.Runtime;

public sealed class RuntimeContext
{
    public Heap Heap { get; }

    public IGarbageCollector Gc { get; }

    public RuntimeContext()
    {
        Heap = new Heap();
        Gc = new MarkSweepGc(Heap);
    }

    public void Collect(IRootProvider roots)
    {
        Gc.Collect(roots);
    }
}