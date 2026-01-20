using Skipper.Runtime.Objects;

namespace Skipper.Runtime.Memory;

public sealed class Heap
{
    private readonly List<HeapObject> _objects = [];
    private readonly long _maxSize;

    public IReadOnlyList<HeapObject> Objects => _objects;
    public long AllocatedBytes { get; private set; }
    public long MaxSize => _maxSize;
    public long FreeBytes => _maxSize - AllocatedBytes;

    public Heap(long maxSize = 1024 * 1024)
    {
        _maxSize = maxSize;
        AllocatedBytes = 0;
    }

    public bool HasSpace(int size)
    {
        return (AllocatedBytes + size) <= _maxSize;
    }

    public unsafe nint Allocate(ObjectDescriptor descriptor, int size)
    {
        if (!HasSpace(size))
        {
            throw new OutOfMemoryException($"Not enough memory. Requested: {size}, Available: {_maxSize - AllocatedBytes}");
        }

        // Выделение памяти происходит внутри HeapObject
        HeapObject obj = new(descriptor, size);

        _objects.Add(obj);
        AllocatedBytes += size;

        return (nint)obj.Data;
    }

    public unsafe HeapObject? FindObject(nint address)
    {
        // Поиск объекта, которому принадлежит адрес
        for (var i = _objects.Count - 1; i >= 0; i--)
        {
            var obj = _objects[i];
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
        AllocatedBytes -= obj.Size;
        obj.Free();
        _ = _objects.Remove(obj);
    }

    public unsafe long ReadInt64(nint basePtr, int offset)
    {
        var targetPtr = (byte*)basePtr + offset;
        return *(long*)targetPtr;
    }

    public unsafe void WriteInt64(nint basePtr, int offset, long value)
    {
        var targetPtr = (byte*)basePtr + offset;
        *(long*)targetPtr = value;
    }
}
