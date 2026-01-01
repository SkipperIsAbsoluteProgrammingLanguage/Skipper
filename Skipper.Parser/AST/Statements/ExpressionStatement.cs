using Skipper.Parser.AST.Expressions;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Statements;

/// <summary>
/// Инструкция-выражение
/// Обертка над Expression, позволяющая использовать его как Statement.
/// Обычно используется для выражений с побочными эффектами (присваивание, вызов функции)
/// </summary>
public sealed class ExpressionStatement : Statement
{
    public Expression Expression { get; }

    public override AstNodeType NodeType => AstNodeType.ExpressionStatement;

    public ExpressionStatement(Expression expression)
        : base(expression.Token)
    {
        Expression = expression;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitExpressionStatement(this);
    }
}