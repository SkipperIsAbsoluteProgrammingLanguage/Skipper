using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class TestHelpers
{
    public static BytecodeProgram Generate(string source)
    {
        var lexer = new Lexer.Lexer.Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser.Parser.Parser(tokens);
        var result = parser.Parse();

        if (result.HasErrors)
        {
            throw new Exception($"Parser errors: {string.Join(", ", result.Diagnostics.Select(d => d.Message))}");
        }

        var generator = new BytecodeGenerator();
        return generator.Generate(result.Root);
    }

    public static List<Instruction> GetInstructions(BytecodeProgram program, string funcName)
    {
        var func = program.Functions.FirstOrDefault(f => f.Name == funcName);
        Assert.NotNull(func);
        return func.Code;
    }
}