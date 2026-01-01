using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел литерала (константы)
/// </summary>
public class LiteralExpression : Expression
{
    public object Value { get; }

    public override AstNodeType NodeType => AstNodeType.LiteralExpression;

    public LiteralExpression(object value, Token token)
        : base(token)
    {
        Value = value;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitLiteralExpression(this);
    }
}