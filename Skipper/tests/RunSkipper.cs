// using Skipper.BaitCode.Generator;
// using Skipper.Lexer.Lexer;
// using Skipper.Parser.Parser;
// using Skipper.Runtime;
// using Skipper.Runtime.Values;
// using Skipper.Semantic;
// using Skipper.VM;
//
// var argsList = Environment.GetCommandLineArgs().Skip(1).ToArray();
// if (argsList.Length == 0)
// {
//     Console.WriteLine("Usage: RunSkipper <file.sk> [--jit] [--jit-threshold N]");
//     return;
// }
//
// var useJit = false;
// var jitThreshold = 50;
// string? path = null;
//
// for (var i = 0; i < argsList.Length; i++)
// {
//     var arg = argsList[i];
//     if (arg == "--jit")
//     {
//         useJit = true;
//         continue;
//     }
//
//     if (arg.StartsWith("--jit-threshold=", StringComparison.Ordinal))
//     {
//         var value = arg["--jit-threshold=".Length..];
//         if (!int.TryParse(value, out jitThreshold))
//         {
//             Console.WriteLine($"Invalid --jit-threshold value: {value}");
//             return;
//         }
//
//         continue;
//     }
//
//     if (arg == "--jit-threshold" && i + 1 < argsList.Length)
//     {
//         var value = argsList[++i];
//         if (!int.TryParse(value, out jitThreshold))
//         {
//             Console.WriteLine($"Invalid --jit-threshold value: {value}");
//             return;
//         }
//
//         continue;
//     }
//
//     if (path == null)
//     {
//         path = arg;
//         continue;
//     }
//
//     Console.WriteLine($"Unknown argument: {arg}");
//     return;
// }
//
// if (path == null)
// {
//     Console.WriteLine("Usage: RunSkipper <file.sk> [--jit] [--jit-threshold N]");
//     return;
// }
//
// var code = File.ReadAllText(path);
//
// var lexer = new Lexer(code);
// var lexerResult = lexer.TokenizeWithDiagnostics();
// if (lexerResult.HasErrors)
// {
//     Console.WriteLine("Lexer errors:");
//     foreach (var diag in lexerResult.Diagnostics)
//     {
//         Console.WriteLine($"- {diag}");
//     }
//     return;
// }
//
// var parser = new Parser(lexerResult.Tokens);
// var parserResult = parser.Parse();
// if (parserResult.HasErrors)
// {
//     Console.WriteLine("Parser errors:");
//     foreach (var diag in parserResult.Diagnostics)
//     {
//         Console.WriteLine($"- {diag}");
//     }
//     return;
// }
//
// var semantic = new SemanticAnalyzer();
// semantic.VisitProgram(parserResult.Root);
// if (semantic.HasErrors)
// {
//     Console.WriteLine("Semantic errors:");
//     foreach (var diag in semantic.Diagnostics)
//     {
//         Console.WriteLine($"- {diag}");
//     }
//     return;
// }
//
// var generator = new BytecodeGenerator();
// var program = generator.Generate(parserResult.Root);
//
// var runtime = new RuntimeContext();
// Value result;
// if (useJit)
// {
//     var hybrid = new HybridVirtualMachine(program, runtime, jitThreshold);
//     result = hybrid.Run("main");
//     Console.WriteLine($"JIT compiled functions: {hybrid.JittedFunctionCount}");
// }
// else
// {
//     result = new VirtualMachine(program, runtime).Run("main");
// }
//
// Console.WriteLine($"Result: {result}");
