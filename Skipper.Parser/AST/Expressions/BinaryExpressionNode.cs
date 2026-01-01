using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел бинарного выражения
/// </summary>
public class BinaryExpression : Expression
{
    public Expression Left { get; }
    public Token Operator { get; }
    public Expression Right { get; }

    public override AstNodeType NodeType => AstNodeType.BinaryExpression;

    public BinaryExpression(Expression left, Token op, Expression right)
        : base(op)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitBinaryExpression(this);
    }
}