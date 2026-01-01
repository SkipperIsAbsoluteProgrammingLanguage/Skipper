using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Declarations;

/// <summary>
/// Объявление переменной или поля класса
/// </summary>
public sealed class VariableDeclaration : Declaration
{
    public override string Name { get; }
    public string TypeName { get; }
    public Expression? Initializer { get; }
    public bool IsPublic { get; }

    public override AstNodeType NodeType => AstNodeType.VariableDeclaration;

    public VariableDeclaration(string typeName, string name, Expression? initializer, bool isPublic = false)
        : base(new Token(TokenType.IDENTIFIER, name))
    {
        TypeName = typeName;
        Name = name;
        Initializer = initializer;
        IsPublic = isPublic;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitVariableDeclaration(this);
    }
}