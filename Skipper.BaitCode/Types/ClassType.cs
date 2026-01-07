namespace Skipper.BaitCode.Types;

public sealed class ClassType(int classId, string name) : BytecodeType
{
    public int ClassId { get; } = classId;
    public string Name { get; } = name;
}