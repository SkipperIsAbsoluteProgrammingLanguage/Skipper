using Skipper.BaitCode.Types;
using Skipper.Runtime.Values;
using Skipper.VM.Execution;

namespace Skipper.VM.Jit;

internal static class JitOps
{
    internal static Value FromConst(JitExecutionContext ctx, object c)
    {
        return c switch
        {
            null => Value.Null(),
            int i => Value.FromInt(i),
            long l => Value.FromLong(l),
            double d => Value.FromDouble(d),
            bool b => Value.FromBool(b),
            char ch => Value.FromChar(ch),
            string s => Value.FromObject(ctx.Runtime.AllocateString(s)),
            _ => throw new NotImplementedException($"Const type {c.GetType()} not supported")
        };
    }

    internal static Value Add(JitExecutionContext ctx, Value a, Value b)
    {
        // Сложение с поддержкой строк и чисел.
        if (a.Kind == ValueKind.ObjectRef && b.Kind == ValueKind.ObjectRef)
        {
            var newPtr = ctx.Runtime.ConcatStrings(a.AsObject(), b.AsObject());
            return Value.FromObject(newPtr);
        }

        if (a.Kind == ValueKind.ObjectRef && IsScalarForStringConcat(b))
        {
            var rightPtr = ctx.Runtime.AllocateString(FormatScalar(b));
            var newPtr = ctx.Runtime.ConcatStrings(a.AsObject(), rightPtr);
            return Value.FromObject(newPtr);
        }

        if (IsScalarForStringConcat(a) && b.Kind == ValueKind.ObjectRef)
        {
            var leftPtr = ctx.Runtime.AllocateString(FormatScalar(a));
            var newPtr = ctx.Runtime.ConcatStrings(leftPtr, b.AsObject());
            return Value.FromObject(newPtr);
        }

        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
        {
            var d1 = ToDouble(a);
            var d2 = ToDouble(b);
            return Value.FromDouble(d1 + d2);
        }

        if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
        {
            return Value.FromLong(unchecked(ToLong(a) + ToLong(b)));
        }

        return Value.FromInt(unchecked(a.AsInt() + b.AsInt()));
    }

    internal static Value Sub(Value a, Value b)
    {
        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
        {
            return Value.FromDouble(ToDouble(a) - ToDouble(b));
        }

        if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
        {
            return Value.FromLong(unchecked(ToLong(a) - ToLong(b)));
        }

        return Value.FromInt(unchecked(a.AsInt() - b.AsInt()));
    }

    internal static Value Mul(Value a, Value b)
    {
        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
        {
            return Value.FromDouble(ToDouble(a) * ToDouble(b));
        }

        if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
        {
            return Value.FromLong(unchecked(ToLong(a) * ToLong(b)));
        }

        return Value.FromInt(unchecked(a.AsInt() * b.AsInt()));
    }

    internal static Value Div(Value a, Value b)
    {
        if (b.Kind is ValueKind.Int or ValueKind.Long && ToLong(b) == 0)
        {
            throw new DivideByZeroException();
        }

        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
        {
            return Value.FromDouble(ToDouble(a) / ToDouble(b));
        }

        if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
        {
            return Value.FromLong(ToLong(a) / ToLong(b));
        }

        return Value.FromInt(a.AsInt() / b.AsInt());
    }

    internal static Value Mod(Value a, Value b)
    {
        if (b.Kind is ValueKind.Int or ValueKind.Long && ToLong(b) == 0)
        {
            throw new DivideByZeroException();
        }

        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
        {
            return Value.FromDouble(ToDouble(a) % ToDouble(b));
        }

        if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
        {
            return Value.FromLong(ToLong(a) % ToLong(b));
        }

        return Value.FromInt(a.AsInt() % b.AsInt());
    }

    internal static Value Neg(Value v)
    {
        if (v.Kind == ValueKind.Double)
        {
            return Value.FromDouble(-v.AsDouble());
        }

        if (v.Kind == ValueKind.Long)
        {
            return Value.FromLong(unchecked(-v.AsLong()));
        }

        return Value.FromInt(unchecked(-v.AsInt()));
    }

    internal static Value CmpEq(Value a, Value b)
    {
        if (IsNumeric(a) && IsNumeric(b))
        {
            return Value.FromBool(CompareNumeric(a, b) == 0);
        }

        return Value.FromBool(a.Raw == b.Raw);
    }

    internal static Value CmpNe(Value a, Value b)
    {
        if (IsNumeric(a) && IsNumeric(b))
        {
            return Value.FromBool(CompareNumeric(a, b) != 0);
        }

        return Value.FromBool(a.Raw != b.Raw);
    }

    internal static Value CmpLt(Value a, Value b)
    {
        return Value.FromBool(CompareNumeric(a, b) < 0);
    }

    internal static Value CmpGt(Value a, Value b)
    {
        return Value.FromBool(CompareNumeric(a, b) > 0);
    }

    internal static Value CmpLe(Value a, Value b)
    {
        return Value.FromBool(CompareNumeric(a, b) <= 0);
    }

    internal static Value CmpGe(Value a, Value b)
    {
        return Value.FromBool(CompareNumeric(a, b) >= 0);
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

    private static bool IsNumeric(Value value)
    {
        return value.Kind == ValueKind.Int ||
               value.Kind == ValueKind.Long ||
               value.Kind == ValueKind.Double;
    }

    private static long ToLong(Value value)
    {
        return value.Kind == ValueKind.Long ? value.AsLong() : value.AsInt();
    }

    private static double ToDouble(Value value)
    {
        return value.Kind == ValueKind.Double
            ? value.AsDouble()
            : value.Kind == ValueKind.Long
                ? value.AsLong()
                : value.AsInt();
    }

    private static int CompareNumeric(Value left, Value right)
    {
        if (left.Kind == ValueKind.Double || right.Kind == ValueKind.Double)
        {
            return ToDouble(left).CompareTo(ToDouble(right));
        }

        if (left.Kind == ValueKind.Long || right.Kind == ValueKind.Long)
        {
            return ToLong(left).CompareTo(ToLong(right));
        }

        return left.AsInt().CompareTo(right.AsInt());
    }

    private static bool IsScalarForStringConcat(Value value)
    {
        return value.Kind is ValueKind.Int or ValueKind.Long or ValueKind.Double or ValueKind.Bool or ValueKind.Char;
    }

    private static string FormatScalar(Value value)
    {
        return value.Kind switch
        {
            ValueKind.Int => value.AsInt().ToString(),
            ValueKind.Long => value.AsLong().ToString(),
            ValueKind.Double => value.AsDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ValueKind.Bool => value.AsBool() ? "true" : "false",
            ValueKind.Char => value.AsChar().ToString(),
            _ => value.ToString()
        };
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
    
    internal static Value GetField(JitExecutionContext ctx, Value objRef, int classId, int fieldId)
    {
        VmChecks.CheckNull(objRef);

        var raw = ctx.Runtime.ReadFieldRaw(objRef.AsObject(), fieldId);

        var cls = ctx.GetClassById(classId);
        var field = cls.Fields.Values.FirstOrDefault(f => f.FieldId == fieldId);

        if (field != null && field.Type is PrimitiveType prim)
        {
            return prim.Name switch
            {
                "double" => new Value(ValueKind.Double, raw),
                "long" => Value.FromLong(raw),
                "int" => Value.FromInt((int)raw),
                "bool" => Value.FromBool(raw != 0),
                "char" => Value.FromChar((char)raw),
                _ => new Value(raw)
            };
        }

        return new Value(raw);
    }

    internal static void SetField(JitExecutionContext ctx, Value objRef, int classId, int fieldId, Value value)
    {
        VmChecks.CheckNull(objRef);
        ctx.Runtime.WriteField(objRef.AsObject(), fieldId, value);
    }
}
