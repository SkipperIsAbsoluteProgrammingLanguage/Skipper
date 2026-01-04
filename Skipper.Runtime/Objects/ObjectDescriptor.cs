namespace Skipper.Runtime.Objects;

public sealed class ObjectDescriptor
{
    public ObjectKind Kind { get; }
    public IReadOnlyList<int> ReferenceOffsets { get; }

    public ObjectDescriptor(ObjectKind kind, IReadOnlyList<int> referenceOffsets)
    {
        Kind = kind;
        ReferenceOffsets = referenceOffsets;
    }
}