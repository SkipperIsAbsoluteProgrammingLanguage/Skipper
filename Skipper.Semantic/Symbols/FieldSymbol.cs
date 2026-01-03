using Skipper.Semantic.TypeSymbols;

namespace Skipper.Semantic.Symbols;

public sealed class FieldSymbol : Symbol
{
    public FieldSymbol(string name, TypeSymbol type)
        : base(name, type) { }
}