using Skipper.Semantic.TypeSymbols;

namespace Skipper.Semantic.Symbols;

public sealed class FunctionSymbol : Symbol
{
    public IReadOnlyList<ParameterSymbol> Parameters { get; }

    public FunctionSymbol(
        string name,
        TypeSymbol type,
        IReadOnlyList<ParameterSymbol> parameters)
        : base(name, type)
    {
        Parameters = parameters;
    }
}