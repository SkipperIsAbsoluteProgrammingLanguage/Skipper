namespace Skipper.Semantic.TypeSymbols;

public sealed class BuiltinTypeSymbol : TypeSymbol
{
    private BuiltinTypeSymbol(string name) : base(name) { }

    public static readonly BuiltinTypeSymbol Int = new("int");
    public static readonly BuiltinTypeSymbol Long = new("long");
    public static readonly BuiltinTypeSymbol Double = new("double");
    public static readonly BuiltinTypeSymbol Bool = new("bool");
    public static readonly BuiltinTypeSymbol Char = new("char");
    public static readonly BuiltinTypeSymbol String = new("string");
    public static readonly BuiltinTypeSymbol Void = new("void");
}
