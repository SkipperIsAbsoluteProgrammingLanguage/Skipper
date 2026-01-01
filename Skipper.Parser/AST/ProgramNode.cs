using Skipper.Parser.AST.Declarations;
using Skipper.Parser.Visitor;

namespace Skipper.Parser.AST;

/// <summary>
/// Корневой узел AST.
/// Представляет собой результат парсинга всей программы
/// </summary>
public sealed class ProgramNode : AstNode
{
    /// <summary>
    /// Список объявлений верхнего уровня (Top-Level Declarations)
    /// </summary>
    public List<Declaration> Declarations { get; }

    public override AstNodeType NodeType => AstNodeType.Program;

    public ProgramNode(List<Declaration> declarations)
    {
        Declarations = declarations;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitProgram(this);
    }
}