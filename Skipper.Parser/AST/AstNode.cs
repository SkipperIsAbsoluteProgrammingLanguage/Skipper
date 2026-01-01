using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST;

/// <summary>
/// Базовый абстрактный класс для всех узлов AST
/// </summary>
public abstract class AstNode
{
    /// <summary>
    /// Тип узла
    /// </summary>
    public abstract AstNodeType NodeType { get; }

    /// <summary>
    /// Основной токен, ассоциированный с этим узлом
    /// </summary>
    public Token? Token { get; }

    protected AstNode(Token? token = null)
    {
        Token = token;
    }

    /// <summary>
    /// Метод для Visitor Pattern
    /// </summary>
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}