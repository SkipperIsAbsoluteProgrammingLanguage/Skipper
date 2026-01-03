namespace Skipper.Semantic.Tests;

public static class SemanticTestHelper
{
    public static SemanticAnalyzer Analyze(string source)
    {
        var lexer = new Lexer.Lexer.Lexer(source);
        var tokens = lexer.Tokenize();

        var parser = new Parser.Parser.Parser(tokens);
        var result = parser.Parse();

        var semantic = new SemanticAnalyzer();
        semantic.VisitProgram(result.Root);

        return semantic;
    }
}