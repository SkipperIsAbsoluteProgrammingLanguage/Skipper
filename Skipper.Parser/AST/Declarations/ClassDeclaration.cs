using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Declarations;

/// <summary>
/// Объявление класса
/// </summary>
public sealed class ClassDeclaration : Declaration
{
    public override string Name { get; }
    public List<Declaration> Members { get; }

    public override AstNodeType NodeType => AstNodeType.ClassDeclaration;

    public ClassDeclaration(string name, List<Declaration> members)
        : base(new Token(TokenType.KEYWORD_CLASS, "class"))
    {
        Name = name;
        Members = members;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitClassDeclaration(this);
    }
}