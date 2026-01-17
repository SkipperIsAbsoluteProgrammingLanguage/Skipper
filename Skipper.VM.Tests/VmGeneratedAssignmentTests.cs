using Xunit;

namespace Skipper.VM.Tests;

public class VmGeneratedAssignmentTests
{
    [Fact]
    public void Run_Generated_ArrayAssignment_ReturnsValue()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                int[] a = new int[2];
                                a[0] = 42;
                                return a[0];
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(42, result.AsInt());
    }

    [Fact]
    public void Run_Generated_FieldAssignment_ReturnsValue()
    {
        // Arrange
        const string code = """
                            class C { int x; }

                            fn main() -> int {
                                C c = new C();
                                c.x = 7;
                                return c.x;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(7, result.AsInt());
    }

    [Fact]
    public void Run_Generated_AssignmentExpression_ReturnsAssignedValue()
    {
        // Arrange
        const string code = """
                            fn main() -> int {
                                int[] a = new int[1];
                                int x = (a[0] = 5);
                                return x;
                            }
                            """;

        // Act
        var result = TestsHelpers.Run(code);

        // Assert
        Assert.Equal(5, result.AsInt());
    }
}