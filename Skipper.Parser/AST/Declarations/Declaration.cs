using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Statements;

namespace Skipper.Parser.AST.Declarations;

/// <summary>
/// Базовый класс для всех объявлений
/// </summary>
public abstract class Declaration : Statement
{
    /// <summary>
    /// Имя объявляемой сущности
    /// </summary>
    public abstract string Name { get; }

    protected Declaration(Token token) : base(token) { }
}