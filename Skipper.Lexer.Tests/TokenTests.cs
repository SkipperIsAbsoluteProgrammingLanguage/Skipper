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
    [InlineData(TokenType.DOUBLE_LITERAL, true)]
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
    [InlineData(TokenType.PLUS_ASSIGN, true)]
    [InlineData(TokenType.MINUS, true)]
    [InlineData(TokenType.MINUS_ASSIGN, true)]
    [InlineData(TokenType.STAR, true)]
    [InlineData(TokenType.STAR_ASSIGN, true)]
    [InlineData(TokenType.SLASH, true)]
    [InlineData(TokenType.SLASH_ASSIGN, true)]
    [InlineData(TokenType.MODULO, true)]
    [InlineData(TokenType.MODULO_ASSIGN, true)]
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

        // Act
        var intValue = intToken.GetNumericValue();

        // Assert
        Assert.Equal(42, intValue);
    }

    [Fact]
    public void Token_GetDoubleValue_ReturnsCorrectValue()
    {
        // Arrange
        var doubleToken = new Token(TokenType.DOUBLE_LITERAL, "3.14");

        // Act
        var doubleValue = doubleToken.GetDoubleValue();

        // Assert
        Assert.Equal(3.14, doubleValue);
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
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"world\"", "world")]
    public void Token_GetStringValue_ReturnsCorrectValue(string text, string expected)
    {
        // Arrange
        var token = new Token(TokenType.STRING_LITERAL, expected, text);

        // Act
        var value = token.GetStringValue();

        // Assert
        Assert.Equal(expected, value);
    }

    [Fact]
    public void Token_GetStringValue_ThrowsForNonStringToken()
    {
        // Arrange
        var token = new Token(TokenType.NUMBER, "123");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => token.GetStringValue());
        Assert.Contains("is not a string literal", ex.Message);
    }

    [Theory]
    [InlineData("'a'", 'a')]
    [InlineData("'\\n'", '\n')]
    public void Token_GetCharValue_ReturnsCorrectValue(string text, char expected)
    {
        // Arrange
        var token = new Token(TokenType.CHAR_LITERAL, expected, text);

        // Act
        var value = token.GetCharValue();

        // Assert
        Assert.Equal(expected, value);
    }

    [Fact]
    public void Token_GetCharValue_ThrowsForNonCharToken()
    {
        // Arrange
        var token = new Token(TokenType.NUMBER, "123");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => token.GetCharValue());
        Assert.Contains("is not a character literal", ex.Message);
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

    [Fact]
    public void GetNumericValue_Overflow_ThrowsInvalidOperationException()
    {
        // Arrange
        // Число больше int.MaxValue (2147483647)
        const string largeNumber = "2147483648";
        var token = new Token(TokenType.NUMBER, largeNumber);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => token.GetNumericValue());
    }
}
