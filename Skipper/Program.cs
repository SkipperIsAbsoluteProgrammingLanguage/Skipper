using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Writer;
using Skipper.Lexer.Lexer;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Skipper.Parser.Parser;
using Skipper.Semantic;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Interpreter;
using Skipper.VM.Jit;

Header("Skipper Compiler");

if (args.Length == 0)
{
    Console.WriteLine("Usage: Skipper <file.sk> [--jit [N]] [--trace]");
    return 1;
}

var useJit = false;
var jitThreshold = 50;
var trace = false;

var path = args[0];
for (var i = 1; i < args.Length; i++)
{
    var arg = args[i];
    if (arg == "--jit")
    {
        useJit = true;
        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var threshold))
        {
            jitThreshold = threshold;
            i++;
        }

        continue;
    }

    if (arg == "--trace")
    {
        trace = true;
        continue;
    }

    Console.WriteLine($"Unknown argument: {arg}");
    Console.WriteLine("Usage: Skipper <file.sk> [--jit [N]] [--trace]");
    return 1;
}

if (string.IsNullOrWhiteSpace(path))
{
    Console.WriteLine("Usage: Skipper <file.sk> [--jit [N]] [--trace]");
    return 1;
}

if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {path}");
    return 1;
}

var code = File.ReadAllText(path);

// ======================
// Lexer
// ======================
Section("Lexer");

var lexer = new Lexer(code);
var lexerResult = lexer.TokenizeWithDiagnostics();

if (lexerResult.HasErrors)
{
    Console.WriteLine("[FAIL] Diagnostics:");
    foreach (var diag in lexerResult.Diagnostics)
    {
        Indent(1, $"- {diag}");
    }
}
else
{
    Console.WriteLine("[ OK ] No lexer errors");
}

var tokens = lexerResult.Tokens;
Console.WriteLine($"\nTokens ({tokens.Count}):");
for (var i = 0; i < tokens.Count; i++)
{
    Indent(1, $"[{i}] {tokens[i]}");
}

// ======================
// Parser
// ======================
Section("Parser");

var parser = new Parser(tokens);
var parserResult = parser.Parse();

if (parserResult.HasErrors)
{
    Console.WriteLine("[FAIL] Diagnostics:");
    foreach (var diag in parserResult.Diagnostics)
    {
        Indent(1, $"- {diag}");
    }
}
else
{
    Console.WriteLine("[ OK ] No parser errors");
}

Console.WriteLine("\n[ OK ] AST:");
PrintAst(parserResult.Root);

// ======================
// Semantic analysis
// ======================
Section("Semantic analysis");

var semantic = new SemanticAnalyzer();
semantic.VisitProgram(parserResult.Root);

if (semantic.HasErrors)
{
    Console.WriteLine("[FAIL] Diagnostics:");
    foreach (var diag in semantic.Diagnostics)
    {
        Indent(1, $"- {diag}");
    }
}
else
{
    Console.WriteLine("[ OK ] No semantic errors");
}

if (lexerResult.HasErrors || parserResult.HasErrors || semantic.HasErrors)
{
    Header("[FAIL] Compilation failed");
    return 2;
}

// ======================
// Bytecode generation
// ======================
Section("Bytecode");

var bytecodeGenerator = new BytecodeGenerator();
var bytecodeProgram = bytecodeGenerator.Generate(parserResult.Root);

var bytecodePath = Path.ChangeExtension(path, ".json");
var writer = new BytecodeWriter(bytecodeProgram);
writer.SaveToFile(bytecodePath);

Console.WriteLine($"[ OK ] Bytecode saved: {bytecodePath}");

// ======================
// VM execution
// ======================
Section("VM");

var runtime = new RuntimeContext();
var result = useJit
    ? RunJit(bytecodeProgram, runtime, jitThreshold, trace)
    : new VirtualMachine(bytecodeProgram, runtime, trace).Run("main");

Console.WriteLine($"[ OK ] Program result: {result}");

Header("[ OK ] Compilation finished successfully");
return 0;


static Value RunJit(BytecodeProgram program, RuntimeContext runtime, int threshold, bool trace)
{
    var jitVm = new JitVirtualMachine(program, runtime, threshold, trace);
    var result = jitVm.Run("main");
    Console.WriteLine($"[ OK ] JIT compiled functions: {jitVm.JittedFunctionCount}");
    return result;
}

static void Header(string title)
{
    Console.WriteLine();
    Console.WriteLine("==============================");
    Console.WriteLine(title);
    Console.WriteLine("==============================");
}

static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine("──────────────────────────────");
    Console.WriteLine(title);
    Console.WriteLine("──────────────────────────────");
}

static void Indent(int level, string text)
{
    Console.WriteLine($"{new string(' ', level * 2)}{text}");
}

static void PrintAst(AstNode node, int indent = 0, bool isLast = true)
{
    while (true)
    {
        var prefix = new string(' ', indent * 2);
        var pointer = isLast ? "└─" : "├─";

        switch (node)
        {
            case ProgramNode prog:
                Console.WriteLine($"{prefix}{pointer} Program");
                for (var i = 0; i < prog.Declarations.Count; i++)
                    PrintAst(prog.Declarations[i], indent + 1, i == prog.Declarations.Count - 1);
                break;

            case ClassDeclaration cls:
                Console.WriteLine($"{prefix}{pointer} Class: {cls.Name}");
                for (var i = 0; i < cls.Members.Count; i++)
                    PrintAst(cls.Members[i], indent + 1, i == cls.Members.Count - 1);
                break;

            case FunctionDeclaration fn:
                Console.WriteLine(
                    $"{prefix}{pointer} Function: {fn.Name} -> {fn.ReturnType} {(fn.IsPublic ? "[public]" : "")}");
                // Parameters
                Console.WriteLine($"{prefix}  ├─ Parameters:");
                for (var i = 0; i < fn.Parameters.Count; i++)
                    PrintAst(fn.Parameters[i], indent + 2, i == fn.Parameters.Count - 1);
                // Body
                Console.WriteLine($"{prefix}  └─ Body:");
                for (var i = 0; i < fn.Body.Statements.Count; i++)
                    PrintAst(fn.Body.Statements[i], indent + 2, i == fn.Body.Statements.Count - 1);
                break;

            case ParameterDeclaration param:
                Console.WriteLine($"{prefix}{pointer} {param.TypeName} {param.Name}");
                break;

            case VariableDeclaration varDecl:
                var init = varDecl.Initializer != null ? $" = {ExprToString(varDecl.Initializer)}" : "";
                Console.WriteLine(
                    $"{prefix}{pointer} {varDecl.TypeName} {varDecl.Name}{init} {(varDecl.IsPublic ? "[public]" : "")}");
                break;

            case BlockStatement block:
                Console.WriteLine($"{prefix}{pointer} Block");
                for (var i = 0; i < block.Statements.Count; i++)
                    PrintAst(block.Statements[i], indent + 1, i == block.Statements.Count - 1);
                break;

            case ExpressionStatement exprStmt:
                Console.WriteLine($"{prefix}{pointer} ExpressionStatement: {ExprToString(exprStmt.Expression)}");
                break;

            case ReturnStatement ret:
                Console.WriteLine($"{prefix}{pointer} Return: {ExprToString(ret.Value)}");
                break;

            case IfStatement ifStmt:
                Console.WriteLine($"{prefix}{pointer} If: {ExprToString(ifStmt.Condition)}");
                PrintAst(ifStmt.ThenBranch, indent + 1, false);
                if (ifStmt.ElseBranch != null)
                {
                    node = ifStmt.ElseBranch;
                    indent += 1;
                    isLast = true;
                    continue;
                }

                break;

            case WhileStatement wh:
                Console.WriteLine($"{prefix}{pointer} While: {ExprToString(wh.Condition)}");
                node = wh.Body;
                indent += 1;
                isLast = true;
                continue;

            case ForStatement f:
                Console.WriteLine($"{prefix}{pointer} For:");
                if (f.Initializer != null) PrintAst(f.Initializer, indent + 1, false);
                if (f.Condition != null) Console.WriteLine($"{prefix}  ├─ Condition: {ExprToString(f.Condition)}");
                if (f.Increment != null) Console.WriteLine($"{prefix}  ├─ Increment: {ExprToString(f.Increment)}");
                node = f.Body;
                indent += 1;
                isLast = true;
                continue;

            default:
                Console.WriteLine($"{prefix}{pointer} {node.NodeType}");
                break;
        }

        break;
    }
}

static string ExprToString(Expression? expr)
{
    if (expr == null)
    {
        return "(null)";
    }

    return expr switch
    {
        IdentifierExpression id
            => id.Name,
        LiteralExpression lit
            => lit.Value.ToString() ?? "null",
        BinaryExpression bin
            => $"({ExprToString(bin.Left)} {bin.Operator.Text} {ExprToString(bin.Right)})",
        UnaryExpression un
            => $"({un.Operator.Text}{ExprToString(un.Operand)})",
        CallExpression call
            => $"{ExprToString(call.Callee)}({string.Join(", ", call.Arguments.ConvertAll(ExprToString))})",
        MemberAccessExpression mem
            => $"{ExprToString(mem.Object)}.{mem.MemberName}",
        ArrayAccessExpression arr
            => $"{ExprToString(arr.Target)}[{ExprToString(arr.Index)}]",
        NewArrayExpression na
            => $"new {na.ElementType}[{ExprToString(na.SizeExpression)}]",
        NewObjectExpression no
            => $"new {no.ClassName}({string.Join(", ", no.Arguments.ConvertAll(ExprToString))})",
        TernaryExpression ter
            => $"({ExprToString(ter.Condition)} ? {ExprToString(ter.ThenBranch)} : {ExprToString(ter.ElseBranch)})",
        _ => expr.NodeType.ToString()
    };
}
