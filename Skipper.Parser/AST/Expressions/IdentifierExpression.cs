using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел идентификатора (использование переменной)
/// </summary>
public sealed class IdentifierExpression : Expression
{
    public string Name { get; }

    public override AstNodeType NodeType => AstNodeType.IdentifierExpression;

    public IdentifierExpression(Token token)
        : base(token)
    {
        if (token.Type != TokenType.IDENTIFIER)
        {
            throw new ArgumentException($"Expected IDENTIFIER token, got {token.Type}");
        }

        Name = token.Text;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitIdentifierExpression(this);
    }
}