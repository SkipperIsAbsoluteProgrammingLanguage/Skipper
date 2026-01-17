using System.Globalization;

namespace Skipper.Lexer.Tokens;

/// <summary>
/// Контейнер для токена
/// </summary>
public sealed class Token
{
    /// <summary>
    /// Тип токена
    /// </summary>
    public TokenType Type { get; }

    /// <summary>
    /// Исходный текст токена
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Позиция начала токена в исходном коде
    /// </summary>
    public int StartPosition { get; }

    /// <summary>
    /// Длина токена в символах
    /// </summary>
    public int Length => Text.Length;

    /// <summary>
    /// Позиция конца токена (исключающая)
    /// </summary>
    public int EndPosition => StartPosition + Length;

    /// <summary>
    /// Номер строки в исходном коде (начинается с 1)
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Номер столбца в исходном коде (начинается с 1)
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Значение для строковых и символьных литералов
    /// </summary>
    private readonly object? _value;

    public Token(TokenType type, string text, int startPosition = 0, int line = 1, int column = 1)
    {
        Type = type;
        Text = text;
        StartPosition = startPosition;
        Line = line;
        Column = column;
    }

    public Token(TokenType type, object value, string text, int startPosition = 0, int line = 1, int column = 1)
        : this(type, text, startPosition, line, column)
    {
        _value = value;
    }

    public override string ToString() => $"Token({Type}, '{Text}' at {Line}:{Column})";

    public bool Is(TokenType type) => Type == type;

    public bool IsAny(params TokenType[] types) => types.Contains(Type);

    public bool IsKeyword => Type.ToString().StartsWith("KEYWORD_");

    public bool IsLiteral => Type is
        TokenType.NUMBER or
        TokenType.DOUBLE_LITERAL or
        TokenType.CHAR_LITERAL or
        TokenType.STRING_LITERAL or
        TokenType.BOOL_LITERAL;

    public bool IsOperator => Type is
        TokenType.PLUS or
        TokenType.INCREMENT or
        TokenType.PLUS_ASSIGN or
        TokenType.MINUS or
        TokenType.DECREMENT or
        TokenType.MINUS_ASSIGN or
        TokenType.STAR or
        TokenType.STAR_ASSIGN or
        TokenType.SLASH or
        TokenType.SLASH_ASSIGN or
        TokenType.ASSIGN or
        TokenType.EQUAL or
        TokenType.NOT_EQUAL or
        TokenType.LESS or
        TokenType.GREATER or
        TokenType.LESS_EQUAL or
        TokenType.GREATER_EQUAL or
        TokenType.AND or
        TokenType.OR or
        TokenType.NOT or
        TokenType.MODULO or
        TokenType.MODULO_ASSIGN;

    public long GetNumericValue()
    {
        if (Type == TokenType.NUMBER && long.TryParse(Text, out var longValue))
        {
            return longValue;
        }

        throw new InvalidOperationException($"Token {Type} is not a numeric literal");
    }

    public double GetDoubleValue()
    {
        if (Type == TokenType.DOUBLE_LITERAL &&
            double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }

        throw new InvalidOperationException($"Token {Type} is not a double literal");
    }

    public bool GetBoolValue()
    {
        if (Type != TokenType.BOOL_LITERAL)
        {
            throw new InvalidOperationException($"Token {Type} is not a boolean literal");
        }

        return Text == "true";
    }

    public string GetStringValue()
    {
        if (Type != TokenType.STRING_LITERAL || _value is not string strValue)
        {
            throw new InvalidOperationException($"Token {Type} is not a string literal");
        }

        return strValue;
    }

    public char GetCharValue()
    {
        if (Type != TokenType.CHAR_LITERAL || _value is not char chrValue)
        {
            throw new InvalidOperationException($"Token {Type} is not a character literal");
        }

        return chrValue;
    }
}
