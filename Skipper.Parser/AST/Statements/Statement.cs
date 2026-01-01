using Skipper.Lexer.Tokens;

namespace Skipper.Parser.AST.Statements;

/// <summary>
/// Базовый класс для всех инструкций
/// </summary>
public abstract class Statement : AstNode
{
    protected Statement(Token? token) : base(token) { }
}