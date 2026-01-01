using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Statements;

/// <summary>
/// Инструкция возврата из функции
/// </summary>
public class ReturnStatement : Statement
{
    public Expression? Value { get; }

    public override AstNodeType NodeType => AstNodeType.ReturnStatement;

    public ReturnStatement(Expression? value)
        : base(new Token(TokenType.KEYWORD_RETURN, "return"))
    {
        Value = value;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitReturnStatement(this);
    }
}