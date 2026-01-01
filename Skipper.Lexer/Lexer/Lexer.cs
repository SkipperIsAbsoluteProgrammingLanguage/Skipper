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
        { "float", TokenType.KEYWORD_FLOAT },
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

        _tokens.Add(new Token(TokenType.EOF, "", _position, _line, _column));

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
                            $"Неизвестный символ '{token.Text}'",
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

    /// <summary>
    ///  Текущий символ в позиции курсора
    /// </summary>
    private char Current => _position < _source.Length ? _source[_position] : '\0';

    /// <summary>
    /// Следующий символ после текущего (без перемещения курсора)
    /// </summary>
    private char LookAhead => _position + 1 < _source.Length ? _source[_position + 1] : '\0';

    /// <summary>
    /// Читает следующий токен из исходного кода
    /// </summary>
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
                return ReadIdentifier();
            }

            if (Current is '"' or '\'')
            {
                return ReadString();
            }

            return ReadOperatorOrPunctuation();
        }
    }

    /// <summary>
    /// Пропускает пробельные символы
    /// </summary>
    private void SkipWhitespace()
    {
        while (char.IsWhiteSpace(Current))
        {
            Advance();
        }
    }

    /// <summary>
    /// Пропускает однострочный комментарий
    /// </summary>
    private void SkipLineComment()
    {
        Advance(); // /
        Advance(); // /
        while (Current != '\0' && Current != '\n')
        {
            Advance();
        }
    }

    /// <summary>
    /// Пропускает многострочный комментарий
    /// </summary>
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

        throw new LexerException("Незакрытый блочный комментарий", _line, _column);
    }

    /// <summary>
    /// Перемещает курсор на один символ вперед
    /// </summary>
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

    /// <summary>
    /// Пропускает символ, если он совпадает с ожидаемым
    /// </summary>
    private bool Match(char expected)
    {
        if (Current != expected) return false;
        Advance();
        return true;
    }

    /// <summary>
    /// Распознает числовой литерал (целый или с плавающей точкой)
    /// </summary>
    private Token ReadNumber()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;
        _tokenBuilder.Clear();

        var isFloat = false;

        while (char.IsDigit(Current))
        {
            _tokenBuilder.Append(Current);
            Advance();
        }

        if (Current == '.')
        {
            isFloat = true;
            _tokenBuilder.Append(Current);
            Advance();

            if (!char.IsDigit(Current))
            {
                throw new LexerException("Ожидалась цифра после точки в числе", _line, _column);
            }

            while (char.IsDigit(Current))
            {
                _tokenBuilder.Append(Current);
                Advance();
            }
        }

        if (Current is 'e' or 'E')
        {
            isFloat = true;
            _tokenBuilder.Append(Current);
            Advance();

            if (Current is '+' or '-')
            {
                _tokenBuilder.Append(Current);
                Advance();
            }

            if (!char.IsDigit(Current))
            {
                throw new LexerException("Ожидалась цифра в экспоненте", _line, _column);
            }

            while (char.IsDigit(Current))
            {
                _tokenBuilder.Append(Current);
                Advance();
            }
        }

        var tokenType = isFloat ? TokenType.FLOAT_LITERAL : TokenType.NUMBER;
        var text = _tokenBuilder.ToString();
        return new Token(tokenType, text, startPos, startLine, startColumn);
    }

    /// <summary>
    /// Распознает строковый литерал
    /// </summary>
    private Token ReadString()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;
        _tokenBuilder.Clear();

        var quoteChar = Current;
        var isCharLiteral = quoteChar == '\'';
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
            throw new LexerException("Незакрытый строковый литерал", startLine, startColumn);
        }

        Advance();

        var content = _tokenBuilder.ToString();

        if (isCharLiteral && content.Length != 1)
        {
            throw new LexerException($"Недопустимый символьный литерал: '{content}'", startLine, startColumn);
        }

        var text = quoteChar + content + quoteChar;
        var tokenType = isCharLiteral ? TokenType.CHAR_LITERAL : TokenType.STRING_LITERAL;

        return new Token(tokenType, text, startPos, startLine, startColumn);
    }

    /// <summary>
    /// Читает escape-последовательность
    /// </summary>
    private char ReadEscapeSequence()
    {
        Advance(); // \
        if (Current == '\0')
        {
            throw new LexerException("Незавершенная escape-последовательность", _line, _column);
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
            _ => throw new LexerException($"Неизвестная escape-последовательность: \\{Current}", _line, _column)
        };

        Advance();
        return result;
    }

    /// <summary>
    /// Распознает идентификатор или ключевое слово
    /// </summary>
    private Token ReadIdentifier()
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

    /// <summary>
    /// Распознает операторы и другие символы
    /// </summary>
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
                    throw new LexerException("Ожидался '&'", _line, _column); // TODO: bitwise and
                return new Token(TokenType.AND, "&&", startPos, startLine, startColumn);

            case '|':
                Advance();
                if (!Match('|'))
                    throw new LexerException("Ожидался '|'", _line, _column); // TODO: bitwise or
                return new Token(TokenType.OR, "||", startPos, startLine, startColumn);

            case '-':
                Advance();
                if (Match('>'))
                    return new Token(TokenType.ARROW, "->", startPos, startLine, startColumn);
                return new Token(TokenType.MINUS, "-", startPos, startLine, startColumn);

            case '+':
                Advance();
                return new Token(TokenType.PLUS, "+", startPos, startLine, startColumn);
            case '*':
                Advance();
                return new Token(TokenType.STAR, "*", startPos, startLine, startColumn);
            case '/':
                Advance();
                return new Token(TokenType.SLASH, "/", startPos, startLine, startColumn);
            case '%':
                Advance();
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