using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Expressions;

/// <summary>
/// Узел создания нового массива
/// </summary>
public class NewArrayExpression : Expression
{
    public string ElementType { get; }
    public Expression SizeExpression { get; }

    public override AstNodeType NodeType => AstNodeType.NewArrayExpression;

    public NewArrayExpression(string elementType, Expression sizeExpression)
        : base(new Token(TokenType.KEYWORD_NEW, "new"))
    {
        ElementType = elementType;
        SizeExpression = sizeExpression;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitNewArrayExpression(this);
    }
}