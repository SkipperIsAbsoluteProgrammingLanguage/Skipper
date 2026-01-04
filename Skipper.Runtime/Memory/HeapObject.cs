using System.Runtime.InteropServices;
using Skipper.Runtime.Objects;

namespace Skipper.Runtime.Memory;

public sealed unsafe class HeapObject
{
    public ObjectDescriptor Descriptor { get; }
    public byte* Data { get; }
    public int Size { get; }
    public bool Marked { get; set; }

    public HeapObject(ObjectDescriptor descriptor, int size)
    {
        Descriptor = descriptor;
        Size = size;
        Data = (byte*)NativeMemory.Alloc((nuint)size);
    }

    public IEnumerable<nint> EnumerateReferences()
    {
        return Descriptor.ReferenceOffsets
            .Select(offset => *(nint*)(Data + offset))
            .Where(ptr => ptr != 0);
    }

    public void Free()
    {
        NativeMemory.Free(Data);
    }
}