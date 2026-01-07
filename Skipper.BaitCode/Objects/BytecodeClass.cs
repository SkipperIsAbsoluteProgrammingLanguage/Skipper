using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public sealed class BytecodeClass(int classId, string name)
{
    // Id класса
    public int ClassId { get; } = classId;
    // Имя класса
    public string Name { get; } = name;
    // Id полей и тип по названию в классе
    public Dictionary<string, (int FieldId, BytecodeType Type)> Fields { get; } = [];
    // Id методов по названию в классе
    public Dictionary<string, int> Methods { get; } = new();
    // Количество полей в классе (возможно не нужно, ведь количество памяти будет выделяться по другому)
    public int ObjectSize => Fields.Count;
}
