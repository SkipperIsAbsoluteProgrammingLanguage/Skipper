using Skipper.Lexer.Tokens;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Базовый класс для всех выражений
/// </summary>
public abstract class Expression : AstNode
{
    protected Expression(Token? token) : base(token) { }
}