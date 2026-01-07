namespace Skipper.BaitCode.Objects.Instructions;

public sealed class Instruction(OpCode opCode, params object[] operands)
{
    public OpCode OpCode { get; } = opCode;
    public IReadOnlyList<object> Operands { get; } = operands;

    public override string ToString()
    {
        return Operands.Count == 0 ? OpCode.ToString() : $"{OpCode} {string.Join(", ", Operands)}";
    }
}