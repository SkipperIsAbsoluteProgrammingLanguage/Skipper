using Skipper.BaitCode.Objects;
using BytecodeOpCode = Skipper.BaitCode.Objects.Instructions.OpCode;

namespace Skipper.VM.Jit.Optimisations;

public static class OptimisationTools
{
    public static bool TryGetConst(BytecodeProgram program, object operand, out object? value)
    {
        value = null;
        var id = Convert.ToInt32(operand);
        if (id < 0 || id >= program.ConstantPool.Count)
        {
            return false;
        }

        value = program.ConstantPool[id];
        return true;
    }

    public static bool IsCmp(BytecodeOpCode op) =>
        op is BytecodeOpCode.CMP_EQ
            or BytecodeOpCode.CMP_NE
            or BytecodeOpCode.CMP_LT
            or BytecodeOpCode.CMP_GT
            or BytecodeOpCode.CMP_LE
            or BytecodeOpCode.CMP_GE;

    public static bool TryGetConstBool(BytecodeProgram program, int constId, out bool value)
    {
        value = false;
        if (constId < 0 || constId >= program.ConstantPool.Count)
        {
            return false;
        }

        var c = program.ConstantPool[constId];
        switch (c)
        {
            case null:
                value = false;
                return true;
            case bool b:
                value = b;
                return true;
            case int i:
                value = i != 0;
                return true;
            case long l:
                value = l != 0;
                return true;
            case double d:
                value = Math.Abs(d) > double.Epsilon;
                return true;
            case char ch:
                value = ch != '\0';
                return true;
            case string:
                value = true;
                return true;
            default:
                return false;
        }
    }

    public static bool TryFoldCmp(
        BytecodeOpCode op,
        object? left,
        object? right,
        out bool result)
    {
        result = false;
        if (left == null || right == null)
        {
            return false;
        }

        switch (left)
        {
            case int li when right is int ri:
                result = op switch
                {
                    BytecodeOpCode.CMP_EQ => li == ri,
                    BytecodeOpCode.CMP_NE => li != ri,
                    BytecodeOpCode.CMP_LT => li < ri,
                    BytecodeOpCode.CMP_GT => li > ri,
                    BytecodeOpCode.CMP_LE => li <= ri,
                    BytecodeOpCode.CMP_GE => li >= ri,
                    _ => false
                };
                return true;

            case long ll when right is long rl:
                result = op switch
                {
                    BytecodeOpCode.CMP_EQ => ll == rl,
                    BytecodeOpCode.CMP_NE => ll != rl,
                    BytecodeOpCode.CMP_LT => ll < rl,
                    BytecodeOpCode.CMP_GT => ll > rl,
                    BytecodeOpCode.CMP_LE => ll <= rl,
                    BytecodeOpCode.CMP_GE => ll >= rl,
                    _ => false
                };
                return true;

            case double ld when right is double rd:
                result = op switch
                {
                    BytecodeOpCode.CMP_EQ => Math.Abs(ld - rd) < double.Epsilon,
                    BytecodeOpCode.CMP_NE => Math.Abs(ld - rd) >= double.Epsilon,
                    BytecodeOpCode.CMP_LT => ld < rd,
                    BytecodeOpCode.CMP_GT => ld > rd,
                    BytecodeOpCode.CMP_LE => ld <= rd,
                    BytecodeOpCode.CMP_GE => ld >= rd,
                    _ => false
                };
                return true;

            case char lc when right is char rc:
                result = op switch
                {
                    BytecodeOpCode.CMP_EQ => lc == rc,
                    BytecodeOpCode.CMP_NE => lc != rc,
                    BytecodeOpCode.CMP_LT => lc < rc,
                    BytecodeOpCode.CMP_GT => lc > rc,
                    BytecodeOpCode.CMP_LE => lc <= rc,
                    BytecodeOpCode.CMP_GE => lc >= rc,
                    _ => false
                };
                return true;
        }

        return false;
    }

    public static bool IsFoldableBinary(BytecodeOpCode op) =>
        op is BytecodeOpCode.ADD
            or BytecodeOpCode.SUB
            or BytecodeOpCode.MUL
            or BytecodeOpCode.DIV
            or BytecodeOpCode.MOD
            or BytecodeOpCode.AND
            or BytecodeOpCode.OR;

    public static bool TryFoldBinary(
        BytecodeOpCode op,
        object? left,
        object? right,
        out object result)
    {
        result = null!;
        if (left == null || right == null)
        {
            return false;
        }

        switch (left)
        {
            // INT 
            case int li when right is int ri:
                switch (op)
                {
                    case BytecodeOpCode.ADD:
                        result = li + ri;
                        return true;
                    case BytecodeOpCode.SUB:
                        result = li - ri;
                        return true;
                    case BytecodeOpCode.MUL:
                        result = li * ri;
                        return true;
                    case BytecodeOpCode.DIV:
                        if (ri == 0)
                            return false;
                        result = li / ri;
                        return true;
                    case BytecodeOpCode.MOD:
                        if (ri == 0)
                            return false;
                        result = li % ri;
                        return true;
                }

                break;
            // LONG
            case long ll when right is long rl:
                switch (op)
                {
                    case BytecodeOpCode.ADD:
                        result = ll + rl;
                        return true;
                    case BytecodeOpCode.SUB:
                        result = ll - rl;
                        return true;
                    case BytecodeOpCode.MUL:
                        result = ll * rl;
                        return true;
                    case BytecodeOpCode.DIV:
                        if (rl == 0)
                            return false;
                        result = ll / rl;
                        return true;
                    case BytecodeOpCode.MOD:
                        if (rl == 0)
                            return false;
                        result = ll % rl;
                        return true;
                }

                break;
            // DOUBLE
            case double ld when right is double rd:
                switch (op)
                {
                    case BytecodeOpCode.ADD:
                        result = ld + rd;
                        return true;
                    case BytecodeOpCode.SUB:
                        result = ld - rd;
                        return true;
                    case BytecodeOpCode.MUL:
                        result = ld * rd;
                        return true;
                    case BytecodeOpCode.DIV:
                        if (Math.Abs(rd) < double.Epsilon)
                            return false;
                        result = ld / rd;
                        return true;
                    case BytecodeOpCode.MOD:
                        if (Math.Abs(rd) < double.Epsilon)
                            return false;
                        result = ld % rd;
                        return true;
                }

                break;
            // CHAR
            case char lc when right is char rc:
                switch (op)
                {
                    case BytecodeOpCode.ADD:
                        result = (char)(lc + rc);
                        return true;
                    case BytecodeOpCode.SUB:
                        result = (char)(lc - rc);
                        return true;
                    case BytecodeOpCode.MUL:
                        result = (char)(lc * rc);
                        return true;
                    case BytecodeOpCode.DIV:
                        if (rc == 0)
                            return false;
                        result = (char)(lc / rc);
                        return true;
                    case BytecodeOpCode.MOD:
                        if (rc == 0)
                            return false;
                        result = (char)(lc % rc);
                        return true;
                }

                break;
            // BOOL
            case bool lb when right is bool rb:
                switch (op)
                {
                    case BytecodeOpCode.AND:
                        result = lb && rb;
                        return true;
                    case BytecodeOpCode.OR:
                        result = lb || rb;
                        return true;
                }

                break;
        }


        return false;
    }

    public static bool IsJump(BytecodeOpCode op) =>
        op is BytecodeOpCode.JUMP or BytecodeOpCode.JUMP_IF_FALSE or BytecodeOpCode.JUMP_IF_TRUE;
}