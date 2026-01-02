using Skipper.Semantic.TypeSymbols;

namespace Skipper.Semantic.Symbols;

public sealed class ClassSymbol : Symbol
{
    public Dictionary<string, FieldSymbol> Fields { get; } = new();
    public Dictionary<string, MethodSymbol> Methods { get; } = new();

    public ClassTypeSymbol ClassType => (ClassTypeSymbol)Type;

    public ClassSymbol(string name)
        : base(name, null!)
    {
        Type = new ClassTypeSymbol(this);
    }
}