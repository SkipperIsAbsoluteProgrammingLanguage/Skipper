using Skipper.Semantic.Symbols;

namespace Skipper.Semantic;

public sealed class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new();
    public Scope? Parent { get; }

    public Scope(Scope? parent)
    {
        Parent = parent;
    }

    public bool Declare(Symbol symbol)
    {
        return _symbols.TryAdd(symbol.Name, symbol);
    }

    public Symbol? Resolve(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        return Parent?.Resolve(name);
    }
}
