using Xunit;

namespace Skipper.Parser.Tests;

public class ErrorTests
{
    [Fact]
    public void Parse_MissingSemicolon_ReportsError()
    {
        // Arrange
        const string source = "fn main() { x = 5 }";

        // Act
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());
        var parserResult = parser.Parse();

        // Assert
        Assert.True(parserResult.HasErrors);
        Assert.Contains(parserResult.Diagnostics, d => d.Message.Contains("Expected ';'"));
    }

    [Fact]
    public void Parse_MissingBrace_ReportsError()
    {
        // Assert
        const string source = "fn main() { return; ";

        // Act
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());
        var parserResult = parser.Parse();

        // Assert
        Assert.True(parserResult.HasErrors);
        Assert.Contains(parserResult.Diagnostics, d => d.Message.Contains("Expected '}'"));
    }

    [Fact]
    public void Parse_InvalidExpression_Synchronizes()
    {
        // Arrange
        const string source = """
                              fn main() {
                                  int x = ;
                                  int y = 10;
                              }
                              """;

        // Act
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());
        var parserResult = parser.Parse();

        // Assert
        Assert.True(parserResult.HasErrors);
        Assert.NotEmpty(parserResult.Root.Declarations);
    }

    [Fact]
    public void Parse_InvalidAssignmentTarget_ReportsError()
    {
        // Arrange
        const string source = "fn main() { 10 = x; }";

        // Act
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());
        var parserResult = parser.Parse();

        // Assert
        Assert.True(parserResult.HasErrors);
        Assert.Contains(parserResult.Diagnostics, d => d.Message.Contains("Invalid assignment target"));
    }

    [Fact]
    public void Parse_UnexpectedEOF_ReportsError()
    {
        // Arrange
        const string source = "fn main() { x = 5";

        // Act
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());
        var parserResult = parser.Parse();

        // Assert
        Assert.True(parserResult.HasErrors);
        Assert.NotEmpty(parserResult.Diagnostics);
    }
}