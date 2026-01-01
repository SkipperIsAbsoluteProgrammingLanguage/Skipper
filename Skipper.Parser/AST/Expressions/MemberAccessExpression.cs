using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел доступа к члену класса
/// </summary>
public sealed class MemberAccessExpression : Expression
{
    public Expression Object { get; }
    public string MemberName { get; }

    public override AstNodeType NodeType => AstNodeType.MemberAccessExpression;

    public MemberAccessExpression(Expression obj, string memberName)
        : base(obj.Token)
    {
        Object = obj;
        MemberName = memberName;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitMemberAccessExpression(this);
    }
}