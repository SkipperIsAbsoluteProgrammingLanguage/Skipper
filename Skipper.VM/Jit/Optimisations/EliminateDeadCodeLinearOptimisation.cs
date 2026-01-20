using Skipper.BaitCode.Objects.Instructions;
using BytecodeOpCode = Skipper.BaitCode.Objects.Instructions.OpCode;

namespace Skipper.VM.Jit.Optimisations;

public static class EliminateDeadCodeLinearOptimisation
{
    public static List<Instruction> EliminateDeadCodeLinear(List<Instruction> code)
    {
        var targets = new HashSet<int>();

        for (var i = 0; i < code.Count; i++)
        {
            if (OptimisationTools.IsJump(code[i].OpCode))
            {
                targets.Add(Convert.ToInt32(code[i].Operands[0]));
            }
        }

        var result = new List<Instruction>(code.Count);
        var map = new int[code.Count + 1];
        Array.Fill(map, -1);

        var dead = false;

        for (var i = 0; i < code.Count; i++)
        {
            if (targets.Contains(i))
            {
                dead = false;
            }

            if (dead)
            {
                map[i] = result.Count;
                continue;
            }

            var instr = code[i];
            result.Add(instr);
            map[i] = result.Count - 1;

            if (instr.OpCode is BytecodeOpCode.JUMP or BytecodeOpCode.RETURN)
            {
                dead = true;
            }
        }

        map[code.Count] = result.Count;

        var next = result.Count;
        for (var i = code.Count; i >= 0; i--)
        {
            if (map[i] >= 0)
            {
                next = map[i];
            }
            else
            {
                map[i] = next;
            }
        }

        for (var i = 0; i < result.Count; i++)
        {
            if (OptimisationTools.IsJump(result[i].OpCode))
            {
                var oldTarget = Convert.ToInt32(result[i].Operands[0]);
                var newTarget = map[oldTarget];
                result[i] = new Instruction(result[i].OpCode, newTarget);
            }
        }

        return result;
    }
}