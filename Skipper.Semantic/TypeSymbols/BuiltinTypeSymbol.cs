namespace Skipper.Semantic.TypeSymbols;

public sealed class BuiltinTypeSymbol : TypeSymbol
{
    private BuiltinTypeSymbol(string name) : base(name) { }

    public static readonly BuiltinTypeSymbol Int = new("int");
    public static readonly BuiltinTypeSymbol Float = new("float");
    public static readonly BuiltinTypeSymbol Bool = new("bool");
    public static readonly BuiltinTypeSymbol Char = new("char");
    public static readonly BuiltinTypeSymbol String = new("string");
    public static readonly BuiltinTypeSymbol Void = new("void");
}