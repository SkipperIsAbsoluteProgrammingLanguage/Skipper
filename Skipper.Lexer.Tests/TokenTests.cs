using Skipper.Lexer.Tokens;
using Xunit;

namespace Skipper.Lexer.Tests;

public class TokenTests
{
    [Fact]
    public void Token_Constructor_SetsProperties()
    {
        // Arrange & Act
        var token = new Token(TokenType.NUMBER, "123", 10, 2, 5);

        // Assert
        Assert.Equal(TokenType.NUMBER, token.Type);
        Assert.Equal("123", token.Text);
        Assert.Equal(10, token.StartPosition);
        Assert.Equal(2, token.Line);
        Assert.Equal(5, token.Column);
        Assert.Equal(3, token.Length);
        Assert.Equal(13, token.EndPosition);
    }

    [Fact]
    public void Token_Is_ReturnsTrueOnlyForMatchingType()
    {
        var token = new Token(TokenType.IDENTIFIER, "x");

        Assert.True(token.Is(TokenType.IDENTIFIER));
        Assert.False(token.Is(TokenType.NUMBER));
    }

    [Fact]
    public void Token_IsAny_ReturnsTrueIfTypeIsInList()
    {
        var token = new Token(TokenType.PLUS, "+");

        Assert.True(token.IsAny(TokenType.MINUS, TokenType.PLUS));
        Assert.False(token.IsAny(TokenType.STAR, TokenType.SLASH));
    }

    [Theory]
    [InlineData(TokenType.KEYWORD_FN, true)]
    [InlineData(TokenType.KEYWORD_INT, true)]
    [InlineData(TokenType.KEYWORD_RETURN, true)]
    [InlineData(TokenType.IDENTIFIER, false)]
    [InlineData(TokenType.NUMBER, false)]
    public void Token_IsKeyword_ReturnsTrueForKeywords(TokenType type, bool expected)
    {
        // Arrange
        var token = new Token(type, "test");

        // Act & Assert
        Assert.Equal(expected, token.IsKeyword);
    }

    [Theory]
    [InlineData(TokenType.NUMBER, true)]
    [InlineData(TokenType.FLOAT_LITERAL, true)]
    [InlineData(TokenType.STRING_LITERAL, true)]
    [InlineData(TokenType.CHAR_LITERAL, true)]
    [InlineData(TokenType.BOOL_LITERAL, true)]
    [InlineData(TokenType.IDENTIFIER, false)]
    [InlineData(TokenType.PLUS, false)]
    public void Token_IsLiteral_ReturnsTrueForLiterals(TokenType type, bool expected)
    {
        // Arrange
        var token = new Token(type, "test");

        // Act & Assert
        Assert.Equal(expected, token.IsLiteral);
    }

    [Theory]
    [InlineData(TokenType.PLUS, true)]
    [InlineData(TokenType.MINUS, true)]
    [InlineData(TokenType.STAR, true)]
    [InlineData(TokenType.SLASH, true)]
    [InlineData(TokenType.ASSIGN, true)]
    [InlineData(TokenType.EQUAL, true)]
    [InlineData(TokenType.NOT_EQUAL, true)]
    [InlineData(TokenType.LESS, true)]
    [InlineData(TokenType.GREATER, true)]
    [InlineData(TokenType.LESS_EQUAL, true)]
    [InlineData(TokenType.GREATER_EQUAL, true)]
    [InlineData(TokenType.AND, true)]
    [InlineData(TokenType.OR, true)]
    [InlineData(TokenType.NOT, true)]
    [InlineData(TokenType.NUMBER, false)]
    [InlineData(TokenType.IDENTIFIER, false)]
    [InlineData(TokenType.STRING_LITERAL, false)]
    public void Token_IsOperator_ReturnsTrueForOperators(TokenType type, bool expected)
    {
        // Arrange
        var token = new Token(type, "test");

        // Act & Assert
        Assert.Equal(expected, token.IsOperator);
    }

    [Fact]
    public void Token_GetNumericValue_ReturnsCorrectValue()
    {
        // Arrange
        var intToken = new Token(TokenType.NUMBER, "42");
        var floatToken = new Token(TokenType.FLOAT_LITERAL, "3.14");

        // Act
        var intValue = intToken.GetNumericValue();
        var floatValue = floatToken.GetNumericValue();

        // Assert
        Assert.Equal(42L, intValue);
        Assert.Equal(3.14, (double)floatValue, 5);
    }

    [Fact]
    public void Token_GetNumericValue_ThrowsForNonNumericToken()
    {
        // Arrange
        var token = new Token(TokenType.IDENTIFIER, "x");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => token.GetNumericValue());
        Assert.Contains("is not a numeric literal", ex.Message);
    }

    [Fact]
    public void Token_GetBoolValue_ReturnsCorrectValue()
    {
        // Arrange
        var trueToken = new Token(TokenType.BOOL_LITERAL, "true");
        var falseToken = new Token(TokenType.BOOL_LITERAL, "false");

        // Act & Assert
        Assert.True(trueToken.GetBoolValue());
        Assert.False(falseToken.GetBoolValue());
    }

    [Fact]
    public void Token_GetBoolValue_ThrowsForNonBooleanToken()
    {
        // Arrange
        var token = new Token(TokenType.NUMBER, "1");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => token.GetBoolValue());
        Assert.Contains("is not a boolean literal", ex.Message);
    }

    [Theory]
    [InlineData("\"hello\"", "hello", TokenType.STRING_LITERAL)]
    [InlineData("\"world\"", "world", TokenType.STRING_LITERAL)]
    [InlineData("'a'", "a", TokenType.CHAR_LITERAL)]
    [InlineData("'\\n'", "\\n", TokenType.CHAR_LITERAL)]
    public void Token_GetStringValue_ReturnsCorrectValue(string text, string expected, TokenType type)
    {
        // Arrange
        var token = new Token(type, text);

        // Act
        var value = token.GetStringValue();

        // Assert
        Assert.Equal(expected, value);
    }

    [Fact]
    public void Token_GetStringValue_ThrowsForNonStringOrCharToken()
    {
        // Arrange
        var token = new Token(TokenType.NUMBER, "123");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => token.GetStringValue());
        Assert.Contains("is not a string or character literal", ex.Message);
    }

    [Fact]
    public void Token_ToString_ReturnsFormattedString()
    {
        // Arrange
        var token = new Token(TokenType.NUMBER, "123", 10, 2, 5);

        // Act
        var result = token.ToString();

        // Assert
        Assert.Equal("Token(NUMBER, '123' at 2:5)", result);
    }
}