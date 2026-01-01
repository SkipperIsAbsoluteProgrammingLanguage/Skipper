using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Xunit;

namespace Skipper.Parser.Tests;

public static class TestHelpers
{
    public static ProgramNode Parse(string source, bool expectErrors = false)
    {
        var lexer = new Lexer.Lexer.Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser.Parser(tokens);
        var program = parser.Parse();

        if (!expectErrors)
        {
            Assert.False(
                parser.HasErrors,
                $"Parser has errors: {string.Join(", ", parser.Diagnostics.Select(d => d.Message))}");
        }
        else
        {
            Assert.True(parser.HasErrors, "Expected parser errors, but none were found.");
        }

        return program;
    }

    /// <summary>
    /// Хелпер для получения единственного выражения из source (оборачивает в fn main)
    /// </summary>
    public static T ParseExpression<T>(string expressionSource) where T : Expression
    {
        var source = $"fn main() {{ var = {expressionSource}; }}";
        var program = Parse(source);

        var func = (FunctionDeclaration)program.Declarations[0];
        var stmt = (ExpressionStatement)func.Body.Statements[0];
        var assignment = (BinaryExpression)stmt.Expression;

        return Assert.IsType<T>(assignment.Right);
    }
}