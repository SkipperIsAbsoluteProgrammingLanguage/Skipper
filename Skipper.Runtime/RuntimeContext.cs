using Skipper.Runtime.Abstractions;
using Skipper.Runtime.GC;
using Skipper.Runtime.Memory;
using Skipper.Runtime.Objects;

namespace Skipper.Runtime;

public sealed class RuntimeContext
{
    private readonly Heap _heap;
    private readonly IGarbageCollector _gc;

    public RuntimeContext()
    {
        _heap = new Heap();
        _gc = new MarkSweepGc(_heap);
    }

    public nint Allocate(ObjectDescriptor desc, int size)
    {
        return _heap.Allocate(desc, size);
    }

    public void Collect(IRootProvider roots)
    {
        _gc.Collect(roots);
    }
}