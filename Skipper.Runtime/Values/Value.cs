namespace Skipper.Runtime.Values;

public struct Value
{
    public ValueKind Kind;
    public long Raw;

    public static Value FromInt(int v) => new()
    {
        Kind = ValueKind.Int, Raw = v
    };

    public static Value FromDouble(double v) => new()
    {
        Kind = ValueKind.Double, Raw = BitConverter.DoubleToInt64Bits(v)
    };

    public static Value FromBool(bool v) => new()
    {
        Kind = ValueKind.Bool, Raw = v ? 1 : 0
    };

    public static Value FromChar(char v) => new()
    {
        Kind = ValueKind.Char, Raw = v
    };

    public static Value FromObject(nint ptr) => new()
    {
        Kind = ValueKind.ObjectRef, Raw = ptr
    };

    public static Value Null() => new()
    {
        Kind = ValueKind.Null, Raw = 0
    };

    public nint AsObject() => (nint)Raw;
}