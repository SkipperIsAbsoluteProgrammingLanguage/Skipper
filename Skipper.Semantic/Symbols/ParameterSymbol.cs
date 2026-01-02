using Skipper.Semantic.TypeSymbols;

namespace Skipper.Semantic.Symbols;

public sealed class ParameterSymbol : Symbol
{
    public ParameterSymbol(string name, TypeSymbol type)
        : base(name, type) { }
}