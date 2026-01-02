namespace Skipper.Semantic.TypeSymbols;

public sealed class ArrayTypeSymbol : TypeSymbol
{
    public TypeSymbol ElementType { get; }

    internal ArrayTypeSymbol(TypeSymbol elementType)
        : base($"{elementType}[]")
    {
        ElementType = elementType;
    }
}