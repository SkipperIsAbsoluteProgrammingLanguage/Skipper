using Skipper.Lexer.Lexer;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.Parser;
using Skipper.Semantic;

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

var lexer = new Lexer(code);
var tokens = lexer.Tokenize();

Console.WriteLine("\nTokens:");
foreach (var t in tokens)
{
    Console.WriteLine($" - {t}");
}

var parser = new Parser(tokens);
var result = parser.Parse();

Console.WriteLine("\nParsed declarations:");
foreach (var decl in result.Root.Declarations)
{
    Console.WriteLine($" - {decl.NodeType} {decl.Token?.Text} name={decl.Name}");

    if (decl.NodeType == AstNodeType.FunctionDeclaration)
    {
        var fn = (FunctionDeclaration)decl;
        Console.WriteLine($"   params: {string.Join(", ", fn.Parameters.Select(p => p.TypeName + " " + p.Name))}");
        Console.WriteLine("   body statements:");
        foreach (var s in fn.Body.Statements)
        {
            Console.WriteLine($"     - {s.NodeType} token={s.Token?.Text}");
            if (s is VariableDeclaration v)
            {
                Console.WriteLine($"       var: {v.TypeName} {v.Name}");
            }
        }
    }
}

var semantic = new SemanticAnalyzer();
semantic.VisitProgram(result.Root);

Console.WriteLine("\nDiagnostics:");
foreach (var diag in semantic.Diagnostics)
{
    Console.WriteLine(diag);
}

return semantic.Diagnostics.Count == 0 ? 0 : 2;