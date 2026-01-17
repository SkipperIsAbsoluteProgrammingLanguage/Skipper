using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел унарного выражения (Operator Operand)
/// </summary>
public sealed class UnaryExpression : Expression
{
    public Token Operator { get; }
    public Expression Operand { get; }
    public bool IsPostfix { get; }

    public override AstNodeType NodeType => AstNodeType.UnaryExpression;

    public UnaryExpression(Token op, Expression operand, bool isPostfix = false) : base(op)
    {
        Operator = op;
        Operand = operand;
        IsPostfix = isPostfix;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitUnaryExpression(this);
    }
}
