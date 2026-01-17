using System.Text;
using Skipper.Lexer.Tokens;

namespace Skipper.Lexer.Lexer;

public sealed class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private readonly StringBuilder _tokenBuilder = new();
    private readonly List<Token> _tokens = [];

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        // Типы
        { "int", TokenType.KEYWORD_INT },
        { "double", TokenType.KEYWORD_DOUBLE },
        { "bool", TokenType.KEYWORD_BOOL },
        { "char", TokenType.KEYWORD_CHAR },
        { "string", TokenType.KEYWORD_STRING },
        { "void", TokenType.KEYWORD_VOID },

        // Управляющие конструкции
        { "fn", TokenType.KEYWORD_FN },
        { "return", TokenType.KEYWORD_RETURN },
        { "if", TokenType.KEYWORD_IF },
        { "else", TokenType.KEYWORD_ELSE },
        { "while", TokenType.KEYWORD_WHILE },
        { "for", TokenType.KEYWORD_FOR },

        // Модификаторы и классы
        { "public", TokenType.KEYWORD_PUBLIC },
        { "class", TokenType.KEYWORD_CLASS },
        { "new", TokenType.KEYWORD_NEW },

        // Литералы для булевых значений
        { "true", TokenType.BOOL_LITERAL },
        { "false", TokenType.BOOL_LITERAL }
    };

    public Lexer(string source)
    {
        _source = source;
        _position = 0;
    }

    /// <summary>
    /// Преобразует исходный код в список токенов
    /// </summary>
    public List<Token> Tokenize()
    {
        _tokens.Clear();

        while (true)
        {
            var token = ReadNextToken();
            if (token == null)
            {
                break;
            }

            _tokens.Add(token);
        }

        _tokens.Add(new Token(TokenType.EOF, string.Empty, _position, _line, _column));

        return _tokens;
    }

    /// <summary>
    /// Преобразует исходный код в список токенов с обработкой ошибок
    /// </summary>
    public LexerResult TokenizeWithDiagnostics()
    {
        var tokens = new List<Token>();
        var diagnostics = new List<LexerDiagnostic>();

        var savedPosition = _position;
        var savedLine = _line;
        var savedColumn = _column;

        try
        {
            while (true)
            {
                try
                {
                    var token = ReadNextToken();
                    if (token == null)
                    {
                        break;
                    }

                    if (token.Type == TokenType.BAD)
                    {
                        diagnostics.Add(new LexerDiagnostic(
                            LexerDiagnosticLevel.Error,
                            $"Unknown character '{token.Text}'",
                            token.Line,
                            token.Column
                        ));
                    }
                    else
                    {
                        tokens.Add(token);
                    }
                }
                catch (LexerException ex)
                {
                    diagnostics.Add(new LexerDiagnostic(
                        LexerDiagnosticLevel.Error,
                        ex.Message,
                        ex.Line,
                        ex.Column
                    ));

                    while (Current != '\0' && !char.IsWhiteSpace(Current) && !char.IsLetterOrDigit(Current)
                           && Current != '"' && Current != '\'')
                    {
                        Advance();
                    }
                }
            }

            tokens.Add(new Token(TokenType.EOF, "", _position, _line, _column));
        }
        finally
        {
            _position = savedPosition;
            _line = savedLine;
            _column = savedColumn;
        }

        return new LexerResult(tokens, diagnostics);
    }

    private char Current => _position < _source.Length ? _source[_position] : '\0';

    private char LookAhead => _position + 1 < _source.Length ? _source[_position + 1] : '\0';

    private void Advance()
    {
        if (Current == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        _position++;
    }

    private bool Match(char expected)
    {
        if (Current != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    private Token? ReadNextToken()
    {
        while (true)
        {
            SkipWhitespace();

            if (Current == '\0')
            {
                return null;
            }

            if (Current == '/' && LookAhead == '/')
            {
                SkipLineComment();
                continue;
            }

            if (Current == '/' && LookAhead == '*')
            {
                SkipBlockComment();
                continue;
            }

            if (char.IsDigit(Current))
            {
                return ReadNumber();
            }

            if (char.IsLetter(Current) || Current == '_')
            {
                return ReadIdentifierOrKeyword();
            }

            if (Current == '"')
            {
                return ReadString();
            }

            if (Current == '\'')
            {
                return ReadChar();
            }

            return ReadOperatorOrPunctuation();
        }
    }

    private void SkipWhitespace()
    {
        while (char.IsWhiteSpace(Current))
        {
            Advance();
        }
    }

    private void SkipLineComment()
    {
        Advance(); // /
        Advance(); // /
        while (Current != '\0' && Current != '\n')
        {
            Advance();
        }
    }

    private void SkipBlockComment()
    {
        Advance(); // /
        Advance(); // *
        while (Current != '\0')
        {
            if (Current == '*' && LookAhead == '/')
            {
                Advance(); // *
                Advance(); // /
                return;
            }

            Advance();
        }

        throw new LexerException("Unterminated block comment", _line, _column);
    }

    private Token ReadNumber()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;
        _tokenBuilder.Clear();

        var isDouble = false;

        while (char.IsDigit(Current))
        {
            _tokenBuilder.Append(Current);
            Advance();
        }

        if (Current == '.')
        {
            isDouble = true;
            _tokenBuilder.Append(Current);
            Advance();

            if (!char.IsDigit(Current))
            {
                throw new LexerException("Expected digit after decimal point in number", _line, _column);
            }

            while (char.IsDigit(Current))
            {
                _tokenBuilder.Append(Current);
                Advance();
            }
        }

        var tokenType = isDouble ? TokenType.DOUBLE_LITERAL : TokenType.NUMBER;
        var text = _tokenBuilder.ToString();
        return new Token(tokenType, text, startPos, startLine, startColumn);
    }

    private Token ReadString()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;
        _tokenBuilder.Clear();

        var quoteChar = Current;
        Advance();

        while (Current != '\0' && Current != quoteChar)
        {
            if (Current == '\\')
            {
                _tokenBuilder.Append(ReadEscapeSequence());
            }
            else
            {
                _tokenBuilder.Append(Current);
                Advance();
            }
        }

        if (Current != quoteChar)
        {
            throw new LexerException("Unterminated string literal", startLine, startColumn);
        }

        Advance();

        var value = _tokenBuilder.ToString();
        var text = _source.Substring(startPos, _position - startPos);

        return new Token(TokenType.STRING_LITERAL, value, text, startPos, startLine, startColumn);
    }

    private Token ReadChar()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;
        char chr;

        var quoteChar = Current;
        Advance();

        if (Current == '\\')
        {
            chr = ReadEscapeSequence();
        }
        else
        {
            chr = Current;
            Advance();
        }

        if (Current != quoteChar)
        {
            throw new LexerException("Invalid character literal", startLine, startColumn);
        }

        Advance();

        var text = _source.Substring(startPos, _position - startPos);

        return new Token(TokenType.CHAR_LITERAL, chr, text, startPos, startLine, startColumn);
    }

    private char ReadEscapeSequence()
    {
        Advance(); // \
        if (Current == '\0')
        {
            throw new LexerException("Unterminated escape sequence", _line, _column);
        }

        var result = Current switch
        {
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            '\\' => '\\',
            '\'' => '\'',
            '"' => '"',
            '0' => '\0',
            _ => throw new LexerException($"Unknown escape sequence: \\{Current}", _line, _column)
        };

        Advance();
        return result;
    }

    private Token ReadIdentifierOrKeyword()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;
        _tokenBuilder.Clear();

        _tokenBuilder.Append(Current);
        Advance();

        while (char.IsLetterOrDigit(Current) || Current == '_')
        {
            _tokenBuilder.Append(Current);
            Advance();
        }

        var text = _tokenBuilder.ToString();

        if (Keywords.TryGetValue(text, out var keywordType))
        {
            return new Token(keywordType, text, startPos, startLine, startColumn);
        }

        return new Token(TokenType.IDENTIFIER, text, startPos, startLine, startColumn);
    }

    private Token ReadOperatorOrPunctuation()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;

        switch (Current)
        {
            case '=':
                Advance();
                if (Match('='))
                    return new Token(TokenType.EQUAL, "==", startPos, startLine, startColumn);
                return new Token(TokenType.ASSIGN, "=", startPos, startLine, startColumn);

            case '!':
                Advance();
                if (Match('='))
                    return new Token(TokenType.NOT_EQUAL, "!=", startPos, startLine, startColumn);
                return new Token(TokenType.NOT, "!", startPos, startLine, startColumn);

            case '<':
                Advance();
                if (Match('='))
                    return new Token(TokenType.LESS_EQUAL, "<=", startPos, startLine, startColumn);
                return new Token(TokenType.LESS, "<", startPos, startLine, startColumn);

            case '>':
                Advance();
                if (Match('='))
                    return new Token(TokenType.GREATER_EQUAL, ">=", startPos, startLine, startColumn);
                return new Token(TokenType.GREATER, ">", startPos, startLine, startColumn);

            case '&':
                Advance();
                if (!Match('&'))
                    throw new LexerException("Expected '&' for '&&' operator", _line, _column);
                return new Token(TokenType.AND, "&&", startPos, startLine, startColumn);

            case '|':
                Advance();
                if (!Match('|'))
                    throw new LexerException("Expected '|' for '||' operator", _line, _column);
                return new Token(TokenType.OR, "||", startPos, startLine, startColumn);

            case '-':
                Advance();
                if (Match('>'))
                    return new Token(TokenType.ARROW, "->", startPos, startLine, startColumn);
                if (Match('='))
                    return new Token(TokenType.MINUS_ASSIGN, "-=", startPos, startLine, startColumn);
                return new Token(TokenType.MINUS, "-", startPos, startLine, startColumn);

            case '+':
                Advance();
                if (Match('='))
                    return new Token(TokenType.PLUS_ASSIGN, "+=", startPos, startLine, startColumn);
                return new Token(TokenType.PLUS, "+", startPos, startLine, startColumn);
            case '*':
                Advance();
                if (Match('='))
                    return new Token(TokenType.STAR_ASSIGN, "*=", startPos, startLine, startColumn);
                return new Token(TokenType.STAR, "*", startPos, startLine, startColumn);
            case '/':
                Advance();
                if (Match('='))
                    return new Token(TokenType.SLASH_ASSIGN, "/=", startPos, startLine, startColumn);
                return new Token(TokenType.SLASH, "/", startPos, startLine, startColumn);
            case '%':
                Advance();
                if (Match('='))
                    return new Token(TokenType.MODULO_ASSIGN, "%=", startPos, startLine, startColumn);
                return new Token(TokenType.MODULO, "%", startPos, startLine, startColumn);

            case '(':
                Advance();
                return new Token(TokenType.LPAREN, "(", startPos, startLine, startColumn);
            case ')':
                Advance();
                return new Token(TokenType.RPAREN, ")", startPos, startLine, startColumn);
            case '{':
                Advance();
                return new Token(TokenType.BRACE_OPEN, "{", startPos, startLine, startColumn);
            case '}':
                Advance();
                return new Token(TokenType.BRACE_CLOSE, "}", startPos, startLine, startColumn);
            case '[':
                Advance();
                return new Token(TokenType.BRACKET_OPEN, "[", startPos, startLine, startColumn);
            case ']':
                Advance();
                return new Token(TokenType.BRACKET_CLOSE, "]", startPos, startLine, startColumn);
            case ';':
                Advance();
                return new Token(TokenType.SEMICOLON, ";", startPos, startLine, startColumn);
            case ',':
                Advance();
                return new Token(TokenType.COMMA, ",", startPos, startLine, startColumn);
            case '.':
                Advance();
                return new Token(TokenType.DOT, ".", startPos, startLine, startColumn);
            case '?':
                Advance();
                return new Token(TokenType.QUESTION_MARK, "?", startPos, startLine, startColumn);
            case ':':
                Advance();
                return new Token(TokenType.COLON, ":", startPos, startLine, startColumn);

            default:
                var badChar = Current.ToString();
                Advance();
                return new Token(TokenType.BAD, badChar, startPos, startLine, startColumn);
        }
    }
}
