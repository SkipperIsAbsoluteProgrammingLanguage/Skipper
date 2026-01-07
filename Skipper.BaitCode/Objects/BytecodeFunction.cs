using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

public class BytecodeFunction(
    int id,
    string name,
    BytecodeType returnType,
    List<(string Names, BytecodeType Type)> parameters)
{
    // Id, также может содержаться и в классах
    public int FunctionId { get; set; } = id;
    // Название функции
    public string Name { get; set; } = name;

    // Сигнатура
    public BytecodeType ReturnType { get; set; } = returnType;
    public List<(string Names, BytecodeType Type)> ParameterTypes { get; set; } = parameters;

    // Конкретные инструкции
    public List<Instruction> Code { get; set; } = [];
    public List<BytecodeVariable> Locals { get; } = [];
}
