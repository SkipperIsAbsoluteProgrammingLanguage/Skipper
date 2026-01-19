using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;

namespace Skipper.Cli;

public static class AstPrinter
{
    public static void Print(IReporter reporter, AstNode node, int indent = 0, bool isLast = true)
    {
        while (true)
        {
            var prefix = new string(' ', indent * 2);
            var pointer = isLast ? "└-" : "|-";

            switch (node)
            {
                case ProgramNode prog:
                    reporter.Line($"{prefix}{pointer} Program");
                    for (var i = 0; i < prog.Declarations.Count; i++)
                    {
                        Print(reporter, prog.Declarations[i], indent + 1, i == prog.Declarations.Count - 1);
                    }
                    break;

                case ClassDeclaration cls:
                    reporter.Line($"{prefix}{pointer} Class: {cls.Name}");
                    for (var i = 0; i < cls.Members.Count; i++)
                    {
                        Print(reporter, cls.Members[i], indent + 1, i == cls.Members.Count - 1);
                    }
                    break;

                case FunctionDeclaration fn:
                    reporter.Line($"{prefix}{pointer} Function: {fn.Name} -> {fn.ReturnType} {(fn.IsPublic ? "[public]" : "")}");
                    reporter.Line($"{prefix}  |- Parameters:");
                    for (var i = 0; i < fn.Parameters.Count; i++)
                    {
                        Print(reporter, fn.Parameters[i], indent + 2, i == fn.Parameters.Count - 1);
                    }
                    reporter.Line($"{prefix}  |- Body:");
                    for (var i = 0; i < fn.Body.Statements.Count; i++)
                    {
                        Print(reporter, fn.Body.Statements[i], indent + 2, i == fn.Body.Statements.Count - 1);
                    }
                    break;

                case ParameterDeclaration param:
                    reporter.Line($"{prefix}{pointer} {param.TypeName} {param.Name}");
                    break;

                case VariableDeclaration varDecl:
                    var init = varDecl.Initializer != null ? $" = {ExprToString(varDecl.Initializer)}" : "";
                    reporter.Line($"{prefix}{pointer} {varDecl.TypeName} {varDecl.Name}{init} {(varDecl.IsPublic ? "[public]" : "")}");
                    break;

                case BlockStatement block:
                    reporter.Line($"{prefix}{pointer} Block");
                    for (var i = 0; i < block.Statements.Count; i++)
                    {
                        Print(reporter, block.Statements[i], indent + 1, i == block.Statements.Count - 1);
                    }
                    break;

                case ExpressionStatement exprStmt:
                    reporter.Line($"{prefix}{pointer} ExpressionStatement: {ExprToString(exprStmt.Expression)}");
                    break;

                case ReturnStatement ret:
                    reporter.Line($"{prefix}{pointer} Return: {ExprToString(ret.Value)}");
                    break;

                case IfStatement ifStmt:
                    reporter.Line($"{prefix}{pointer} If: {ExprToString(ifStmt.Condition)}");
                    Print(reporter, ifStmt.ThenBranch, indent + 1, false);
                    if (ifStmt.ElseBranch != null)
                    {
                        node = ifStmt.ElseBranch;
                        indent += 1;
                        isLast = true;
                        continue;
                    }
                    break;

                case WhileStatement wh:
                    reporter.Line($"{prefix}{pointer} While: {ExprToString(wh.Condition)}");
                    node = wh.Body;
                    indent += 1;
                    isLast = true;
                    continue;

                case ForStatement f:
                    reporter.Line($"{prefix}{pointer} For:");
                    if (f.Initializer != null) Print(reporter, f.Initializer, indent + 1, false);
                    if (f.Condition != null) reporter.Line($"{prefix}  |- Condition: {ExprToString(f.Condition)}");
                    if (f.Increment != null) reporter.Line($"{prefix}  |- Increment: {ExprToString(f.Increment)}");
                    node = f.Body;
                    indent += 1;
                    isLast = true;
                    continue;

                default:
                    reporter.Line($"{prefix}{pointer} {node.NodeType}");
                    break;
            }

            break;
        }
    }

    private static string ExprToString(Expression? expr)
    {
        if (expr == null)
        {
            return "(null)";
        }

        return expr switch
        {
            IdentifierExpression id => id.Name,
            LiteralExpression lit => lit.Value.ToString() ?? "null",
            BinaryExpression bin => $"({ExprToString(bin.Left)} {bin.Operator.Text} {ExprToString(bin.Right)})",
            UnaryExpression un => $"({un.Operator.Text}{ExprToString(un.Operand)})",
            CallExpression call => $"{ExprToString(call.Callee)}({string.Join(", ", call.Arguments.ConvertAll(ExprToString))})",
            MemberAccessExpression mem => $"{ExprToString(mem.Object)}.{mem.MemberName}",
            ArrayAccessExpression arr => $"{ExprToString(arr.Target)}[{ExprToString(arr.Index)}]",
            NewArrayExpression na => $"new {na.ElementType}[{ExprToString(na.SizeExpression)}]",
            NewObjectExpression no => $"new {no.ClassName}({string.Join(", ", no.Arguments.ConvertAll(ExprToString))})",
            TernaryExpression ter => $"({ExprToString(ter.Condition)} ? {ExprToString(ter.ThenBranch)} : {ExprToString(ter.ElseBranch)})",
            _ => expr.NodeType.ToString()
        };
    }
}
