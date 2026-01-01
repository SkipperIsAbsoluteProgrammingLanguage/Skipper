using Skipper.Parser.AST;

namespace Skipper.Parser.Parser;

/// <summary>
/// Результат работы парсера
/// </summary>
public sealed class ParserResult
{
    public ProgramNode Root { get; }
    public IReadOnlyList<ParserDiagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(d => d.Level == ParserDiagnosticLevel.Error);

    public ParserResult(ProgramNode root, List<ParserDiagnostic> diagnostics)
    {
        Root = root;
        Diagnostics = diagnostics.AsReadOnly();
    }
}