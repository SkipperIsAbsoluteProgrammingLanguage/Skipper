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

    public Token(TokenType type, string text, int startPosition = 0, int line = 1, int column = 1)
    {
        Type = type;
        Text = text;
        StartPosition = startPosition;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Переопределение ToString для удобства отладки
    /// </summary>
    public override string ToString() => $"Token({Type}, '{Text}' at {Line}:{Column})";

    /// <summary>
    /// Проверяет, является ли токен указанного типа
    /// </summary>s
    public bool Is(TokenType type) => Type == type;

    /// <summary>
    /// Проверяет, является ли токен любым из указанных типов
    /// </summary>
    public bool IsAny(params TokenType[] types) => types.Contains(Type);

    /// <summary>
    /// Проверяет, является ли токен ключевым словом
    /// </summary>
    public bool IsKeyword => Type.ToString().StartsWith("KEYWORD_");

    /// <summary>
    /// Проверяет, является ли токен литералом
    /// </summary>
    public bool IsLiteral => Type is
        TokenType.NUMBER or
        TokenType.FLOAT_LITERAL or
        TokenType.CHAR_LITERAL or
        TokenType.STRING_LITERAL or
        TokenType.BOOL_LITERAL;

    /// <summary>
    /// Проверяет, является ли токен оператором
    /// </summary>
    public bool IsOperator => Type is
        TokenType.PLUS or
        TokenType.MINUS or
        TokenType.STAR or
        TokenType.SLASH or
        TokenType.ASSIGN or
        TokenType.EQUAL or
        TokenType.NOT_EQUAL or
        TokenType.LESS or
        TokenType.GREATER or
        TokenType.LESS_EQUAL or
        TokenType.GREATER_EQUAL or
        TokenType.AND or
        TokenType.OR or
        TokenType.NOT;

    /// <summary>
    /// Получает числовое значение для числовых токенов
    /// </summary>
    public object GetNumericValue()
    {
        if (Type == TokenType.NUMBER)
        {
            if (long.TryParse(Text, out var intValue))
            {
                return intValue;
            }
        }
        else if (Type == TokenType.FLOAT_LITERAL)
        {
            if (double.TryParse(Text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
            {
                return floatValue;
            }
        }

        throw new InvalidOperationException($"Token {Type} is not a numeric literal");
    }

    /// <summary>
    /// Получает булево значение для булевых токенов
    /// </summary>
    public bool GetBoolValue()
    {
        if (Type != TokenType.BOOL_LITERAL)
        {
            throw new InvalidOperationException($"Token {Type} is not a boolean literal");
        }

        return Text == "true";
    }

    /// <summary>
    /// Получает строковое значение для строковых и символьных токенов
    /// </summary>
    public string GetStringValue()
    {
        if (Type != TokenType.STRING_LITERAL && Type != TokenType.CHAR_LITERAL)
        {
            throw new InvalidOperationException($"Token {Type} is not a string or character literal");
        }

        var content = Text;

        // Убираем кавычки
        if (Type == TokenType.STRING_LITERAL && content.Length >= 2 ||
            Type == TokenType.CHAR_LITERAL && content.Length >= 2)
        {
            content = content.Substring(1, content.Length - 2);
        }

        // Обработка escape-последовательностей будет в лексере
        return content;
    }
}