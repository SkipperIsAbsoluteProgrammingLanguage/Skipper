using Skipper.Semantic.TypeSymbols;

namespace Skipper.Semantic.Symbols;

public sealed class VariableSymbol : Symbol
{
    public VariableSymbol(string name, TypeSymbol type)
        : base(name, type) { }
}