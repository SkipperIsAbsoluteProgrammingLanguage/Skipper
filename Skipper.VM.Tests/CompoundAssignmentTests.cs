using Xunit;

namespace Skipper.VM.Tests;

public class CompoundAssignmentTests
{
    [Fact]
    public void CompoundAssignment_UpdatesVariable()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int a = 1;
                                a += 2;
                                a *= 3;
                                a -= 1;
                                a /= 2;
                                print(a);
                            }
                            """;

        // Act
        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });

        // Assert
        Assert.Contains("4", output);
    }
}
