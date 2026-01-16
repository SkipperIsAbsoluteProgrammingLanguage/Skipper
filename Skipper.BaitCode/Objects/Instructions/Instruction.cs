using System.Text.Json.Serialization;

namespace Skipper.BaitCode.Objects.Instructions;

public sealed class Instruction
{
    public OpCode OpCode { get; set; }
    [JsonInclude]
    public IReadOnlyList<object> Operands { get; set; } = [];

    [JsonConstructor]
    public Instruction() { }

    public Instruction(OpCode opCode, params object[] operands)
    {
        OpCode = opCode;
        Operands = new List<object>(operands);
    }

    public override string ToString()
    {
        return Operands.Count == 0 ? OpCode.ToString() : $"{OpCode} {string.Join(", ", Operands)}";
    }
}
