using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел создания нового экземпляра класса
/// </summary>
public sealed class NewObjectExpression : Expression
{
    public string ClassName { get; }
    public List<Expression> Arguments { get; }

    public override AstNodeType NodeType => AstNodeType.NewObjectExpression;

    public NewObjectExpression(string className, List<Expression> arguments)
        : base(new Token(TokenType.KEYWORD_NEW, "new"))
    {
        ClassName = className;
        Arguments = arguments;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitNewObjectExpression(this);
    }
}