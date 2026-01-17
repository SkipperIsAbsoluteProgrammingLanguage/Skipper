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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot return value of type"));
    }

    [Fact]
    public void Call_Undefined_Function_Error()
    {
        // Arrange
        const string code = "fn main() { foo(); }";

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Unknown function or method"));
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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot convert argument"));
    }

    [Fact]
    public void Class_Member_Duplicate_Errors()
    {
        // Arrange
        const string code = "class A { int x; int x; fn f() {} fn f() {} }";

        // Act
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Not all code paths return"));
    }

    [Fact]
    public void Method_Missing_Return_Error()
    {
        // Arrange
        const string code = "class A { int x; fn public m() -> int { int y = x; } }";

        // Act
        var semantic = TestHelpers.Analyze(code);

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
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot assign value of type"));
        Assert.Equal(2, semantic.Diagnostics.Count);
    }

    [Fact]
    public void Variable_Shadowing_IsAllowed()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int x = 1;
                                {
                                    bool x = true; // Это законно, новый скоуп
                                    if (x) { }
                                }
                                int y = x + 1; // Здесь x снова int (1)
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Functions_CanCall_Forward_And_Recursively()
    {
        // Arrange
        const string code = """
                            fn main() {
                                foo();
                            }

                            fn foo() {
                                bar(); // Forward reference
                                foo(); // Recursion
                            }

                            fn bar() { }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Class_Implicit_Field_Access_Works()
    {
        // Arrange
        const string code = """
                            class Box {
                                int width;
                                
                                fn setWidth(int w) {
                                    width = w; // Должно найти поле width
                                }
                                
                                fn getWidth() -> int {
                                    return width;
                                }
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Return_Analysis_Works_Nested()
    {
        // Arrange
        const string code = """
                            fn test(int x) -> int {
                                if (x > 0) {
                                    return 1;
                                } else {
                                    return 0;
                                }
                                // Здесь return не нужен, так как if/else покрывают все ветки
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Scope_Hierarchy_Parameter_Hides_Global_And_Field()
    {
        // Arrange
        // Логика MVP: Scope.Resolve (Локальные + Глобальные) имеет приоритет над полями класса.
        // 1. В method1: аргумент 'val' перекрывает и глобальную 'val', и поле 'val'.
        // 2. В method2: аргумента нет. Resolve находит глобальную 'val'. Поле класса игнорируется.
        const string code = """
                            int val = 100; // Глобальная

                            class Test {
                                int val; // Поле (будет скрыто глобальной)

                                fn method1(bool val) {
                                    // Здесь val - это bool (аргумент)
                                    if (val) { } 
                                }

                                fn method2() {
                                    // Здесь val - это int (глобальная), а не поле!
                                    // Если бы это было поле, мы бы не смогли присвоить ей результат method1 (void/error)
                                    // Но проверка типов пройдет, так как int (global) = int
                                    int x = val; 
                                }
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Deeply_Nested_Block_Access_Works()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int outer = 1;
                                {
                                    {
                                        {
                                            while(true) {
                                                // Должны видеть outer через 4 уровня скоупов
                                                outer = outer + 1;
                                                if (outer > 10) { return; }
                                            }
                                        }
                                    }
                                }
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void ControlFlow_Return_Analysis_Complex()
    {
        // Arrange
        const string code = """
                            fn check(int x) -> int {
                                if (x > 0) {
                                    return 1;
                                } else {
                                    if (x < 0) {
                                        return -1;
                                    } else {
                                        return 0;
                                    }
                                    // Здесь все ветки else закрыты return-ом
                                }
                                // Сюда управление не дойдет, анализатор должен это понять
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void ControlFlow_Return_Analysis_Missing_In_Loop()
    {
        // Arrange
        // Анализатор консервативен: он не знает, выполнится ли while(true) хотя бы раз,
        // поэтому считает, что выход из функции возможен без return.
        const string code = """
                            fn loop() -> int {
                                while (true) {
                                    return 1;
                                }
                                // Ошибка: а вдруг цикл не выполнится? (для статического анализатора)
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Not all code paths return"));
    }

    [Fact]
    public void Type_Compatibility_Int_To_Double()
    {
        // Arrange
        const string code = """
                            fn main() {
                                double d = 10; // int -> double (OK)
                                d = 5 + 2.5;   // int + double -> double (OK)
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Type_Compatibility_Double_To_Int_Fails()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int i = 5.5; // Error
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void Class_Methods_Can_Call_Each_Other_Recursively()
    {
        // Arrange
        const string code = """
                            class Math {
                                fn isEven(int n) -> bool {
                                    if (n == 0) { return true; }
                                    return isOdd(n - 1); // isOdd объявлен ниже, но должен быть виден
                                }
                                
                                fn isOdd(int n) -> bool {
                                    if (n == 0) { return false; }
                                    return isEven(n - 1); // Рекурсия
                                }
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Chained_Member_Access_Works()
    {
        // Arrange
        const string code = """
                            class Point { int x; int y; }

                            class Shape {
                                fn getCenter() -> Point {
                                    return new Point();
                                }
                            }

                            fn main() {
                                Shape s;
                                // s.getCenter() возвращает Point
                                // .x обращается к полю Point
                                int val = s.getCenter().x; 
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Class_Can_Reference_Itself_Recursive_Type()
    {
        // Arrange
        const string code = """
                            class Node {
                                int value;
                                Node next; // Поле типа самого класса
                            }

                            fn main() {
                                Node n;
                                n.next = new Node();
                                n.next.value = 5;
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Classes_Circular_Dependency_Works()
    {
        // Arrange
        const string code = """
                            class A {
                                B b;
                            }

                            class B {
                                A a;
                            }

                            fn main() {
                                A objA;
                                objA.b = new B();
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Array_Of_Objects_Member_Access()
    {
        // Arrange
        const string code = """
                            class Point { int x; int y; }

                            fn main() {
                                Point[] points;
                                points[0] = new Point();
                                
                                // Доступ к массиву -> Доступ к полю
                                points[0].x = 10;
                                
                                int val = points[0].y;
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void For_Loop_Variable_Shadowing()
    {
        // Arrange
        const string code = """
                            fn main() {
                                for (int i = 0; i < 10; i = i + 1) {
                                    // Это должно быть законно (новый блок)
                                    int i = 5; 
                                }
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Return_Value_From_Void_Function_Error()
    {
        // Arrange
        const string code = """
                            fn proc() { // void по умолчанию
                                return 1;
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot return value"));
    }

    [Fact]
    public void Array_Assignment_Type_Check()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int[] a;
                                int[] b;
                                double[] c;
                                
                                a = b; // OK
                                a = c; // Error: int[] != double[]
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot assign"));
    }

    [Fact]
    public void Builtin_Functions_OK()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int t = time();
                             int r = random(5);
                             print(r);
                             println("done");
                             print();
                             println();
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Fact]
    public void Builtin_Time_Args_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             time(1);
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Expected 0 arguments"));
    }

    [Fact]
    public void Builtin_Random_Type_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             int x = random(true);
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Cannot convert argument 0"));
    }

    [Fact]
    public void Builtin_Print_Args_Error()
    {
        // Arrange
        const string code = """
                            fn main() {
                             print(1, 2);
                            }
                            """;

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Expected 0 or 1 arguments"));
    }
}
