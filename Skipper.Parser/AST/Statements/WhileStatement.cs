using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Statements;

/// <summary>
/// Цикл while
/// </summary>
public sealed class WhileStatement : Statement
{
    public Expression Condition { get; }
    public Statement Body { get; }

    public override AstNodeType NodeType => AstNodeType.WhileStatement;

    public WhileStatement(Expression condition, Statement body)
        : base(new Token(TokenType.KEYWORD_WHILE, "while"))
    {
        Condition = condition;
        Body = body;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitWhileStatement(this);
    }
}