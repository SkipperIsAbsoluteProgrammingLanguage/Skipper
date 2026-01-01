namespace Skipper.Lexer.Lexer;

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