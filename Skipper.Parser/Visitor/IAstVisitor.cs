using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;

namespace Skipper.Parser.Visitor;

/// <summary>
/// Интерфейс посетителя AST (Visitor Pattern). 
/// Позволяет реализовать операции над деревом (интерпретация, компиляция, принтинг) без изменения классов узлов
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAstVisitor<out T>
{
    // Root
    T VisitProgram(ProgramNode node);

    // Declarations
    T VisitFunctionDeclaration(FunctionDeclaration node);
    T VisitVariableDeclaration(VariableDeclaration node);
    T VisitClassDeclaration(ClassDeclaration node);
    T VisitParameterDeclaration(ParameterDeclaration node);

    // Statements
    T VisitBlockStatement(BlockStatement node);
    T VisitIfStatement(IfStatement node);
    T VisitWhileStatement(WhileStatement node);
    T VisitForStatement(ForStatement node);
    T VisitReturnStatement(ReturnStatement node);
    T VisitExpressionStatement(ExpressionStatement node);

    // Expressions
    T VisitBinaryExpression(BinaryExpression node);
    T VisitUnaryExpression(UnaryExpression node);
    T VisitLiteralExpression(LiteralExpression node);
    T VisitIdentifierExpression(IdentifierExpression node);
    T VisitCallExpression(CallExpression node);
    T VisitTernaryExpression(TernaryExpression node);

    // Access
    T VisitArrayAccessExpression(ArrayAccessExpression node);
    T VisitMemberAccessExpression(MemberAccessExpression node);

    // New
    T VisitNewArrayExpression(NewArrayExpression node);
    T VisitNewObjectExpression(NewObjectExpression node);
}