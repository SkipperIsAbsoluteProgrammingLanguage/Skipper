using Skipper.Lexer.Tokens;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Declarations;

/// <summary>
/// Объявление параметра функции
/// </summary>
public class ParameterDeclaration : Declaration
{
    public string TypeName { get; }
    public override string Name { get; }

    public override AstNodeType NodeType => AstNodeType.ParameterDeclaration;

    public ParameterDeclaration(string typeName, string name)
        : base(new Token(TokenType.IDENTIFIER, name))
    {
        TypeName = typeName;
        Name = name;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitParameterDeclaration(this);
    }
}