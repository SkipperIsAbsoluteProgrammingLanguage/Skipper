namespace Skipper.Semantic.TypeSymbols;

public static class TypeSystem
{
    public static bool AreAssignable(TypeSymbol from, TypeSymbol to)
    {
        while (true)
        {
            if (ReferenceEquals(from, to))
            {
                return true;
            }

            if (from == BuiltinTypeSymbol.Int && to == BuiltinTypeSymbol.Double)
            {
                return true;
            }

            if (from is not ArrayTypeSymbol fa || to is not ArrayTypeSymbol ta)
            {
                return false;
            }

            from = fa.ElementType;
            to = ta.ElementType;
        }
    }
}