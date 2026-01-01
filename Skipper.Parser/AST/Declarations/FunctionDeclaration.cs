using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Statements;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST.Declarations;

/// <summary>
/// Объявление функции или метода
/// </summary>
public class FunctionDeclaration : Declaration
{
    public override string Name { get; }
    public string ReturnType { get; }
    public List<ParameterDeclaration> Parameters { get; }

    /// <summary>
    /// Тело функции (блок кода)
    /// </summary>
    public BlockStatement Body { get; }

    /// <summary>
    /// Флаг публичного доступа
    /// </summary>
    public bool IsPublic { get; }

    public override AstNodeType NodeType => AstNodeType.FunctionDeclaration;

    public FunctionDeclaration(string name, string returnType, List<ParameterDeclaration> parameters,
        BlockStatement body, bool isPublic)
        : base(new Token(TokenType.KEYWORD_FN, "fn"))
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
        Body = body;
        IsPublic = isPublic;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFunctionDeclaration(this);
    }
}