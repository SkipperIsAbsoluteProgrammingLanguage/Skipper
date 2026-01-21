using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;

namespace Skipper.VM.Jit.Optimisations;

public static class PeepholeOptimisation
{
    public static List<Instruction> PeepholeOptimize(
        List<Instruction> code,
        BytecodeProgram program)
    {
        var result = new List<Instruction>(code.Count);
        var map = new int[code.Count + 1];
        Array.Fill(map, -1);
        var jumpFixups = new List<int>();

        for (var i = 0; i < code.Count; i++)
        {
            if (i + 1 < code.Count &&
    code[i].OpCode == OpCode.PUSH &&
    code[i + 1].OpCode == OpCode.POP)
            {
                map[i] = result.Count;
                map[i + 1] = result.Count;
                i++;
                continue;
            }

            if (i + 1 < code.Count &&
    code[i].OpCode == OpCode.DUP &&
    code[i + 1].OpCode == OpCode.POP)
            {
                map[i] = result.Count;
                map[i + 1] = result.Count;
                i++;
                continue;
            }

            if (i + 2 < code.Count &&
    code[i].OpCode == OpCode.PUSH &&
    code[i + 1].OpCode == OpCode.PUSH)
            {
                var op = code[i + 2].OpCode;
                if (OptimisationTools.IsFoldableBinary(op) &&
                    OptimisationTools.TryGetConst(program, code[i].Operands[0], out var c1) &&
                    OptimisationTools.TryGetConst(program, code[i + 1].Operands[0], out var c2) &&
                    OptimisationTools.TryFoldBinary(op, c1, c2, out var folded))
                {
                    var id = program.ConstantPool.Count;
                    program.ConstantPool.Add(folded);
                    result.Add(new Instruction(OpCode.PUSH, id));
                    var newIndex = result.Count - 1;
                    map[i] = newIndex;
                    map[i + 1] = newIndex;
                    map[i + 2] = newIndex;
                    i += 2;
                    continue;
                }
            }

            if (i + 1 < code.Count &&
    code[i].OpCode == OpCode.LOAD_LOCAL &&
    code[i + 1].OpCode == OpCode.STORE_LOCAL &&
    Equals(code[i].Operands[1], code[i + 1].Operands[1]))
            {
                map[i] = result.Count;
                map[i + 1] = result.Count;
                i++;
                continue;
            }

            if (code[i].OpCode == OpCode.JUMP)
            {
                var target = Convert.ToInt32(code[i].Operands[0]);
                if (target < code.Count && code[target].OpCode == OpCode.JUMP)
                {
                    result.Add(new Instruction(OpCode.JUMP, Convert.ToInt32(code[target].Operands[0])));
                    map[i] = result.Count - 1;
                    jumpFixups.Add(result.Count - 1);
                    continue;
                }
            }

            result.Add(code[i]);
            map[i] = result.Count - 1;
            if (OptimisationTools.IsJump(code[i].OpCode))
            {
                jumpFixups.Add(result.Count - 1);
            }
        }

        map[code.Count] = result.Count;
        var nextNew = result.Count;
        for (var i = code.Count; i >= 0; i--)
        {
            if (map[i] >= 0)
            {
                nextNew = map[i];
            } else
            {
                map[i] = nextNew;
            }
        }

        foreach (var idx in jumpFixups)
        {
            var instr = result[idx];
            var oldTarget = Convert.ToInt32(instr.Operands[0]);
            var newTarget = map[oldTarget];
            result[idx] = new Instruction(instr.OpCode, newTarget);
        }

        return result;
    }
}