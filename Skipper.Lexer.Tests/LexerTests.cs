using Skipper.Lexer.Lexer;
using Skipper.Lexer.Tokens;
using Xunit;

namespace Skipper.Lexer.Tests;

public class LexerTests
{
    [Fact]
    public void Tokenize_EmptyString_ReturnsOnlyEOF()
    {
        // Arrange
        var lexer = new Lexer.Lexer("");

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Single(result);
        Assert.Equal(TokenType.EOF, result[0].Type);
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_ReturnsOnlyEOF()
    {
        // Arrange
        var lexer = new Lexer.Lexer("   \t\n  ");

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Single(result);
        Assert.Equal(TokenType.EOF, result[0].Type);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("123", "123")]
    [InlineData("999999", "999999")]
    public void Tokenize_IntegerLiteral_ReturnsCorrectToken(string input, string expectedText)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(TokenType.NUMBER, result[0].Type);
        Assert.Equal(expectedText, result[0].Text);
    }

    [Theory]
    [InlineData("3.14", "3.14")]
    [InlineData("0.5", "0.5")]
    [InlineData("2.0", "2.0")]
    public void Tokenize_DoubleLiteral_ReturnsCorrectToken(string input, string expectedText)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(TokenType.DOUBLE_LITERAL, result[0].Type);
        Assert.Equal(expectedText, result[0].Text);
    }

    [Theory]
    [InlineData("123.")]
    [InlineData("0.")]
    [InlineData("45.e10")]
    public void Tokenize_NumberWithDotWithoutFraction_ThrowsLexerException(string input)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act & Assert
        var ex = Assert.Throws<LexerException>(() => lexer.Tokenize());
        Assert.Contains("Expected digit after decimal point in number", ex.Message);
    }

    [Theory]
    [InlineData("+", TokenType.PLUS)]
    [InlineData("-", TokenType.MINUS)]
    [InlineData("*", TokenType.STAR)]
    [InlineData("/", TokenType.SLASH)]
    [InlineData("%", TokenType.MODULO)]
    public void Tokenize_SingleCharacterOperator_ReturnsCorrectToken(string input, TokenType expectedType)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedType, result[0].Type);
        Assert.Equal(input, result[0].Text);
    }

    [Theory]
    [InlineData("==", TokenType.EQUAL)]
    [InlineData("!=", TokenType.NOT_EQUAL)]
    [InlineData("<=", TokenType.LESS_EQUAL)]
    [InlineData(">=", TokenType.GREATER_EQUAL)]
    [InlineData("&&", TokenType.AND)]
    [InlineData("||", TokenType.OR)]
    [InlineData("->", TokenType.ARROW)]
    public void Tokenize_DoubleCharacterOperator_ReturnsCorrectToken(string input, TokenType expectedType)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedType, result[0].Type);
        Assert.Equal(input, result[0].Text);
    }

    [Theory]
    [InlineData("fn", TokenType.KEYWORD_FN)]
    [InlineData("int", TokenType.KEYWORD_INT)]
    [InlineData("long", TokenType.KEYWORD_LONG)]
    [InlineData("double", TokenType.KEYWORD_DOUBLE)]
    [InlineData("bool", TokenType.KEYWORD_BOOL)]
    [InlineData("char", TokenType.KEYWORD_CHAR)]
    [InlineData("string", TokenType.KEYWORD_STRING)]
    [InlineData("return", TokenType.KEYWORD_RETURN)]
    [InlineData("if", TokenType.KEYWORD_IF)]
    [InlineData("else", TokenType.KEYWORD_ELSE)]
    [InlineData("while", TokenType.KEYWORD_WHILE)]
    [InlineData("for", TokenType.KEYWORD_FOR)]
    [InlineData("public", TokenType.KEYWORD_PUBLIC)]
    [InlineData("class", TokenType.KEYWORD_CLASS)]
    [InlineData("new", TokenType.KEYWORD_NEW)]
    [InlineData("void", TokenType.KEYWORD_VOID)]
    public void Tokenize_Keyword_ReturnsCorrectToken(string input, TokenType expectedType)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedType, result[0].Type);
        Assert.Equal(input, result[0].Text);
    }

    [Theory]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    public void Tokenize_BoolLiteral_ReturnsCorrectToken(string input, string expectedText)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(TokenType.BOOL_LITERAL, result[0].Type);
        Assert.Equal(expectedText, result[0].Text);
    }

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"\"", "")]
    [InlineData("\"a\"", "a")]
    [InlineData("\"Say \\\"Hello\\\"!\"", "Say \"Hello\"!")] // "Say \"Hello\"!" -> Say "Hello"!
    [InlineData("\"Path\\\\To\\\\File\"", "Path\\To\\File")] // "Path\\To\\File" -> Path\To\File
    public void Tokenize_StringLiteral_ReturnsCorrectToken(string input, string expectedValue)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(TokenType.STRING_LITERAL, result[0].Type);
        Assert.Equal(input, result[0].Text);

        var actualValue = result[0].GetStringValue();
        Assert.Equal(expectedValue, actualValue);
    }

    [Theory]
    [InlineData("'a'", 'a')]
    [InlineData("'4'", '4')]
    [InlineData(@"'\''", '\'')]
    [InlineData(@"'\n'", '\n')]
    [InlineData(@"'\\'", '\\')]
    public void Tokenize_CharLiteral_ReturnsCorrectToken(string input, char expectedValue)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(TokenType.CHAR_LITERAL, result[0].Type);
        Assert.Equal(input, result[0].Text);

        var actualValue = result[0].GetCharValue();
        Assert.Equal(expectedValue, actualValue);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("variable")]
    [InlineData("_private")]
    [InlineData("var123")]
    [InlineData("camelCase")]
    [InlineData("PascalCase")]
    [InlineData("var1")]
    [InlineData("point2d")]
    [InlineData("i_am_snake_case")]
    public void Tokenize_Identifier_ReturnsCorrectToken(string input)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(TokenType.IDENTIFIER, result[0].Type);
        Assert.Equal(input, result[0].Text);
    }

    [Theory]
    [InlineData("(", TokenType.LPAREN)]
    [InlineData(")", TokenType.RPAREN)]
    [InlineData("{", TokenType.BRACE_OPEN)]
    [InlineData("}", TokenType.BRACE_CLOSE)]
    [InlineData("[", TokenType.BRACKET_OPEN)]
    [InlineData("]", TokenType.BRACKET_CLOSE)]
    [InlineData(";", TokenType.SEMICOLON)]
    [InlineData(",", TokenType.COMMA)]
    [InlineData(".", TokenType.DOT)]
    [InlineData("?", TokenType.QUESTION_MARK)]
    [InlineData(":", TokenType.COLON)]
    public void Tokenize_Punctuation_ReturnsCorrectToken(string input, TokenType expectedType)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedType, result[0].Type);
        Assert.Equal(input, result[0].Text);
    }

    [Fact]
    public void Tokenize_SimpleExpression_ReturnsCorrectSequence()
    {
        // Arrange
        const string source = "1 + 2 * 3";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        var expectedTypes = new[]
        {
            TokenType.NUMBER, // 1
            TokenType.PLUS, // +
            TokenType.NUMBER, // 2
            TokenType.STAR, // *
            TokenType.NUMBER, // 3
            TokenType.EOF // конец
        };

        Assert.Equal(expectedTypes.Length, result.Count);

        for (var i = 0; i < expectedTypes.Length; i++)
        {
            Assert.Equal(expectedTypes[i], result[i].Type);
        }
    }

    [Fact]
    public void Tokenize_FunctionDeclaration_ReturnsCorrectTokens()
    {
        // Arrange
        const string source = "fn factorial(int n) -> int";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        var expectedTypes = new[]
        {
            TokenType.KEYWORD_FN, // fn
            TokenType.IDENTIFIER, // factorial
            TokenType.LPAREN, // (
            TokenType.KEYWORD_INT, // int
            TokenType.IDENTIFIER, // n
            TokenType.RPAREN, // )
            TokenType.ARROW, // ->
            TokenType.KEYWORD_INT, // int
            TokenType.EOF // конец
        };

        Assert.Equal(expectedTypes.Length, result.Count);

        for (var i = 0; i < expectedTypes.Length; i++)
        {
            Assert.Equal(expectedTypes[i], result[i].Type);
        }
    }

    [Fact]
    public void Tokenize_FunctionDeclaration_WithLong_ReturnsCorrectTokens()
    {
        // Arrange
        const string source = "fn sum(long a) -> long";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        var expectedTypes = new[]
        {
            TokenType.KEYWORD_FN,
            TokenType.IDENTIFIER,
            TokenType.LPAREN,
            TokenType.KEYWORD_LONG,
            TokenType.IDENTIFIER,
            TokenType.RPAREN,
            TokenType.ARROW,
            TokenType.KEYWORD_LONG,
            TokenType.EOF
        };

        Assert.Equal(expectedTypes.Length, result.Count);

        for (var i = 0; i < expectedTypes.Length; i++)
        {
            Assert.Equal(expectedTypes[i], result[i].Type);
        }
    }

    [Fact]
    public void Tokenize_IfStatement_ReturnsCorrectTokens()
    {
        // Arrange
        const string source = "if (x > 0) { return true; }";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        var expectedTypes = new[]
        {
            TokenType.KEYWORD_IF, // if
            TokenType.LPAREN, // (
            TokenType.IDENTIFIER, // x
            TokenType.GREATER, // >
            TokenType.NUMBER, // 0
            TokenType.RPAREN, // )
            TokenType.BRACE_OPEN, // {
            TokenType.KEYWORD_RETURN, // return
            TokenType.BOOL_LITERAL, // true
            TokenType.SEMICOLON, // ;
            TokenType.BRACE_CLOSE, // }
            TokenType.EOF // конец
        };

        Assert.Equal(expectedTypes.Length, result.Count);

        for (var i = 0; i < expectedTypes.Length; i++)
        {
            Assert.Equal(expectedTypes[i], result[i].Type);
        }
    }

    [Fact]
    public void Tokenize_WithLineComment_IgnoresComment()
    {
        // Arrange
        const string source = "x = 5 // это комментарий";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(4, result.Count); // x = 5 + EOF
        Assert.Equal(TokenType.IDENTIFIER, result[0].Type);
        Assert.Equal(TokenType.ASSIGN, result[1].Type);
        Assert.Equal(TokenType.NUMBER, result[2].Type);
    }

    [Fact]
    public void Tokenize_WithBlockComment_IgnoresComment()
    {
        // Arrange
        const string source = "x = /* комментарий */ 5";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(4, result.Count); // x = 5 + EOF
        Assert.Equal(TokenType.IDENTIFIER, result[0].Type);
        Assert.Equal(TokenType.ASSIGN, result[1].Type);
        Assert.Equal(TokenType.NUMBER, result[2].Type);
    }

    [Fact]
    public void Tokenize_InvalidCharacter_ReturnsBADToken()
    {
        // Arrange
        const string source = "x = $invalid";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        var badToken = result.FirstOrDefault(t => t.Type == TokenType.BAD);
        Assert.NotNull(badToken);
        Assert.Equal("$", badToken.Text);
    }

    [Fact]
    public void Tokenize_UnterminatedString_ThrowsException()
    {
        // Arrange
        const string source = "\"незакрытая строка";
        var lexer = new Lexer.Lexer(source);

        // Act & Assert
        Assert.Throws<LexerException>(() => lexer.Tokenize());
    }

    [Fact]
    public void Tokenize_TracksPositionCorrectly()
    {
        // Arrange
        const string source = "x = 5\n y = 10";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(1, result[0].Line); // x
        Assert.Equal(1, result[0].Column);

        Assert.Equal(1, result[1].Line); // =
        Assert.Equal(3, result[1].Column);

        Assert.Equal(1, result[2].Line); // 5
        Assert.Equal(5, result[2].Column);

        Assert.Equal(2, result[3].Line);
        Assert.Equal(2, result[3].Column);
    }

    [Fact]
    public void TokenizeWithDiagnostics_InvalidInput_ReturnsDiagnostics()
    {
        // Arrange
        const string source = "x @ y";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.TokenizeWithDiagnostics();

        // Assert
        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var error = result.Diagnostics[0];
        Assert.Equal(LexerDiagnosticLevel.Error, error.Level);
        Assert.Contains("Unknown character", error.Message);
    }

    [Fact]
    public void Tokenize_UnterminatedBlockComment_ThrowsException()
    {
        // Arrange
        const string source = "/* это начало комментария, а конца нет... ";
        var lexer = new Lexer.Lexer(source);

        // Act & Assert
        Assert.Throws<LexerException>(() => lexer.Tokenize());
    }

    [Fact]
    public void Tokenize_KeywordPrefixInIdentifier_ReturnsIdentifier()
    {
        // Arrange
        // "int" и "return" — ключевые слова, но здесь они часть имени
        const string source = "integer returnVal internal";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(4, result.Count); // 3 id + EOF
        Assert.Equal(TokenType.IDENTIFIER, result[0].Type);
        Assert.Equal("integer", result[0].Text);

        Assert.Equal(TokenType.IDENTIFIER, result[1].Type);
        Assert.Equal("returnVal", result[1].Text);

        Assert.Equal(TokenType.IDENTIFIER, result[2].Type);
        Assert.Equal("internal", result[2].Text);
    }

    [Theory]
    [InlineData("_")]
    [InlineData("__init__")]
    [InlineData("_123")]
    [InlineData("var_name_1")]
    public void Tokenize_ComplexIdentifiers_ReturnsIdentifier(string input)
    {
        // Arrange
        var lexer = new Lexer.Lexer(input);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(TokenType.IDENTIFIER, result[0].Type);
        Assert.Equal(input, result[0].Text);
    }

    [Fact]
    public void Tokenize_NoSpacesAroundOperators_ReturnsCorrectTokens()
    {
        // Arrange
        const string source = "x=1+y*(z-2);";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        // x, =, 1, +, y, *, (, z, -, 2, ), ;, EOF
        Assert.Equal(13, result.Count);
        Assert.Equal(TokenType.IDENTIFIER, result[0].Type); // x
        Assert.Equal(TokenType.ASSIGN, result[1].Type); // =
        Assert.Equal(TokenType.NUMBER, result[2].Type); // 1
        Assert.Equal(TokenType.PLUS, result[3].Type); // +
        Assert.Equal(TokenType.IDENTIFIER, result[4].Type); // y
    }

    [Fact]
    public void Tokenize_LineCommentAtEOF_DoesNotCrash()
    {
        // Arrange
        const string source = "x = 1 // end of file";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        Assert.Equal(4, result.Count); // x, =, 1, EOF
        Assert.Equal(TokenType.NUMBER, result[2].Type);
        Assert.Equal(TokenType.EOF, result[3].Type);
    }

    [Fact]
    public void Tokenize_MultipleDots_SplitsTokens()
    {
        // Arrange
        const string source = "1.2.3";
        var lexer = new Lexer.Lexer(source);

        // Act
        var result = lexer.Tokenize();

        // Assert
        // Ожидаем: [Double 1.2], [Dot .], [Int 3], [EOF]
        Assert.Equal(4, result.Count);
        Assert.Equal(TokenType.DOUBLE_LITERAL, result[0].Type);
        Assert.Equal("1.2", result[0].Text);

        Assert.Equal(TokenType.DOT, result[1].Type);

        Assert.Equal(TokenType.NUMBER, result[2].Type);
        Assert.Equal("3", result[2].Text);
    }
}
