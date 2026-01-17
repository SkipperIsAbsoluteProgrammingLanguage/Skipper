using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Writer;
using Skipper.Lexer.Lexer;
using Skipper.Parser.Parser;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.Semantic;
using Skipper.VM;
using System.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine("Использование: Skipper <file.sk> [--jit] [--jit-threshold N]");
    return 1;
}

var useJit = false;
var jitThreshold = 50;
string? path = null;

for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];

    if (arg == "--jit")
    {
        useJit = true;
        continue;
    }

    if (arg.StartsWith("--jit-threshold=", StringComparison.Ordinal))
    {
        var value = arg["--jit-threshold=".Length..];
        if (!int.TryParse(value, out jitThreshold))
        {
            Console.WriteLine($"Неверное значение --jit-threshold: {value}");
            return 1;
        }
        continue;
    }

    if (arg == "--jit-threshold" && i + 1 < args.Length)
    {
        var value = args[++i];
        if (!int.TryParse(value, out jitThreshold))
        {
            Console.WriteLine($"Неверное значение --jit-threshold: {value}");
            return 1;
        }
        continue;
    }

    if (path == null)
    {
        path = arg;
        continue;
    }

    Console.WriteLine($"Неизвестный аргумент: {arg}");
    return 1;
}

if (path == null)
{
    Console.WriteLine("Использование: Skipper <file.sk> [--jit] [--jit-threshold N]");
    return 1;
}

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не найден: {path}");
    return 1;
}

var code = File.ReadAllText(path);

// Лексер
Section("Лексер");

var lexer = new Lexer(code);
LexerResult lexerResult = lexer.TokenizeWithDiagnostics();

if (lexerResult.HasErrors)
{
    Console.WriteLine("Ошибки:");
    foreach (LexerDiagnostic diag in lexerResult.Diagnostics)
    {
        Console.WriteLine($"  - {diag}");
    }

    return 2;
}

Console.WriteLine($"Токенов: {lexerResult.Tokens.Count}");

// Парсер
Section("Парсер");

var parser = new Parser(lexerResult.Tokens);
ParserResult parserResult = parser.Parse();

if (parserResult.HasErrors)
{
    Console.WriteLine("Ошибки:");
    foreach (ParserDiagnostic diag in parserResult.Diagnostics)
    {
        Console.WriteLine($"  - {diag}");
    }

    return 2;
}

Console.WriteLine("AST построен");

// Семантический анализ
Section("Семантика");

var semantic = new SemanticAnalyzer();
semantic.VisitProgram(parserResult.Root);

if (semantic.HasErrors)
{
    Console.WriteLine("Ошибки:");
    foreach (SemanticDiagnostic diag in semantic.Diagnostics)
    {
        Console.WriteLine($"  - {diag}");
    }

    return 2;
}

Console.WriteLine("Проверка пройдена");

// Генерация байткода
Section("Байткод");

var generator = new BytecodeGenerator();
BytecodeProgram program = generator.Generate(parserResult.Root);

Console.WriteLine($"Функций: {program.Functions.Count}");
Console.WriteLine($"Констант: {program.ConstantPool.Count}");
Console.WriteLine($"Классов: {program.Classes.Count}");

var bytecodePath = Path.ChangeExtension(path, ".json");
new BytecodeWriter(program).SaveToFile(bytecodePath);
Console.WriteLine($"Сохранено: {bytecodePath}");

// Выполнение
Section("Выполнение");

Console.WriteLine(useJit ? $"Режим: JIT (порог: {jitThreshold})" : "Режим: Интерпретатор");
Console.WriteLine();

try
{
    var runtime = new RuntimeContext();
    var sw = Stopwatch.StartNew();

    Value result;
    var jitCount = 0;

    if (useJit)
    {
        var hybridVm = new HybridVirtualMachine(program, runtime, jitThreshold);
        result = hybridVm.Run("main");
        jitCount = hybridVm.JittedFunctionCount;
    } else
    {
        var vm = new VirtualMachine(program, runtime);
        result = vm.Run("main");
    }

    sw.Stop();

    Console.WriteLine();
    Section("Результат");
    Console.WriteLine($"Время: {sw.ElapsedMilliseconds} мс");
    Console.WriteLine($"Результат: {result}");

    if (useJit)
    {
        Console.WriteLine($"JIT-компиляций: {jitCount}");
    }

    return 0;
} catch (Exception ex)
{
    Console.WriteLine($"Ошибка выполнения: {ex.Message}");
    return 3;
}

static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine($"--- {title} ---");
}
