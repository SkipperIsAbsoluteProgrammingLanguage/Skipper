using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Statements;

/// <summary>
/// Блок инструкций, заключенный в фигурные скобки.
/// Вводит новую лексическую область видимости
/// </summary>
public sealed class BlockStatement : Statement
{
    public List<Statement> Statements { get; }

    public override AstNodeType NodeType => AstNodeType.BlockStatement;

    public BlockStatement(List<Statement> statements)
        : base(new Token(TokenType.BRACE_OPEN, "{"))
    {
        Statements = statements;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitBlockStatement(this);
    }
}