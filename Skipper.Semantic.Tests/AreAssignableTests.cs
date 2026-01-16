using Skipper.Semantic.TypeSymbols;
using Xunit;

namespace Skipper.Semantic.Tests;

public class AreAssignableTests
{
    [Fact]
    public void SameBuiltin_IsAssignable()
    {
        Assert.True(TypeSystem.AreAssignable(BuiltinTypeSymbol.Int, BuiltinTypeSymbol.Int));
    }

    [Fact]
    public void IntToDouble_IsAssignable()
    {
        Assert.True(TypeSystem.AreAssignable(BuiltinTypeSymbol.Int, BuiltinTypeSymbol.Double));
    }

    [Fact]
    public void DoubleToInt_NotAssignable()
    {
        Assert.False(TypeSystem.AreAssignable(BuiltinTypeSymbol.Double, BuiltinTypeSymbol.Int));
    }

    [Fact]
    public void Array_SameInstance_IsAssignable()
    {
        var ai = TypeFactory.Array(BuiltinTypeSymbol.Int);
        var aii = TypeFactory.Array(BuiltinTypeSymbol.Int);
        Assert.True(ReferenceEquals(ai, aii));
        Assert.True(TypeSystem.AreAssignable(ai, aii));
    }

    [Fact]
    public void ArrayIntToArrayDouble_IsAssignable()
    {
        var ai = TypeFactory.Array(BuiltinTypeSymbol.Int);
        var af = TypeFactory.Array(BuiltinTypeSymbol.Double);
        Assert.True(TypeSystem.AreAssignable(ai, af));
    }

    [Fact]
    public void NestedArrayDimensionMismatch_NotAssignable()
    {
        var ai2 = TypeFactory.Array(TypeFactory.Array(BuiltinTypeSymbol.Int)); // int[][]
        var af = TypeFactory.Array(BuiltinTypeSymbol.Double); // double[]
        Assert.False(TypeSystem.AreAssignable(ai2, af));
    }

    [Fact]
    public void NestedArrayPromotion_IsAssignable()
    {
        var ai2 = TypeFactory.Array(TypeFactory.Array(BuiltinTypeSymbol.Int)); // int[][]
        var af2 = TypeFactory.Array(TypeFactory.Array(BuiltinTypeSymbol.Double)); // double[][]
        Assert.True(TypeSystem.AreAssignable(ai2, af2));
    }

    [Fact]
    public void ArrayToElement_NotAssignable()
    {
        var ai = TypeFactory.Array(BuiltinTypeSymbol.Int);
        Assert.False(TypeSystem.AreAssignable(ai, BuiltinTypeSymbol.Int));
        Assert.False(TypeSystem.AreAssignable(BuiltinTypeSymbol.Int, ai));
    }
}