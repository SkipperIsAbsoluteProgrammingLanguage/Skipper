using System.Globalization;

namespace Skipper.Runtime.Values;

public struct Value
{
    public ValueKind Kind;
    public long Raw;

    public Value(long raw)
    {
        Raw = raw;
        Kind = ValueKind.Int;
    }

    public Value(ValueKind kind, long raw)
    {
        Kind = kind;
        Raw = raw;
    }

    public static Value FromInt(int v)
    {
        return new()
        {
            Kind = ValueKind.Int,
            Raw = v
        };
    }

    public static Value FromLong(long v)
    {
        return new()
        {
            Kind = ValueKind.Long,
            Raw = v
        };
    }

    public static Value FromDouble(double v)
    {
        return new()
        {
            Kind = ValueKind.Double,
            Raw = BitConverter.DoubleToInt64Bits(v)
        };
    }

    public static Value FromBool(bool v)
    {
        return new()
        {
            Kind = ValueKind.Bool,
            Raw = v ? 1 : 0
        };
    }

    public static Value FromChar(char v)
    {
        return new()
        {
            Kind = ValueKind.Char,
            Raw = v
        };
    }

    public static Value FromObject(nint ptr)
    {
        return new()
        {
            Kind = ValueKind.ObjectRef,
            Raw = ptr
        };
    }

    public static Value Null()
    {
        return new()
        {
            Kind = ValueKind.Null,
            Raw = 0
        };
    }

    public int AsInt()
    {
        return (int)Raw;
    }

    public long AsLong()
    {
        return Raw;
    }

    public bool AsBool()
    {
        return Raw != 0;
    }

    public double AsDouble()
    {
        return BitConverter.Int64BitsToDouble(Raw);
    }

    public char AsChar()
    {
        return (char)Raw;
    }

    public nint AsObject()
    {
        return (nint)Raw;
    }

    public override string ToString()
    {
        return Kind switch
        {
            ValueKind.Int => AsInt().ToString(),
            ValueKind.Long => AsLong().ToString(),
            ValueKind.Bool => AsBool().ToString(),
            ValueKind.Double => AsDouble().ToString(CultureInfo.InvariantCulture),
            ValueKind.Null => "null",
            _ => $"{Kind}:{Raw}"
        };
    }
}
