using Skipper.Parser.AST;

namespace Skipper.Parser.Parser;

// Результат работы парсера. Содержит построенное AST (если удалось)
// и список диагностических сообщений.
public class ParserResult
{
    // Корневой узел абстрактного синтаксического дерева.
    // Может быть неполным или пустым, если возникли критические ошибки.
    public ProgramNode Root { get; }

    // Список ошибок и предупреждений.
    public List<ParserDiagnostic> Diagnostics { get; }
    
    // Указывает, успешно ли прошел парсинг (отсутствуют ошибки уровня Error).
    public bool IsSuccess => !Diagnostics.Any(d => d.Level == ParserDiagnosticLevel.Error);

    public ParserResult(ProgramNode root, List<ParserDiagnostic> diagnostics)
    {
        Root = root;
        Diagnostics = diagnostics ?? [];
    }
}