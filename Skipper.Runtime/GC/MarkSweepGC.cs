using Skipper.Runtime.Abstractions;
using Skipper.Runtime.Memory;

namespace Skipper.Runtime.GC;

public sealed class MarkSweepGc : IGarbageCollector
{
    private readonly Heap _heap;

    public MarkSweepGc(Heap heap)
    {
        _heap = heap;
    }

    public void Collect(IRootProvider roots)
    {
        Mark(roots);
        Sweep();
    }

    private void Mark(IRootProvider roots)
    {
        var stack = new Stack<nint>();

        foreach (var root in roots.EnumerateRoots())
        {
            if (root != 0)
            {
                stack.Push(root);
            }
        }

        while (stack.Count > 0)
        {
            var ptr = stack.Pop();
            var obj = _heap.FindObject(ptr);

            if (obj == null || obj.Marked)
            {
                continue;
            }

            obj.Marked = true;

            foreach (var child in obj.EnumerateReferences())
            {
                stack.Push(child);
            }
        }
    }

    private void Sweep()
    {
        var dead = new List<HeapObject>();

        foreach (var obj in _heap.Objects)
        {
            if (!obj.Marked)
            {
                dead.Add(obj);
            }
            else
            {
                obj.Marked = false;
            }
        }

        foreach (var obj in dead)
        {
            _heap.Free(obj);
        }
    }
}