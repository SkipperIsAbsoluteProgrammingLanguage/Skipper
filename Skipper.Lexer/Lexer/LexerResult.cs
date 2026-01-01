using Skipper.Lexer.Tokens;

namespace Skipper.Lexer.Lexer;

/// <summary>
/// Результат работы лексера
/// </summary>
public sealed class LexerResult
{
    public IReadOnlyList<Token> Tokens { get; }
    public IReadOnlyList<LexerDiagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(d => d.Level == LexerDiagnosticLevel.Error);

    public LexerResult(List<Token> tokens, List<LexerDiagnostic> diagnostics)
    {
        Tokens = tokens.AsReadOnly();
        Diagnostics = diagnostics.AsReadOnly();
    }
}