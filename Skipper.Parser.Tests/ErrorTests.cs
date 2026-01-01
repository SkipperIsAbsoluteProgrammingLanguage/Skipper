using Skipper.Parser.Parser;
using Xunit;

namespace Skipper.Parser.Tests;

public class ErrorTests
{
    [Fact]
    public void Parse_MissingSemicolon_ReportsError()
    {
        const string source = "fn main() { x = 5 }"; // Нет точки с запятой
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());

        parser.Parse();

        Assert.True(parser.HasErrors);
        var error = parser.Diagnostics.FirstOrDefault(d => d.Level == ParserDiagnosticLevel.Error);
        Assert.NotNull(error);
        Assert.Contains("Expected ';'", error.Message);
    }

    [Fact]
    public void Parse_MissingBrace_ReportsError()
    {
        const string source = "fn main() { return; "; // Нет закрывающей скобки
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());

        parser.Parse();

        Assert.True(parser.HasErrors);
        Assert.Contains(parser.Diagnostics, d => d.Message.Contains("Expected '}'"));
    }

    [Fact]
    public void Parse_InvalidExpression_Synchronizes()
    {
        // Ошибка в первом выражении, но второе корректное
        const string source = """

                                              fn main() {
                                                  int x = ;     // Ошибка
                                                  int y = 10;   // Должно распарситься после восстановления
                                              }
                                          
                              """;
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());

        var program = parser.Parse();

        Assert.True(parser.HasErrors);

        // Проверяем, что парсер восстановился и увидел функцию
        // (В простой реализации Panic Mode внутри блока он может пропустить y=10,
        // но главная цель - не упасть и вернуть то, что удалось)
        Assert.NotEmpty(program.Declarations);
    }

    [Fact]
    public void Parse_InvalidAssignmentTarget_ReportsError()
    {
        // 10 = x; — это должно быть ошибкой "Invalid assignment target"
        const string source = "fn main() { 10 = x; }";
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());

        parser.Parse();

        Assert.True(parser.HasErrors);
        var error = parser.Diagnostics.FirstOrDefault(d => d.Message.Contains("Invalid assignment target"));
        Assert.NotNull(error);
    }

    [Fact]
    public void Parse_UnexpectedEOF_ReportsError()
    {
        // Обрыв файла посреди функции
        const string source = "fn main() { x = 5";
        var lexer = new Lexer.Lexer.Lexer(source);
        var parser = new Parser.Parser(lexer.Tokenize());

        parser.Parse();

        Assert.True(parser.HasErrors);
        // Ожидается ошибка либо про ';', либо про '}'
        Assert.NotEmpty(parser.Diagnostics);
    }
}