using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел тернарного оператора
/// </summary>
public sealed class TernaryExpression : Expression
{
    public Expression Condition { get; }
    public Expression ThenBranch { get; }
    public Expression ElseBranch { get; }

    public override AstNodeType NodeType => AstNodeType.TernaryExpression;

    public TernaryExpression(Expression condition, Expression thenBranch, Expression elseBranch, Token questionMark)
        : base(questionMark)
    {
        Condition = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitTernaryExpression(this);
    }
}