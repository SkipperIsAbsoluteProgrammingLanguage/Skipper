using Skipper.Semantic.TypeSymbols;

namespace Skipper.Semantic.Symbols;

public sealed class MethodSymbol : Symbol
{
    public IReadOnlyList<ParameterSymbol> Parameters { get; }

    public MethodSymbol(
        string name,
        TypeSymbol type,
        IReadOnlyList<ParameterSymbol> parameters)
        : base(name, type)
    {
        Parameters = parameters;
    }
}