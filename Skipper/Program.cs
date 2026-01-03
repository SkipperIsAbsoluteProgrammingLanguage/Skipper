using Skipper.Lexer.Lexer;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.Parser;
using Skipper.Semantic;

Header("🚀 Skipper Compiler");

if (args.Length == 0)
{
    Console.WriteLine("Usage: Skipper <file.sk>");
    return 1;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {path}");
    return 1;
}

var code = File.ReadAllText(path);

// ======================
// Lexer
// ======================
Section("🔍 Lexer");

var lexer = new Lexer(code);
var lexerResult = lexer.TokenizeWithDiagnostics();

if (lexerResult.HasErrors)
{
    Console.WriteLine("❌ Diagnostics:");
    foreach (var diag in lexerResult.Diagnostics)
        Indent(1, $"- {diag}");
}
else
{
    Console.WriteLine("✔ No lexer errors");
}

var tokens = lexerResult.Tokens;
Console.WriteLine($"\n✔ Tokens ({tokens.Count}):");
for (var i = 0; i < tokens.Count; i++)
{
    Indent(1, $"[{i}] {tokens[i]}");
}

// ======================
// Parser
// ======================
Section("🧩 Parser");

var parser = new Parser(tokens);
var parserResult = parser.Parse();

if (parserResult.HasErrors)
{
    Console.WriteLine("❌ Diagnostics:");
    foreach (var diag in parserResult.Diagnostics)
        Indent(1, $"- {diag}");
}
else
{
    Console.WriteLine("✔ No parser errors");
}

Console.WriteLine($"\n✔ Declarations ({parserResult.Root.Declarations.Count}):");

foreach (var decl in parserResult.Root.Declarations)
{
    Indent(1, $"📦 {decl.NodeType}: {decl.Name}");

    if (decl is FunctionDeclaration fn)
    {
        Indent(2, "├─ Parameters:");
        if (fn.Parameters.Count == 0)
        {
            Indent(3, "(none)");
        }
        else
        {
            foreach (var p in fn.Parameters)
                Indent(3, $"{p.TypeName} {p.Name}");
        }

        Indent(2, "└─ Body:");
        foreach (var stmt in fn.Body.Statements)
        {
            Indent(3, $"├─ {stmt.NodeType}");

            if (stmt is VariableDeclaration v)
                Indent(4, $"{v.TypeName} {v.Name}");
        }
    }
}

// ======================
// Semantic analysis
// ======================
Section("🧠 Semantic analysis");

var semantic = new SemanticAnalyzer();
semantic.VisitProgram(parserResult.Root);

if (semantic.HasErrors)
{
    Console.WriteLine("❌ Errors:");
    foreach (var diag in semantic.Diagnostics)
    {
        Indent(1, $"- {diag}");
    }
}
else
{
    Console.WriteLine("✔ No semantic errors");
}

Header(
    lexerResult.HasErrors || parserResult.HasErrors || semantic.HasErrors
        ? "❌ Compilation failed"
        : "✅ Compilation finished successfully"
);

return semantic.Diagnostics.Count == 0 ? 0 : 2;


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