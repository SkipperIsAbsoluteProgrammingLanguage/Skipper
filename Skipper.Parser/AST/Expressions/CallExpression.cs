using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел вызова функции
/// </summary>
public class CallExpression : Expression
{
    public Expression Callee { get; }
    public List<Expression> Arguments { get; }

    public override AstNodeType NodeType => AstNodeType.CallExpression;

    public CallExpression(Expression callee, List<Expression> arguments)
        : base(callee.Token)
    {
        Callee = callee;
        Arguments = arguments;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitCallExpression(this);
    }
}