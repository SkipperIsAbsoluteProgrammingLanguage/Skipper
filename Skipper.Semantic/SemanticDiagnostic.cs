using Skipper.Lexer.Tokens;

namespace Skipper.Semantic;

public sealed class SemanticDiagnostic
{
    public SemanticDiagnosticLevel Level { get; }
    public string Message { get; }
    public Token? Token { get; }

    public SemanticDiagnostic(
        SemanticDiagnosticLevel level,
        string message,
        Token? token = null)
    {
        Level = level;
        Message = message;
        Token = token;
    }

    public override string ToString()
    {
        if (Token == null)
        {
            return $"{Level}: {Message}";
        }

        return $"{Level}: {Message} at {Token.Line}:{Token.Column}";
    }
}