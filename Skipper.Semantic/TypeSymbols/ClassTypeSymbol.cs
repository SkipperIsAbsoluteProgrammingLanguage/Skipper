using Skipper.Semantic.Symbols;

namespace Skipper.Semantic.TypeSymbols;

public sealed class ClassTypeSymbol : TypeSymbol
{
    public ClassSymbol Class { get; }

    internal ClassTypeSymbol(ClassSymbol cls)
        : base(cls.Name)
    {
        Class = cls;
    }
}