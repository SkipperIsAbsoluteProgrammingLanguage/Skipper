using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел доступа к элементу массива
/// </summary>
public class ArrayAccessExpression : Expression
{
    public Expression Target { get; }
    public Expression Index { get; }

    public override AstNodeType NodeType => AstNodeType.ArrayAccessExpression;

    public ArrayAccessExpression(Expression target, Expression index)
        : base(target.Token)
    {
        Target = target;
        Index = index;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitArrayAccessExpression(this);
    }
}