using Skipper.Lexer.Tokens;

namespace Skipper.Parser.Parser;

/// <summary>
/// Диагностическое сообщение парсера
/// </summary>
public class ParserDiagnostic
{
    public ParserDiagnosticLevel Level { get; }
    public string Message { get; }
    public Token? Token { get; }

    public ParserDiagnostic(ParserDiagnosticLevel level, string message, Token? token = null)
    {
        Level = level;
        Message = message;
        Token = token;
    }

    public override string ToString()
    {
        var location = "";
        if (Token != null)
        {
            if (Token.Type == TokenType.EOF)
            {
                location = " at end of file";
            }
            else
            {
                location = $" at line {Token.Line}, column {Token.Column} ('{Token.Text}')";
            }
        }

        return $"{Level}: {Message}{location}";
    }
}