using Skipper.Lexer.Tokens;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Xunit;

namespace Skipper.Semantic.Tests;

public class FallbackBranchesTests
{
    [Fact]
    public void UnsupportedCallExpression_ReportsDiagnostic()
    {
        // Arrange
        const string code = "fn main() { (1 + 2)(3); }";

        // Act
        var semantic = TestHelpers.Analyze(code);

        // Assert
        Assert.Contains(semantic.Diagnostics, d => d.Message.Contains("Unsupported call expression"));
    }

    [Fact]
    public void UnsupportedClassMember_ReportsDiagnostic()
    {
        // Arrange
        // Construct ProgramNode with a class that contains a ParameterDeclaration as a member
        var members = new List<Declaration> { new ParameterDeclaration("int", "p") };
        var cls = new ClassDeclaration("C", members);
        var program = new ProgramNode([cls]);

        // Act
        var analyzer = new SemanticAnalyzer();
        analyzer.VisitProgram(program);

        // Assert
        Assert.Contains(analyzer.Diagnostics, d => d.Message.Contains("Unsupported class member"));
    }

    [Fact]
    public void UnsupportedBinaryOperator_ReportsDiagnostic()
    {
        // Arrange
        var left = new LiteralExpression(1L, new Token(TokenType.NUMBER, "1", 0, 1, 1));
        var right = new LiteralExpression(2L, new Token(TokenType.NUMBER, "2", 1, 1, 3));
        var op = new Token(TokenType.ARROW, "->", 2, 1, 2); // ARROW is not handled in VisitBinaryExpression

        var bin = new BinaryExpression(left, op, right);

        // Act
        var analyzer = new SemanticAnalyzer();
        analyzer.VisitBinaryExpression(bin);

        // Assert
        Assert.Contains(analyzer.Diagnostics, d => d.Message.Contains("Unsupported binary operator"));
    }

    [Fact]
    public void UnsupportedUnaryOperator_ReportsDiagnostic()
    {
        // Arrange
        var operand = new LiteralExpression(1L, new Token(TokenType.NUMBER, "1", 0, 1, 1));
        var op = new Token(TokenType.ARROW, "->", 2, 1, 2); // ARROW is not a unary operator handled by analyzer

        var unary = new UnaryExpression(op, operand);

        // Act
        var analyzer = new SemanticAnalyzer();
        analyzer.VisitUnaryExpression(unary);

        // Assert
        Assert.Contains(analyzer.Diagnostics, d => d.Message.Contains("Unsupported unary operator"));
    }
}