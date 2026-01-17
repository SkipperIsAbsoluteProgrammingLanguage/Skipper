using Skipper.Lexer.Tokens;
using Skipper.Parser.Parser;
using Xunit;

namespace Skipper.Parser.Tests;

public class DiagnosticTests
{
    [Fact]
    public void ParserDiagnostic_ToString_WithoutToken()
    {
        // Arrange
        var diag = new ParserDiagnostic(ParserDiagnosticLevel.Error, "oops");

        // Act
        var text = diag.ToString();

        // Assert
        Assert.Contains("Error", text);
        Assert.Contains("oops", text);
    }

    [Fact]
    public void ParserDiagnostic_ToString_WithEofToken()
    {
        // Arrange
        var token = new Token(TokenType.EOF, string.Empty, 0, 1, 1);
        var diag = new ParserDiagnostic(ParserDiagnosticLevel.Warning, "unexpected", token);

        // Act
        var text = diag.ToString();

        // Assert
        Assert.Contains("Warning", text);
        Assert.Contains("unexpected", text);
        Assert.Contains("end of file", text);
    }

    [Fact]
    public void ParserDiagnostic_ToString_WithTokenLocation()
    {
        // Arrange
        var token = new Token(TokenType.IDENTIFIER, "x", 0, 2, 5);
        var diag = new ParserDiagnostic(ParserDiagnosticLevel.Info, "message", token);

        // Act
        var text = diag.ToString();

        // Assert
        Assert.Contains("Info", text);
        Assert.Contains("message", text);
        Assert.Contains("line 2", text);
        Assert.Contains("column 5", text);
        Assert.Contains("'x'", text);
    }

    [Fact]
    public void ParserException_FormatsWithoutToken()
    {
        // Arrange & Act
        var ex = new ParserException("missing token");

        // Assert
        Assert.Contains("missing token", ex.Message);
    }

    [Fact]
    public void ParserException_FormatsWithEofToken()
    {
        // Arrange
        var token = new Token(TokenType.EOF, string.Empty, 0, 1, 1);

        // Act
        var ex = new ParserException("missing token", token);

        // Assert
        Assert.Contains("missing token", ex.Message);
        Assert.Contains("end of file", ex.Message);
    }

    [Fact]
    public void ParserException_FormatsWithTokenLocation()
    {
        // Arrange
        var token = new Token(TokenType.IDENTIFIER, "name", 0, 4, 2);

        // Act
        var ex = new ParserException("bad", token);

        // Assert
        Assert.Contains("bad", ex.Message);
        Assert.Contains("'name'", ex.Message);
        Assert.Contains("line 4", ex.Message);
        Assert.Contains("column 2", ex.Message);
    }
}
