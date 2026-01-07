namespace Skipper.BaitCode.Types;

public sealed class PrimitiveType(string name) : BytecodeType
{
    public string Name { get; } = name;
}