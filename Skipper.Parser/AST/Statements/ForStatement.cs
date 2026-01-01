using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Statements;

/// <summary>
/// Цикл for
/// </summary>
public sealed class ForStatement : Statement
{
    public Statement? Initializer { get; }
    public Expression? Condition { get; }
    public Expression? Increment { get; }
    public Statement Body { get; }

    public override AstNodeType NodeType => AstNodeType.ForStatement;

    public ForStatement(Statement? initializer, Expression? condition, Expression? increment, Statement body)
        : base(new Token(TokenType.KEYWORD_FOR, "for"))
    {
        Initializer = initializer;
        Condition = condition;
        Increment = increment;
        Body = body;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitForStatement(this);
    }
}