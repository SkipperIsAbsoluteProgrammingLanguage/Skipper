namespace Skipper.Lexer.Lexer;

/// <summary>
/// Исключение лексера с информацией о позиции
/// </summary>
public class LexerException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public LexerException(string message, int line, int column)
        : base($"{message} на строке {line}, столбец {column}")
    {
        Line = line;
        Column = column;
    }
}