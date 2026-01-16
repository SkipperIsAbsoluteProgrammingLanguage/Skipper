using Skipper.Runtime.Values;
using Skipper.VM.Interpreter;

namespace Skipper.VM.Jit;

internal static class JitOps
{
    internal static Value FromConst(JitExecutionContext ctx, object c)
    {
        return c switch
        {
            null => Value.Null(),
            int i => Value.FromInt(i),
            long l => Value.FromInt((int)l),
            double d => Value.FromDouble(d),
            bool b => Value.FromBool(b),
            char ch => Value.FromChar(ch),
            string s => Value.FromObject(ctx.Runtime.AllocateString(s)),
            _ => throw new NotImplementedException($"Const type {c.GetType()} not supported")
        };
    }

    internal static Value Add(JitExecutionContext ctx, Value a, Value b)
    {
        if (a.Kind == ValueKind.ObjectRef && b.Kind == ValueKind.ObjectRef)
        {
            var newPtr = ctx.Runtime.ConcatStrings(a.AsObject(), b.AsObject());
            return Value.FromObject(newPtr);
        }

        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
        {
            var d1 = a.Kind == ValueKind.Double ? a.AsDouble() : a.AsInt();
            var d2 = b.Kind == ValueKind.Double ? b.AsDouble() : b.AsInt();
            return Value.FromDouble(d1 + d2);
        }

        return Value.FromInt(a.AsInt() + b.AsInt());
    }

    internal static Value Sub(Value a, Value b)
    {
        return Value.FromInt(a.AsInt() - b.AsInt());
    }

    internal static Value Mul(Value a, Value b)
    {
        return Value.FromInt(a.AsInt() * b.AsInt());
    }

    internal static Value Div(Value a, Value b)
    {
        var divisor = b.AsInt();
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        return Value.FromInt(a.AsInt() / divisor);
    }

    internal static Value Mod(Value a, Value b)
    {
        var divisor = b.AsInt();
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        return Value.FromInt(a.AsInt() % divisor);
    }

    internal static Value Neg(Value v)
    {
        if (v.Kind == ValueKind.Double)
        {
            return Value.FromDouble(-v.AsDouble());
        }

        return Value.FromInt(-v.AsInt());
    }

    internal static Value CmpEq(Value a, Value b)
    {
        return Value.FromBool(a.Raw == b.Raw);
    }

    internal static Value CmpNe(Value a, Value b)
    {
        return Value.FromBool(a.Raw != b.Raw);
    }

    internal static Value CmpLt(Value a, Value b)
    {
        return Value.FromBool(a.AsInt() < b.AsInt());
    }

    internal static Value CmpGt(Value a, Value b)
    {
        return Value.FromBool(a.AsInt() > b.AsInt());
    }

    internal static Value CmpLe(Value a, Value b)
    {
        return Value.FromBool(a.AsInt() <= b.AsInt());
    }

    internal static Value CmpGe(Value a, Value b)
    {
        return Value.FromBool(a.AsInt() >= b.AsInt());
    }

    internal static Value And(Value a, Value b)
    {
        return Value.FromBool(a.AsBool() && b.AsBool());
    }

    internal static Value Or(Value a, Value b)
    {
        return Value.FromBool(a.AsBool() || b.AsBool());
    }

    internal static Value Not(Value a)
    {
        return Value.FromBool(!a.AsBool());
    }

    internal static bool IsTrue(Value v)
    {
        return v.AsBool();
    }

    internal static Value NewObject(JitExecutionContext ctx, int classId)
    {
        var cls = ctx.GetClassById(classId);
        var payloadSize = cls.Fields.Count * 8;

        if (!ctx.Runtime.CanAllocate(payloadSize))
        {
            ctx.Runtime.Collect(ctx);
            if (!ctx.Runtime.CanAllocate(payloadSize))
            {
                throw new OutOfMemoryException("Heap full after GC");
            }
        }

        var ptr = ctx.Runtime.AllocateObject(payloadSize, classId);
        return Value.FromObject(ptr);
    }

    internal static Value NewArray(JitExecutionContext ctx, Value lengthValue)
    {
        var length = lengthValue.AsInt();
        if (length < 0)
        {
            throw new InvalidOperationException("Array size cannot be negative");
        }

        var payloadSize = length * 8;
        if (!ctx.Runtime.CanAllocate(payloadSize))
        {
            ctx.Runtime.Collect(ctx);
            if (!ctx.Runtime.CanAllocate(payloadSize))
            {
                throw new OutOfMemoryException("Heap full after GC (Array)");
            }
        }

        var ptr = ctx.Runtime.AllocateArray(length);
        return Value.FromObject(ptr);
    }

    internal static Value GetField(JitExecutionContext ctx, Value objRef, int fieldId)
    {
        VmChecks.CheckNull(objRef);
        return ctx.Runtime.ReadField(objRef.AsObject(), fieldId);
    }

    internal static void SetField(JitExecutionContext ctx, Value objRef, int fieldId, Value value)
    {
        VmChecks.CheckNull(objRef);
        ctx.Runtime.WriteField(objRef.AsObject(), fieldId, value);
    }

    internal static Value GetElement(JitExecutionContext ctx, Value arrRef, Value indexValue)
    {
        VmChecks.CheckNull(arrRef);
        var index = indexValue.AsInt();
        return ctx.Runtime.ReadArrayElement(arrRef.AsObject(), index);
    }

    internal static void SetElement(JitExecutionContext ctx, Value arrRef, Value indexValue, Value value)
    {
        VmChecks.CheckNull(arrRef);
        var index = indexValue.AsInt();
        ctx.Runtime.WriteArrayElement(arrRef.AsObject(), index, value);
    }
}
