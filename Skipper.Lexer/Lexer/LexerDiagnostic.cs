namespace Skipper.Lexer.Lexer;

/// <summary>
/// Диагностическое сообщение лексера
/// </summary>
public sealed class LexerDiagnostic
{
    public LexerDiagnosticLevel Level { get; }
    public string Message { get; }
    public int Line { get; }
    public int Column { get; }

    public LexerDiagnostic(LexerDiagnosticLevel level, string message, int line, int column)
    {
        Level = level;
        Message = message;
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        var levelStr = Level switch
        {
            LexerDiagnosticLevel.Error => "Ошибка",
            LexerDiagnosticLevel.Warning => "Предупреждение",
            _ => "Информация"
        };
        return $"{levelStr}: {Message} (строка {Line}, столбец {Column})";
    }
}