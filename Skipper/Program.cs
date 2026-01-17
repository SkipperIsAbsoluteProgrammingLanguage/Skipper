using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Writer;
using Skipper.Lexer.Lexer;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Skipper.Parser.Parser;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.Semantic;
using Skipper.VM.Interpreter;
using Skipper.VM.Jit;
using System.Diagnostics;

Header("Skipper Compiler");

if (args.Length == 0)
{
    Console.WriteLine("Использование: Skipper <file.sk> [--jit [порог]] [--trace] [--mem MB]");
    return 1;
}

var useJit = false;
var jitThreshold = 50;
var trace = false;
var memMb = 64;

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

    if (arg == "--mem")
    {
        if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out var memValue) || memValue <= 0)
        {
            Console.WriteLine("Неверное значение --mem. Ожидается целое число (МБ).");
            return 1;
        }
        memMb = memValue;
        i++;
        continue;
    }

    Console.WriteLine($"Неизвестный аргумент: {arg}");
    return 1;
}

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не найден: {path}");
    return 1;
}

var code = File.ReadAllText(path);

// ======================
// Лексер
// ======================
Section("Лексер");

var lexer = new Lexer(code);
LexerResult lexerResult = lexer.TokenizeWithDiagnostics();

if (lexerResult.HasErrors)
{
    Console.WriteLine("[FAIL] Ошибки лексера:");
    foreach (var diag in lexerResult.Diagnostics) Indent(1, $"- {diag}");
    return 2;
}

Console.WriteLine($"[ OK ] Токенов: {lexerResult.Tokens.Count}");

// ======================
// Парсер
// ======================
Section("Парсер");

var parser = new Parser(lexerResult.Tokens);
ParserResult parserResult = parser.Parse();

if (parserResult.HasErrors)
{
    Console.WriteLine("[FAIL] Ошибки парсера:");
    foreach (var diag in parserResult.Diagnostics) Indent(1, $"- {diag}");
    return 2;
}

Console.WriteLine("[ OK ] AST построен:");
PrintAst(parserResult.Root);

// ======================
// Семантика
// ======================
Section("Семантика");

var semantic = new SemanticAnalyzer();
semantic.VisitProgram(parserResult.Root);

if (semantic.HasErrors)
{
    Console.WriteLine("[FAIL] Ошибки семантики:");
    foreach (var diag in semantic.Diagnostics) Indent(1, $"- {diag}");
    return 2;
}

Console.WriteLine("[ OK ] Семантических ошибок нет");

// ======================
// Генерация
// ======================
Section("Байткод");

var generator = new BytecodeGenerator();
BytecodeProgram program = generator.Generate(parserResult.Root);

Console.WriteLine($"Функций: {program.Functions.Count}");
Console.WriteLine($"Констант: {program.ConstantPool.Count}");
Console.WriteLine($"Классов: {program.Classes.Count}");

var bytecodePath = Path.ChangeExtension(path, ".json");
new BytecodeWriter(program).SaveToFile(bytecodePath);
Console.WriteLine($"Сохранено: {bytecodePath}");

// ======================
// Выполнение (VM / Hybrid)
// ======================
Section("Выполнение");

Console.WriteLine(useJit 
    ? $"Режим: Hybrid JIT (порог: {jitThreshold})" 
    : "Режим: Интерпретатор");

if (trace) Console.WriteLine("Трассировка: ВКЛ");

try
{
    var runtime = new RuntimeContext(); 
    
    var sw = Stopwatch.StartNew();
    Value result;
    int jitCount = 0;

    if (useJit)
    {
        var hybridVm = new JitVirtualMachine(program, runtime, jitThreshold);
        result = hybridVm.Run("main");
        jitCount = hybridVm.JittedFunctionCount;
    }
    else
    {
        var vm = new VirtualMachine(program, runtime);
        result = vm.Run("main");
    }

    sw.Stop();

    Console.WriteLine();
    Section("Результат");
    Console.WriteLine($"Время: {sw.ElapsedMilliseconds} мс");
    Console.WriteLine($"Exit Code: {result}");

    if (useJit)
    {
        Console.WriteLine($"JIT-компиляций: {jitCount}");
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"[CRASH] Ошибка выполнения: {ex.Message}");
    // Console.WriteLine(ex.StackTrace); 
    return 3;
}

// ======================
// Вспомогательные методы
// ======================

static void Header(string title)
{
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

// Красивый вывод AST
static void PrintAst(AstNode node, int indent = 0, bool isLast = true)
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
            Console.WriteLine($"{prefix}{pointer} Function: {fn.Name} -> {fn.ReturnType}");
            Console.WriteLine($"{prefix}  ├─ Parameters:");
            for (var i = 0; i < fn.Parameters.Count; i++)
                PrintAst(fn.Parameters[i], indent + 2, i == fn.Parameters.Count - 1);
            Console.WriteLine($"{prefix}  └─ Body:");
            for (var i = 0; i < fn.Body.Statements.Count; i++)
                PrintAst(fn.Body.Statements[i], indent + 2, i == fn.Body.Statements.Count - 1);
            break;

        case BlockStatement block:
            Console.WriteLine($"{prefix}{pointer} Block");
            for (var i = 0; i < block.Statements.Count; i++)
                PrintAst(block.Statements[i], indent + 1, i == block.Statements.Count - 1);
            break;

        case IfStatement ifStmt:
            Console.WriteLine($"{prefix}{pointer} If: {ExprToString(ifStmt.Condition)}");
            PrintAst(ifStmt.ThenBranch, indent + 1, ifStmt.ElseBranch == null);
            if (ifStmt.ElseBranch != null) PrintAst(ifStmt.ElseBranch, indent + 1, true);
            break;
            
        case WhileStatement wh:
            Console.WriteLine($"{prefix}{pointer} While: {ExprToString(wh.Condition)}");
            PrintAst(wh.Body, indent + 1, true);
            break;

        case ReturnStatement ret:
            Console.WriteLine($"{prefix}{pointer} Return: {ExprToString(ret.Value)}");
            break;
            
        case ExpressionStatement expr:
            Console.WriteLine($"{prefix}{pointer} Expr: {ExprToString(expr.Expression)}");
            break;
            
        case VariableDeclaration varDecl:
             Console.WriteLine($"{prefix}{pointer} Var: {varDecl.TypeName} {varDecl.Name} = {ExprToString(varDecl.Initializer)}");
             break;

        default:
            Console.WriteLine($"{prefix}{pointer} {node.NodeType}");
            break;
    }
}

static string ExprToString(Expression? expr)
{
    if (expr == null) return "null";
    return expr switch
    {
        IdentifierExpression id => id.Name,
        LiteralExpression lit => lit.Value?.ToString() ?? "null",
        BinaryExpression bin => $"({ExprToString(bin.Left)} {bin.Operator.Text} {ExprToString(bin.Right)})",
        CallExpression call => $"{ExprToString(call.Callee)}(...)",
        _ => expr.NodeType.ToString()
    };
}