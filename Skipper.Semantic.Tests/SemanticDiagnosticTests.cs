using Skipper.Lexer.Tokens;
using Xunit;

namespace Skipper.Semantic.Tests;

public class SemanticDiagnosticTests
{
    [Fact]
    public void SemanticDiagnostic_ToString_WithoutToken()
    {
        // Arrange
        var diag = new SemanticDiagnostic(SemanticDiagnosticLevel.Error, "oops");

        // Act
        var text = diag.ToString();

        // Assert
        Assert.Contains("Error", text);
        Assert.Contains("oops", text);
    }

    [Fact]
    public void SemanticDiagnostic_ToString_WithToken()
    {
        // Arrange
        var token = new Token(TokenType.IDENTIFIER, "x", 0, 3, 4);
        var diag = new SemanticDiagnostic(SemanticDiagnosticLevel.Warning, "bad", token);

        // Act
        var text = diag.ToString();

        // Assert
        Assert.Contains("Warning", text);
        Assert.Contains("bad", text);
        Assert.Contains("3:4", text);
    }
}
