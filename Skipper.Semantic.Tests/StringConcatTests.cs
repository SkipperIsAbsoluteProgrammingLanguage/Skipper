using Xunit;

namespace Skipper.Semantic.Tests;

public class StringConcatTests
{
    [Theory]
    [InlineData("string s = \"x=\" + true;\n")]
    [InlineData("string s = \"d=\" + 1.5;\n")]
    [InlineData("string s = \"c=\" + 'a';\n")]
    [InlineData("long l = 7;\nstring s = \"l=\" + l;\n")]
    [InlineData("int i = 3;\nstring s = \"i=\" + i;\n")]
    public void StringPlusScalar_IsAllowed(string body)
    {
        // Arrange
        var code = "fn main() {\n" + body + "}\n";

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }

    [Theory]
    [InlineData("string s = true + \"x=\";\n")]
    [InlineData("string s = 1.5 + \"d=\";\n")]
    [InlineData("string s = 'a' + \"c=\";\n")]
    [InlineData("long l = 7;\nstring s = l + \"l=\";\n")]
    [InlineData("int i = 3;\nstring s = i + \"i=\";\n")]
    public void ScalarPlusString_IsAllowed(string body)
    {
        // Arrange
        var code = "fn main() {\n" + body + "}\n";

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Empty(semantic.Diagnostics);
    }
}
