using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Semantic;
using Xunit;

namespace Skipper.BaitCode.Tests;

public static class TestHelpers
{
    public static BytecodeProgram Generate(string source)
    {
        var lexer = new Lexer.Lexer.Lexer(source);
        var lexerResult = lexer.TokenizeWithDiagnostics();
        Assert.False(lexerResult.HasErrors, "Lexer errors:\n" + string.Join("\n", lexerResult.Diagnostics));

        var parser = new Parser.Parser.Parser(lexerResult.Tokens);
        var parserResult = parser.Parse();
        Assert.False(parserResult.HasErrors, "Parser errors:\n" + string.Join("\n", parserResult.Diagnostics));

        var semantic = new SemanticAnalyzer();
        semantic.VisitProgram(parserResult.Root);
        Assert.False(semantic.HasErrors, "Semantic errors:\n" + string.Join("\n", semantic.Diagnostics));

        var generator = new BytecodeGenerator();
        return generator.Generate(parserResult.Root);
    }

    public static List<Instruction> GetInstructions(BytecodeProgram program, string funcName)
    {
        var func = program.Functions.FirstOrDefault(f => f.Name == funcName);
        Assert.NotNull(func);
        return func.Code;
    }

    public static HashSet<int> GetTempSlots(BytecodeProgram program, string funcName)
    {
        var func = program.Functions.First(f => f.Name == funcName);
        var tempSlots = func.Locals
            .Where(l => l.Name.StartsWith("__tmp", StringComparison.Ordinal))
            .Select(l => l.VariableId)
            .ToHashSet();

        Assert.NotEmpty(tempSlots);
        return tempSlots;
    }
}