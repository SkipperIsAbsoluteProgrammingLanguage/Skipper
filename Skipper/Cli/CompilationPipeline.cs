using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Reader;
using Skipper.BaitCode.Writer;
using Skipper.Lexer.Lexer;
using Skipper.Parser.AST;
using Skipper.Parser.Parser;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.Semantic;
using Skipper.VM.Interpreter;
using Skipper.VM.Jit;

namespace Skipper.Cli;

public static class CompilationPipeline
{
    public static int Run(ProgramOptions options)
    {
        IReporter reporter = options.Trace ? new ConsoleReporter() : new NullReporter();

        BytecodeProgram bytecodeProgram;

        if (options.RunFromBytecode)
        {
            bytecodeProgram = LoadBytecode(options.Path, reporter);
        }
        else
        {
            var code = File.ReadAllText(options.Path);

            var lexerResult = RunLexer(code, reporter);
            var parserResult = RunParser(lexerResult.Tokens, reporter);
            var semantic = RunSemantic(parserResult.Root, reporter);

            if (lexerResult.HasErrors || parserResult.HasErrors || semantic.HasErrors)
            {
                reporter.Header("[FAIL] Compilation failed");

                return 2;
            }

            bytecodeProgram = GenerateBytecode(parserResult.Root, options.Path, reporter);
        }

        reporter.Section("VM");

        var runtime = new RuntimeContext((long)options.MemMb * 1024 * 1024, options.Trace);
        var result = options.UseJit
            ? RunJit(bytecodeProgram, runtime, options.JitThreshold, reporter)
            : new VirtualMachine(bytecodeProgram, runtime, options.Trace).Run("main");

        reporter.Line($"[ OK ] Program result: {result}");

        reporter.Header(options.RunFromBytecode
            ? "[ OK ] Execution finished successfully"
            : "[ OK ] Compilation finished successfully");

        var exitCode = GetExitCode(result);
        if (exitCode != 0)
        {
            Console.Error.WriteLine($"[FAIL] Program exit code: {exitCode}");
            return exitCode;
        }

        return 0;
    }

    private static LexerResult RunLexer(string code, IReporter reporter)
    {
        reporter.Section("Lexer");

        var lexer = new Lexer.Lexer.Lexer(code);
        var lexerResult = lexer.TokenizeWithDiagnostics();

        if (lexerResult.HasErrors)
        {
            reporter.PrintDiagnostics(lexerResult.Diagnostics);
        }
        else
        {
            reporter.Line("[ OK ] No lexer errors");
        }

        var tokens = lexerResult.Tokens;
        reporter.Line($"\nTokens ({tokens.Count}):");
        for (var i = 0; i < tokens.Count; i++)
        {
            reporter.Indent(1, $"[{i}] {tokens[i]}");
        }

        return lexerResult;
    }

    private static ParserResult RunParser(IReadOnlyList<Lexer.Tokens.Token> tokens, IReporter reporter)
    {
        reporter.Section("Parser");

        var parser = new Parser.Parser.Parser(tokens);
        var parserResult = parser.Parse();

        if (parserResult.HasErrors)
        {
            reporter.PrintDiagnostics(parserResult.Diagnostics);
        }
        else
        {
            reporter.Line("[ OK ] No parser errors");
        }

        reporter.Line("\n[ OK ] AST:");
        AstPrinter.Print(reporter, parserResult.Root);

        return parserResult;
    }

    private static SemanticAnalyzer RunSemantic(ProgramNode root, IReporter reporter)
    {
        reporter.Section("Semantic analysis");

        var semantic = new SemanticAnalyzer();
        semantic.VisitProgram(root);

        if (semantic.HasErrors)
        {
            reporter.PrintDiagnostics(semantic.Diagnostics);
        }
        else
        {
            reporter.Line("[ OK ] No semantic errors");
        }

        return semantic;
    }

    private static BytecodeProgram GenerateBytecode(ProgramNode root, string sourcePath, IReporter reporter)
    {
        reporter.Section("Bytecode");

        var bytecodeGenerator = new BytecodeGenerator();
        var bytecodeProgram = bytecodeGenerator.Generate(root);

        var bytecodePath = BytecodePathResolver.GetBytecodePath(sourcePath);
        var writer = new BytecodeWriter(bytecodeProgram);
        writer.SaveToFile(bytecodePath);

        reporter.Line($"[ OK ] Bytecode saved: {bytecodePath}");

        return bytecodeProgram;
    }

    private static BytecodeProgram LoadBytecode(string path, IReporter reporter)
    {
        reporter.Section("Bytecode");

        var program = BytecodeReader.LoadFromFile(path);
        reporter.Line($"[ OK ] Bytecode loaded: {path}");

        return program;
    }

    private static Value RunJit(BytecodeProgram program, RuntimeContext runtime, int threshold, IReporter reporter)
    {
        var jitVm = new JitVirtualMachine(program, runtime, threshold, reporter is ConsoleReporter);
        var result = jitVm.Run("main");
        reporter.Line($"[ OK ] JIT compiled functions: {jitVm.JittedFunctionCount}");
        return result;
    }

    private static int GetExitCode(Value result)
    {
        return result.Kind switch
        {
            ValueKind.Int => result.AsInt() == 0 ? 0 : 1,
            ValueKind.Long => result.AsLong() == 0 ? 0 : 1,
            ValueKind.Bool => result.AsBool() ? 1 : 0,
            ValueKind.Double => Math.Abs(result.AsDouble()) > double.Epsilon ? 1 : 0,
            _ => 0
        };
    }
}
