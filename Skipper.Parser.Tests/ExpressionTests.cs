using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Expressions;
using Xunit;

namespace Skipper.Parser.Tests;

public class ExpressionTests
{
    [Fact]
    public void Parse_BinaryExpression_RespectsPrecedence()
    {
        // 1 + 2 * 3 должно парситься как 1 + (2 * 3)
        var expr = TestHelpers.ParseExpression<BinaryExpression>("1 + 2 * 3");

        Assert.Equal(TokenType.PLUS, expr.Operator.Type);

        var left = Assert.IsType<LiteralExpression>(expr.Left);
        Assert.Equal(1L, left.Value);

        var right = Assert.IsType<BinaryExpression>(expr.Right);
        Assert.Equal(TokenType.STAR, right.Operator.Type);

        var rightLeft = Assert.IsType<LiteralExpression>(right.Left);
        Assert.Equal(2L, rightLeft.Value);

        var rightRight = Assert.IsType<LiteralExpression>(right.Right);
        Assert.Equal(3L, rightRight.Value);
    }

    [Fact]
    public void Parse_Parentheses_OverridePrecedence()
    {
        // (1 + 2) * 3
        var expr = TestHelpers.ParseExpression<BinaryExpression>("(1 + 2) * 3");

        Assert.Equal(TokenType.STAR, expr.Operator.Type);

        var left = Assert.IsType<BinaryExpression>(expr.Left);
        Assert.Equal(TokenType.PLUS, left.Operator.Type); // (1+2) внутри

        var right = Assert.IsType<LiteralExpression>(expr.Right);
        Assert.Equal(3L, right.Value);
    }

    [Fact]
    public void Parse_UnaryExpression_Works()
    {
        // -5
        var expr = TestHelpers.ParseExpression<UnaryExpression>("-5");

        Assert.Equal(TokenType.MINUS, expr.Operator.Type);
        var operand = Assert.IsType<LiteralExpression>(expr.Operand);
        Assert.Equal(5L, operand.Value);
    }

    [Fact]
    public void Parse_FunctionCall_Works()
    {
        // factorial(n - 1)
        var expr = TestHelpers.ParseExpression<CallExpression>("factorial(n - 1)");

        var callee = Assert.IsType<IdentifierExpression>(expr.Callee);
        Assert.Equal("factorial", callee.Name);

        Assert.Single(expr.Arguments);
        Assert.IsType<BinaryExpression>(expr.Arguments[0]);
    }

    [Fact]
    public void Parse_ArrayAccess_Works()
    {
        // arr[i + 1]
        var expr = TestHelpers.ParseExpression<ArrayAccessExpression>("arr[i + 1]");

        var target = Assert.IsType<IdentifierExpression>(expr.Target);
        Assert.Equal("arr", target.Name);

        Assert.IsType<BinaryExpression>(expr.Index);
    }

    [Fact]
    public void Parse_MemberAccess_Works()
    {
        // list.length
        var expr = TestHelpers.ParseExpression<MemberAccessExpression>("list.length");

        var obj = Assert.IsType<IdentifierExpression>(expr.Object);
        Assert.Equal("list", obj.Name);
        Assert.Equal("length", expr.MemberName);
    }

    [Fact]
    public void Parse_NewObject_Works()
    {
        // new User("Ivan")
        var expr = TestHelpers.ParseExpression<NewObjectExpression>("new User(\"Ivan\")");

        Assert.Equal("User", expr.ClassName);
        Assert.Single(expr.Arguments);
        var arg = Assert.IsType<LiteralExpression>(expr.Arguments[0]);
        Assert.Equal("Ivan", arg.Value);
    }

    [Fact]
    public void Parse_LogicalPrecedence_Works()
    {
        // && должно иметь приоритет выше, чем ||
        // "a || b && c" -> "a || (b && c)"
        var expr = TestHelpers.ParseExpression<BinaryExpression>("a || b && c");

        Assert.Equal(TokenType.OR, expr.Operator.Type);

        // Справа должно быть выражение &&
        var right = Assert.IsType<BinaryExpression>(expr.Right);
        Assert.Equal(TokenType.AND, right.Operator.Type);
    }

    [Fact]
    public void Parse_ChainedAssignment_Works()
    {
        // a = b = 5 должно парситься как a = (b = 5) (право-ассоциативно)
        var expr = TestHelpers.ParseExpression<BinaryExpression>("a = b = 5");

        Assert.Equal(TokenType.ASSIGN, expr.Operator.Type);
        var targetA = Assert.IsType<IdentifierExpression>(expr.Left);
        Assert.Equal("a", targetA.Name);

        var right = Assert.IsType<BinaryExpression>(expr.Right);
        Assert.Equal(TokenType.ASSIGN, right.Operator.Type);

        var targetB = Assert.IsType<IdentifierExpression>(right.Left);
        Assert.Equal("b", targetB.Name);
    }

    [Fact]
    public void Parse_ComplexMath_Works()
    {
        // Проверка сложного выражения со скобками и разными приоритетами
        // (5 + 3) * 2 - 10 / 5
        var expr = TestHelpers.ParseExpression<BinaryExpression>("(5 + 3) * 2 - 10 / 5");

        // Дерево должно быть: MINUS( MULT(GROUP(ADD), 2), DIV(10, 5) )
        Assert.Equal(TokenType.MINUS, expr.Operator.Type);

        var leftMult = Assert.IsType<BinaryExpression>(expr.Left);
        Assert.Equal(TokenType.STAR, leftMult.Operator.Type);

        var rightDiv = Assert.IsType<BinaryExpression>(expr.Right);
        Assert.Equal(TokenType.SLASH, rightDiv.Operator.Type);
    }
}