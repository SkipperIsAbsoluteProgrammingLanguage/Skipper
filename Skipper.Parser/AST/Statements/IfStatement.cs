using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Statements;

/// <summary>
/// Условная инструкция
/// </summary>
public class IfStatement : Statement
{
    public Expression Condition { get; }
    public Statement ThenBranch { get; }
    public Statement? ElseBranch { get; }

    public override AstNodeType NodeType => AstNodeType.IfStatement;

    public IfStatement(Expression condition, Statement thenBranch, Statement? elseBranch = null)
        : base(new Token(TokenType.KEYWORD_IF, "if"))
    {
        Condition = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitIfStatement(this);
    }
}