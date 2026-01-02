namespace Skipper.Semantic.TypeSymbols;

public static class TypeFactory
{
    private static readonly Dictionary<TypeSymbol, ArrayTypeSymbol> Arrays = new();

    public static ArrayTypeSymbol Array(TypeSymbol element)
    {
        if (Arrays.TryGetValue(element, out var type))
        {
            return type;
        }

        type = new ArrayTypeSymbol(element);
        Arrays[element] = type;

        return type;
    }
}