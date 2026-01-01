namespace Skipper.Parser.AST;

/// <summary>
/// Перечисление всех типов узлов AST.
/// Используется для быстрой идентификации типа узла без рефлексии
/// </summary>
public enum AstNodeType
{
    // Корневой узел
    Program,

    // Объявления (Declarations)
    FunctionDeclaration,
    VariableDeclaration,
    ClassDeclaration,
    ParameterDeclaration,

    // Инструкции (Statements)
    BlockStatement,
    IfStatement,
    WhileStatement,
    ForStatement,
    ReturnStatement,
    ExpressionStatement,

    // Выражения (Expressions)
    BinaryExpression,
    UnaryExpression,
    LiteralExpression,
    IdentifierExpression,
    CallExpression,
    ArrayAccessExpression,
    MemberAccessExpression,
    NewArrayExpression,
    NewObjectExpression,
    TernaryExpression
}