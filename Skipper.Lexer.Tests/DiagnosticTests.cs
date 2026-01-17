using Skipper.Lexer.Lexer;
using Xunit;

namespace Skipper.Lexer.Tests;

public class DiagnosticTests
{
    [Theory]
    [InlineData(LexerDiagnosticLevel.Error, "Error")]
    [InlineData(LexerDiagnosticLevel.Warning, "Warning")]
    [InlineData(LexerDiagnosticLevel.Info, "Information")]
    public void LexerDiagnostic_ToString_FormatsMessage(LexerDiagnosticLevel level, string levelText)
    {
        // Arrange
        var diag = new LexerDiagnostic(level, "bad char", 3, 7);

        // Act
        var text = diag.ToString();

        // Assert
        Assert.Contains(levelText, text);
        Assert.Contains("bad char", text);
        Assert.Contains("line 3", text);
        Assert.Contains("column 7", text);
    }

    [Fact]
    public void LexerException_StoresLocationAndMessage()
    {
        // Arrange & Act
        var ex = new LexerException("oops", 2, 9);

        // Assert
        Assert.Equal(2, ex.Line);
        Assert.Equal(9, ex.Column);
        Assert.Contains("oops", ex.Message);
        Assert.Contains("line 2, column 9", ex.Message);
    }
}
