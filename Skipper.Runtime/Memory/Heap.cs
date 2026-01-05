using Skipper.Runtime.Objects;

namespace Skipper.Runtime.Memory;

public sealed class Heap
{
    private readonly List<HeapObject> _objects = [];

    public IReadOnlyList<HeapObject> Objects => _objects;

    public unsafe nint Allocate(ObjectDescriptor descriptor, int size)
    {
        var obj = new HeapObject(descriptor, size);
        _objects.Add(obj);
        return (nint)obj.Data;
    }

    public unsafe HeapObject? FindObject(nint address)
    {
        foreach (var obj in _objects)
        {
            var start = (nint)obj.Data;
            var end = start + obj.Size;

            if (address >= start && address < end)
            {
                return obj;
            }
        }

        return null;
    }

    public void Free(HeapObject obj)
    {
        obj.Free();
        _objects.Remove(obj);
    }
}