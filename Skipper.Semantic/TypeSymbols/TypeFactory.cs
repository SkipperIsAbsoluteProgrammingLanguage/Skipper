using System.Collections.Concurrent;

namespace Skipper.Semantic.TypeSymbols;

public static class TypeFactory
{
    private static readonly ConcurrentDictionary<TypeSymbol, ArrayTypeSymbol> Arrays = new();

    public static ArrayTypeSymbol Array(TypeSymbol element)
    {
        return Arrays.GetOrAdd(element, static elem => new ArrayTypeSymbol(elem));
    }
}
