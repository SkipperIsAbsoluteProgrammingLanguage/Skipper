using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class LongTests
{
    [Fact]
    public void VM_LongLiteral_ReturnsValue()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 9223372036854775807;
                                return a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(ValueKind.Long, result.Kind);
        Assert.Equal(9223372036854775807L, result.AsLong());
    }

    [Fact]
    public void VM_Long_Arithmetic_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 10;
                                long b = 3;
                                return a * b + 2 - b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(29L, result.AsLong());
    }

    [Fact]
    public void VM_Long_MixedWithInt_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 2;
                                int b = 3;
                                return a + b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(ValueKind.Long, result.Kind);
        Assert.Equal(5L, result.AsLong());
    }

    [Fact]
    public void VM_Long_FunctionParameters_ConvertIntToLong()
    {
        // Arrange
        const string code = """
                            fn add(long a, long b) -> long {
                                return a + b;
                            }
                            fn main() -> long {
                                return add(1, 2);
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(ValueKind.Long, result.Kind);
        Assert.Equal(3L, result.AsLong());
    }

    [Fact]
    public void VM_Long_MixedWithDouble_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> double {
                                long a = 2;
                                double b = 1.5;
                                return a + b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(ValueKind.Double, result.Kind);
        Assert.Equal(3.5, result.AsDouble(), 10);
    }

    [Fact]
    public void VM_Long_Negative_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = -5;
                                return a * 2;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(-10L, result.AsLong());
    }

    [Fact]
    public void VM_Long_CompoundAssignments_AllOperators()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 100;
                                a += 20;
                                a -= 5;
                                a *= 2;
                                a /= 3;
                                a %= 7;
                                return a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(6L, result.AsLong());
    }

    [Fact]
    public void VM_Long_Comparisons_Work()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                long a = 5;
                                long b = 7;
                                int r = 0;
                                if (a < b) { r = r + 1; }
                                if (a <= b) { r = r + 2; }
                                if (b > a) { r = r + 4; }
                                if (b >= a) { r = r + 8; }
                                if (a == a) { r = r + 16; }
                                if (a != b) { r = r + 32; }
                                return r;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(63, result.AsInt());
    }

    [Fact]
    public void VM_Long_Comparison_WithDouble_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> bool {
                                long a = 5;
                                double b = 5.0;
                                return a == b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.True(result.AsBool());
    }

    [Fact]
    public void VM_Long_Int_Comparison_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> bool {
                                long a = 5;
                                int b = 6;
                                return a < b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.True(result.AsBool());
    }

    [Fact]
    public void VM_Long_Equality_WithInt_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> bool {
                                long a = 5;
                                int b = 5;
                                return a == b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.True(result.AsBool());
    }

    [Fact]
    public void VM_Long_CompoundAssignment_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 1;
                                a += 2;
                                a *= 3;
                                return a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(9L, result.AsLong());
    }

    [Fact]
    public void VM_Long_StringConcat_Works()
    {
        // Arrange
        const string code = """
                            fn main() {
                                long a = 1234567890123;
                                print("value=" + a);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("value=1234567890123", output);
    }

    [Fact]
    public void VM_Long_StringConcat_LeftSide_Works()
    {
        // Arrange
        const string code = """
                            fn main() {
                                long a = 42;
                                print(a + "ms");
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("42ms", output);
    }

    [Fact]
    public void VM_Long_Overflow_Wraps()
    {
        // Arrange
        const string code = """
                            fn main() {
                                long a = 9223372036854775807;
                                a = a + 1;
                                print(a);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("-9223372036854775808", output);
    }

    [Fact]
    public void VM_Long_DivideByZero_Throws()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 10;
                                long b = 0;
                                return a / b;
                            }
                            """;

        // Act & Assert
        Assert.Throws<DivideByZeroException>(() => TestsHelpers.Run(code));
    }

    [Fact]
    public void VM_Long_Modulo_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 10;
                                long b = 6;
                                return a % b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(4L, result.AsLong());
    }

    [Fact]
    public void VM_Long_Division_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 20;
                                long b = 3;
                                return a / b;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(6L, result.AsLong());
    }

    [Fact]
    public void VM_Long_PrefixPostfix_Operators_Work()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 5;
                                long b = ++a;
                                long c = a--;
                                return b * 100 + c * 10 + a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(665L, result.AsLong());
    }

    [Fact]
    public void VM_Long_Increment_Decrement_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 1;
                                long b = a++;
                                --a;
                                return b * 10 + a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(11L, result.AsLong());
    }

    [Fact]
    public void VM_Long_Array_ReadWrite_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long[] arr = new long[2];
                                arr[0] = 5;
                                arr[1] = 7;
                                return arr[0] + arr[1];
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(12L, result.AsLong());
    }

    [Fact]
    public void VM_Long_Field_ReadWrite_Works()
    {
        // Arrange
        const string code = """
                            class Box { long value; }
                            fn main() -> long {
                                Box b = new Box();
                                b.value = 42;
                                return b.value;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(42L, result.AsLong());
    }

    [Fact]
    public void VM_Long_Ternary_ReturnsLong()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = 5;
                                long b = 7;
                                long c = a < b ? a : b;
                                return c;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(5L, result.AsLong());
    }

    [Fact]
    public void VM_Long_MinValue_FromExpression_Works()
    {
        // Arrange
        const string code = """
                            fn main() -> long {
                                long a = -9223372036854775807;
                                a = a - 1;
                                return a;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(-9223372036854775808L, result.AsLong());
    }
}
