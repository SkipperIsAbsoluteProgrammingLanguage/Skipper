using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Writer;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class SerializationTests
{
    private static BytecodeProgram Generate(string source)
    {
        var lexer = new Lexer.Lexer.Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser.Parser.Parser(tokens);
        var result = parser.Parse();
        var generator = new BytecodeGenerator();
        return generator.Generate(result.Root);
    }

    [Fact]
    public void Serialization_FullCycle_PreservesData()
    {
        // Arrange
        const string code = """
                            class User { int id; }
                            fn add(int a, int b) -> int { return a + b; }
                            fn main() { 
                                int x = 10;
                                User u = new User();
                            }
                            """;

        var originalProgram = Generate(code);
        var tempFile = Path.GetTempFileName();
        var writer = new BytecodeWriter(originalProgram);

        try
        {
            // Act
            writer.SaveToFile(tempFile);
            var loadedProgram = BytecodeWriter.LoadFromFile(tempFile);

            // Assert

            // 1. Проверяем функции (main, add)
            Assert.NotEmpty(loadedProgram.Functions);
            Assert.Equal(originalProgram.Functions.Count, loadedProgram.Functions.Count);

            var mainOrig = originalProgram.Functions.First(f => f.Name == "main");
            var mainLoaded = loadedProgram.Functions.First(f => f.Name == "main");

            Assert.Equal(mainOrig.Code.Count, mainLoaded.Code.Count);
            Assert.Equal(mainOrig.Code[0].OpCode, mainLoaded.Code[0].OpCode);

            // 2. Проверяем типы и классы
            Assert.NotEmpty(loadedProgram.Types);
            Assert.NotEmpty(loadedProgram.Classes);
            Assert.Equal("User", loadedProgram.Classes[0].Name);

            // 3. Проверяем константы
            Assert.Equal(originalProgram.ConstantPool.Count, loadedProgram.ConstantPool.Count);
        } finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}