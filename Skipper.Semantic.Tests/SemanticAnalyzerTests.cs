using System.Linq;
using Xunit;

namespace Skipper.Semantic.Tests;

public class SemanticTests
{
    // -------------------------
    // Variables & Scope
    // -------------------------

    [Fact]
    public void Variable_Declaration_OK()
    {
        var code = """
                   fn main() {
                    int x = 5;
                   }
                   """;

        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Variable_Redeclaration_Error()
    {
        var code = """
                   fn main() {
                    int x;
                    int x;
                   }
                   """;

        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("already declared"));
    }

    [Fact]
    public void Variable_Outside_Scope_Error()
    {
        var code = """
                   fn main() {
                    {
                     int x;
                    }
                    x = 1;
                   }
                   """;

        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("Unknown identifier") || d.Message.Contains("Undefined identifier"));
    }

    // -------------------------
    // Type checking
    // -------------------------

    [Fact]
    public void Invalid_Assignment_Type_Error()
    {
        var code = """
                   fn main() {
                    int x = true;
                   }
                   """;

        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void Numeric_Binary_Operator_Type_Error()
    {
        var code = """
                   fn main() {
                    int x = 1 + true;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("requires numeric operands") || d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void Comparison_Type_Mismatch_Error()
    {
        var code = """
                   fn main() {
                    bool b = 1 == "s";
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("Cannot compare") || d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void Logical_Operator_Type_Error()
    {
        var code = """
                   fn main() {
                    bool b = 1 && 0;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("Logical operators require boolean operands") ||
                 d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void Unary_Operator_Type_Error()
    {
        var code = """
                   fn main() {
                    int x = -true;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("Unary '-' requires numeric operand") || d.Message.Contains("Cannot assign"));
    }

    // -------------------------
    // If / While / For / Return
    // -------------------------

    [Fact]
    public void If_While_For_Condition_Type_Errors()
    {
        var code = """
                   fn main() {
                    if (1) {}
                    while ("s") {}
                    for (; 5; ) {}
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Condition expression must be 'bool'"));
    }

    [Fact]
    public void Return_NoValue_From_NonVoid_Error()
    {
        var code = """
                   fn foo() -> int {
                    return;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Return statement missing a value"));
    }

    [Fact]
    public void Return_Wrong_Type_Error()
    {
        var code = """
                   fn foo() -> int {
                    return true;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot return value of type"));
    }

    // -------------------------
    // Functions / Calls
    // -------------------------

    [Fact]
    public void Call_Undefined_Function_Error()
    {
        var code = """
                   fn main() { foo(); }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("is not a function") || d.Message.Contains("Undefined"));
    }

    [Fact]
    public void Call_Wrong_Arg_Count_And_Type_Error()
    {
        var code = """
                   fn sum(int a, int b) -> int { return a + b; }
                   fn main() { sum(1); sum(1, true); }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("expects") || d.Message.Contains("Cannot convert argument") ||
                 d.Message.Contains("is not a function"));
    }

    [Fact]
    public void Calling_NonFunction_Error()
    {
        var code = """
                   fn main() { int x; x(); }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("is not a function") || d.Message.Contains("Unsupported call"));
    }

    // -------------------------
    // Arrays
    // -------------------------

    [Fact]
    public void Array_Index_And_NonArray_Errors()
    {
        var code = """
                   fn main() {
                    int[] a;
                    a[true] = 1;
                    int x;
                    x[0] = 1;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Array index must be 'int'"));
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("is not an array") || d.Message.Contains("non-array"));
    }

    [Fact]
    public void NewArray_Size_NotInt_Error()
    {
        var code = """
                   fn main() { new int[true]; }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Array size must be 'int'"));
    }

    // -------------------------
    // Ternary
    // -------------------------

    [Fact]
    public void Ternary_Condition_And_Branch_Type_Errors()
    {
        var code = """
                   fn main() {
                    int x = 1 ? 2 : 3;
                    int y = true ? 1 : false;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("Condition expression must be 'bool'") ||
                 d.Message.Contains("Incompatible types in ternary"));
    }

    // -------------------------
    // Classes & Members
    // -------------------------

    [Fact]
    public void Class_Member_Access_Errors_And_OK()
    {
        var code = """
                   class A { int x; }
                   fn main() {
                    A a;
                    int y = a.y;
                    int z = a.x;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("not found") || d.Message.Contains("has no field") || d.Message.Contains("Member"));
    }

    [Fact]
    public void Method_Call_On_NonClass_Error()
    {
        var code = """
                   fn main() {
                    int x;
                    x.foo();
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("Cannot call member") || d.Message.Contains("not found"));
    }

    [Fact]
    public void Method_Call_Wrong_Arg_Error()
    {
        var code = """
                   class A { fn m(int a) -> int { return a; } }
                   fn main() { A a; a.m(true); }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("Cannot convert argument") || d.Message.Contains("expects"));
    }

    [Fact]
    public void Class_Member_Duplicate_Errors()
    {
        var code = """
                   class A { int x; int x; fn f() {} fn f() {} }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("already declared in class") || d.Message.Contains("Method"));
    }

    [Fact]
    public void Assign_To_Member_And_Array_Element_Type_Checks()
    {
        var code = """
                   class A { int f; }
                   fn main() {
                    A a; a.f = true;
                    int[] arr; arr[0] = true;
                   }
                   """;
        var semantic = SemanticTestHelper.Analyze(code);
        Assert.Contains(semantic.Diagnostics,
            d => d.Message.Contains("Cannot assign value of type") || d.Message.Contains("Array index must be 'int'"));
    }
}