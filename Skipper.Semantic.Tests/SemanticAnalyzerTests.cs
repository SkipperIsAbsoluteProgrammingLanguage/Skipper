using Xunit;

namespace Skipper.Semantic.Tests;

public class SemanticTests
{
    [Fact]
    public void Variable_Declaration_OK()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = 5;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Variable_Redeclaration_Error()
    {
        // Assert
        const string code = """
                            fn main() {
                             int x;
                             int x;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("already declared"));
    }

    [Fact]
    public void Variable_Outside_Scope_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             {
                              int x;
                             }
                             x = 1;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Unknown identifier"));
    }

    [Fact]
    public void Invalid_Assignment_Type_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = true;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void Numeric_Binary_Operator_Type_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = 1 + true;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d
            => d.Message.Contains("requires numeric operands")
               || d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void Comparison_Type_Mismatch_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             bool b = 1 == "s";
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot compare"));
    }

    [Fact]
    public void Logical_Operator_Type_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             bool b = 1 && 0;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Logical operators require boolean operands"));
    }

    [Fact]
    public void Unary_Operator_Type_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = -true;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Unary '-' requires numeric operand"));
    }

    [Fact]
    public void If_While_For_Condition_Type_Errors()
    {
        // Arrange
        const string code = """
                            fn main() {
                             if (1) {}
                             while ("s") {}
                             for (; 5; ) {}
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        var conditionErrorsCount = semantic.Diagnostics.Count(d
            => d.Message.Contains("Condition expression must be 'bool'"));

        Assert.Equal(3, conditionErrorsCount);
    }

    [Fact]
    public void Return_NoValue_From_NonVoid_Error()
    {
        // Arrange
        const string code = """
                            fn foo() -> int {
                             return;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Return statement missing a value"));
    }

    [Fact]
    public void Return_Wrong_Type_Error()
    {
        // Arrange
        const string code = """
                            fn foo() -> int {
                             return true;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot return value of type"));
    }

    [Fact]
    public void Call_Undefined_Function_Error()
    {
        // Arrange
        const string code = "fn main() { foo(); }";

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("is not a function"));
    }

    [Fact]
    public void Call_Wrong_Arg_Count_And_Type_Error()
    {
        // Arrange
        const string code = """
                            fn sum(int a, int b) -> int { return a + b; }
                            fn main() { sum(1); sum(1, true); }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Expected"));
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot convert argument"));
    }

    [Fact]
    public void Calling_NonFunction_Error()
    {
        // Arrange
        const string code = "fn main() { int x; x(); }";

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("is not a function"));
    }

    [Fact]
    public void Array_Index_And_NonArray_Errors()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int[] a;
                             a[true] = 1;
                             int x;
                             x[0] = 1;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Array index must be 'int'"));
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("is not an array"));
    }

    [Fact]
    public void NewArray_Size_NotInt_Error()
    {
        // Arrange
        const string code = "fn main() { new int[true]; }";

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Array size must be 'int'"));
    }

    [Fact]
    public void Ternary_Condition_And_Branch_Type_Errors()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = 1 ? 2 : 3;
                             int y = true ? 1 : false;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Condition expression must be 'bool'"));
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Incompatible types in ternary"));
    }

    [Fact]
    public void Class_Member_Access_Errors_And_OK()
    {
        // Arrange
        const string code = """
                            class A { int x; }
                            fn main() {
                             A a;
                             int y = a.y;
                             int z = a.x;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("not found"));
    }

    [Fact]
    public void Method_Call_On_NonClass_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x;
                             x.foo();
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot call member"));
    }

    [Fact]
    public void Method_Call_Wrong_Arg_Error()
    {
        // Arrange
        const string code = """
                            class A { fn m(int a) -> int { return a; } }
                            fn main() { A a; a.m(true); }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot convert argument"));
    }

    [Fact]
    public void Class_Member_Duplicate_Errors()
    {
        // Arrange
        const string code = "class A { int x; int x; fn f() {} fn f() {} }";

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Member 'x' already declared in class"));
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Method 'f' already declared in class"));
    }

    [Fact]
    public void Function_Missing_Return_Error()
    {
        // Arrange
        const string code = """
                            class A { int x; }
                            fn func() -> int {
                             A a;
                             int y = a.x;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Not all code paths return"));
    }

    [Fact]
    public void Method_Missing_Return_Error()
    {
        // Arrange
        const string code = "class A { int x; fn public m() -> int { int y = x; } }";

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Not all code paths return"));
    }

    [Fact]
    public void Assign_To_Member_And_Array_Element_Type_Checks()
    {
        // Arrange
        const string code = """
                            class A { int f; }
                            fn main() {
                             A a; a.f = true;
                             int[] arr; arr[0] = true;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot assign value of type"));
        Assert.Equal(2, semantic.Diagnostics.Count);
    }

    [Fact]
    public void BitwiseAnd_IntOperands_OK()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = 5 & 3;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void BitwiseOr_BoolOperands_OK()
    {
        // Arrange
        const string code = """
                            fn main() {
                             bool b = true | false;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void BitwiseAnd_CharOperands_OK()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = 'a' & 'b';
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void BitwiseAnd_MismatchedTypes_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = 1 & true;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Bitwise operator"));
    }

    [Fact]
    public void BitwiseBoolResult_AssignedToInt_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = true & false;
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void BitwiseAnd_DoesNotRequireBooleanContext()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = 5;
                             if (x & 1) {}
                            }
                            """;

        // Act
        var semantic = SemanticTestHelper.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Condition expression must be 'bool'"));
    }
}