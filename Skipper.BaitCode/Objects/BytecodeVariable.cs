using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public class BytecodeVariable(int variableId, string name, BytecodeType type, int offset)
{
    // Id переменной
    public int VariableId { get; set; } = variableId;
    // Название переменной
    public string Name { get; } = name;
    // Тип переменной
    public BytecodeType Type { get; } = type;
    // Смещение переменной в области памяти (где хранится)
    public int Offset { get; } = offset;
}