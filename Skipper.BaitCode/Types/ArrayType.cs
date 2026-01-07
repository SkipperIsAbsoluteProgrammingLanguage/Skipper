namespace Skipper.BaitCode.Types;

public sealed class ArrayType(BytecodeType element) : BytecodeType
{
    public BytecodeType ElementType { get; } = element;
}