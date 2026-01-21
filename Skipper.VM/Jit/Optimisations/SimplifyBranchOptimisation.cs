using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using BytecodeOpCode = Skipper.BaitCode.Objects.Instructions.OpCode;

namespace Skipper.VM.Jit.Optimisations;

public static class SimplifyBranchOptimisation
{
    public static List<Instruction> SimplifyBranches(BytecodeFunction func, BytecodeProgram program)
    {
        var oldCode = func.Code;
        if (oldCode.Count < 2)
        {
            return oldCode;
        }

        var newCode = new List<Instruction>(oldCode.Count);
        var map = new int[oldCode.Count + 1];
        Array.Fill(map, -1);
        var jumpFixups = new List<int>();

        for (var i = 0; i < oldCode.Count; i++)
        {
            if (i + 3 < oldCode.Count &&
    oldCode[i].OpCode == BytecodeOpCode.PUSH &&
    oldCode[i + 1].OpCode == BytecodeOpCode.PUSH &&
    OptimisationTools.IsCmp(oldCode[i + 2].OpCode) &&
    (oldCode[i + 3].OpCode == BytecodeOpCode.JUMP_IF_TRUE ||
     oldCode[i + 3].OpCode == BytecodeOpCode.JUMP_IF_FALSE))
            {
                if (OptimisationTools.TryGetConst(program, oldCode[i].Operands[0], out var c1) &&
                    OptimisationTools.TryGetConst(program, oldCode[i + 1].Operands[0], out var c2) &&
                    OptimisationTools.TryFoldCmp(oldCode[i + 2].OpCode, c1, c2, out var cmpResult))
                {
                    var jump = oldCode[i + 3];
                    var target = Convert.ToInt32(jump.Operands[0]);

                    var take =
                        (jump.OpCode == BytecodeOpCode.JUMP_IF_TRUE && cmpResult) ||
                        (jump.OpCode == BytecodeOpCode.JUMP_IF_FALSE && !cmpResult);

                    if (take)
                    {
                        newCode.Add(new Instruction(BytecodeOpCode.JUMP, target));
                        jumpFixups.Add(newCode.Count - 1);
                        map[i] = map[i + 1] = map[i + 2] = map[i + 3] = newCode.Count - 1;
                    } else
                    {
                        map[i] = map[i + 1] = map[i + 2] = map[i + 3] = newCode.Count;
                    }

                    i += 3;
                    continue;
                }
            }

            if (i + 1 < oldCode.Count &&
                oldCode[i].OpCode == BytecodeOpCode.PUSH &&
                (oldCode[i + 1].OpCode == BytecodeOpCode.JUMP_IF_FALSE ||
                 oldCode[i + 1].OpCode == BytecodeOpCode.JUMP_IF_TRUE))
            {
                var constId = Convert.ToInt32(oldCode[i].Operands[0]);
                if (OptimisationTools.TryGetConstBool(program, constId, out var cond))
                {
                    var next = oldCode[i + 1];
                    var target = Convert.ToInt32(next.Operands[0]);
                    var take = (next.OpCode == BytecodeOpCode.JUMP_IF_TRUE && cond) ||
                               (next.OpCode == BytecodeOpCode.JUMP_IF_FALSE && !cond);

                    if (take)
                    {
                        newCode.Add(new Instruction(BytecodeOpCode.JUMP, target));
                        jumpFixups.Add(newCode.Count - 1);
                        map[i] = newCode.Count - 1;
                        map[i + 1] = newCode.Count - 1;
                    } else
                    {
                        map[i] = newCode.Count;
                        map[i + 1] = newCode.Count;
                    }

                    i++;
                    continue;
                }
            }

            var instr = oldCode[i];
            newCode.Add(instr);
            map[i] = newCode.Count - 1;
            if (instr.OpCode is BytecodeOpCode.JUMP or BytecodeOpCode.JUMP_IF_FALSE or BytecodeOpCode.JUMP_IF_TRUE)
            {
                jumpFixups.Add(newCode.Count - 1);
            }
        }

        map[oldCode.Count] = newCode.Count;

        var nextNew = newCode.Count;
        for (var i = oldCode.Count; i >= 0; i--)
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
            var instr = newCode[idx];
            var oldTarget = Convert.ToInt32(instr.Operands[0]);
            var newTarget = map[oldTarget];
            newCode[idx] = new Instruction(instr.OpCode, newTarget);
        }

        return newCode;
    }
}