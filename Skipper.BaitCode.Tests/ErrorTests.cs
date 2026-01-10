using Skipper.BaitCode.Objects.Instructions;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class ErrorTests
{
    [Fact]
    public void Error_UnknownIdentifier_Throws_WithMessage()
    {
        const string code = "fn main() { x = 5; }";

        var ex = Assert.Throws<Exception>(() => TestHelpers.Generate(code));

        Assert.Contains("Local 'x' not found", ex.Message);
    }

    [Fact]
    public void Error_MemberAccessOnNonClass_Throws_WithMessage()
    {
        const string code = """
            fn main() {
                int x = 5;
                x.val = 10;
            }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => TestHelpers.Generate(code));

        Assert.Contains("Member access on non-class variable 'x'", ex.Message);
    }

    [Fact]
    public void Error_UnknownField_Throws_WithMessage()
    {
        const string code = """
            class A { int x; }
            fn main() {
                A a = new A();
                a.y = 5;
            }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => TestHelpers.Generate(code));

        Assert.Contains("Field 'y' not found in class 'A'", ex.Message);
    }

    [Fact]
    public void Error_UnknownMethod_Throws_WithMessage()
    {
        const string code = """
            class A {}
            fn main() {
                A a = new A();
                a.foo();
            }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => TestHelpers.Generate(code));

        Assert.Contains("Method 'foo' not found in class 'A'", ex.Message);
    }

    [Fact]
    public void Return_VoidFunction_NoValue_GeneratesReturnOnly()
    {
        const string code = "fn main() { return; }";

        var inst = TestHelpers.GetInstructions(
            TestHelpers.Generate(code),
            "main"
        );

        Assert.Equal(OpCode.RETURN, inst.Last().OpCode);
        Assert.Empty(inst.Last().Operands);
    }
    
    [Fact]
    public void Error_UnknownFunction_Throws()
    {
        const string code = "fn main() { foo(); }";

        var ex = Assert.Throws<InvalidOperationException>(() => TestHelpers.Generate(code));

        Assert.Contains("Function 'foo' not found", ex.Message);
    }

    [Fact]
    public void Error_UnknownType_Throws()
    {
        const string code = "fn main() { Foo x; }";

        var ex = Assert.Throws<InvalidOperationException>(() => TestHelpers.Generate(code));

        Assert.Contains("Unknown class type 'Foo'", ex.Message);
    }
}
