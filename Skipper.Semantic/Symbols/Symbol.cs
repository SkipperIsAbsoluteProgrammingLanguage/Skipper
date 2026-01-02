using Skipper.Semantic.TypeSymbols;

namespace Skipper.Semantic.Symbols;

public abstract class Symbol
{
    public string Name { get; }
    public TypeSymbol Type { get; protected init; }

    protected Symbol(string name, TypeSymbol type)
    {
        Name = name;
        Type = type;
    }
}