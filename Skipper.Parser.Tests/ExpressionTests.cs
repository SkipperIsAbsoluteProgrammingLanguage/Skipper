using Skipper.Lexer.Tokens;
using Skipper.Parser.AST.Expressions;
using Xunit;

namespace Skipper.Parser.Tests;

public class ExpressionTests
{
    [Fact]
    public void Parse_BinaryExpression_RespectsPrecedence()
    {
        // Arrange
        const string source = "1 + 2 * 3";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.PLUS, expr.Operator.Type);

        var left = Assert.IsType<LiteralExpression>(expr.Left);
        Assert.Equal(1, left.Value);

        var right = Assert.IsType<BinaryExpression>(expr.Right);
        Assert.Equal(TokenType.STAR, right.Operator.Type);

        var rightLeft = Assert.IsType<LiteralExpression>(right.Left);
        Assert.Equal(2, rightLeft.Value);

        var rightRight = Assert.IsType<LiteralExpression>(right.Right);
        Assert.Equal(3, rightRight.Value);
    }

    [Fact]
    public void Parse_Parentheses_OverridePrecedence()
    {
        // Arrange
        const string source = "(1 + 2) * 3";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.STAR, expr.Operator.Type);

        var left = Assert.IsType<BinaryExpression>(expr.Left);
        Assert.Equal(TokenType.PLUS, left.Operator.Type);

        var right = Assert.IsType<LiteralExpression>(expr.Right);
        Assert.Equal(3, right.Value);
    }

    [Fact]
    public void Parse_UnaryExpression_Works()
    {
        // Arrange
        const string source = "-5";

        // Act
        var expr = TestHelpers.ParseExpression<UnaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.MINUS, expr.Operator.Type);
        var operand = Assert.IsType<LiteralExpression>(expr.Operand);
        Assert.Equal(5, operand.Value);
    }

    [Fact]
    public void Parse_LongLiteral_Works()
    {
        // Arrange
        const string source = "9223372036854775807";

        // Act
        var expr = TestHelpers.ParseExpression<LiteralExpression>(source);

        // Assert
        Assert.Equal(9223372036854775807L, expr.Value);
    }

    [Fact]
    public void Parse_PrefixIncrement_Works()
    {
        // Arrange
        const string source = "++a";

        // Act
        var expr = TestHelpers.ParseExpression<UnaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.INCREMENT, expr.Operator.Type);
        Assert.False(expr.IsPostfix);
        var operand = Assert.IsType<IdentifierExpression>(expr.Operand);
        Assert.Equal("a", operand.Name);
    }

    [Fact]
    public void Parse_PostfixIncrement_Works()
    {
        // Arrange
        const string source = "a++";

        // Act
        var expr = TestHelpers.ParseExpression<UnaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.INCREMENT, expr.Operator.Type);
        Assert.True(expr.IsPostfix);
        var operand = Assert.IsType<IdentifierExpression>(expr.Operand);
        Assert.Equal("a", operand.Name);
    }

    [Fact]
    public void Parse_PostfixDecrement_Works()
    {
        // Arrange
        const string source = "value--";

        // Act
        var expr = TestHelpers.ParseExpression<UnaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.DECREMENT, expr.Operator.Type);
        Assert.True(expr.IsPostfix);
        var operand = Assert.IsType<IdentifierExpression>(expr.Operand);
        Assert.Equal("value", operand.Name);
    }

    [Fact]
    public void Parse_Postfix_RespectsPrecedence()
    {
        // Arrange
        const string source = "a++ + b";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.PLUS, expr.Operator.Type);
        var left = Assert.IsType<UnaryExpression>(expr.Left);
        Assert.Equal(TokenType.INCREMENT, left.Operator.Type);
        Assert.True(left.IsPostfix);
    }

    [Fact]
    public void Parse_FunctionCall_Works()
    {
        // Arrange
        const string source = "factorial(n - 1)";

        // Act
        var expr = TestHelpers.ParseExpression<CallExpression>(source);

        // Assert
        var callee = Assert.IsType<IdentifierExpression>(expr.Callee);
        Assert.Equal("factorial", callee.Name);

        Assert.Single(expr.Arguments);
        Assert.IsType<BinaryExpression>(expr.Arguments[0]);
    }

    [Fact]
    public void Parse_ArrayAccess_Works()
    {
        // Arrange
        const string source = "arr[i + 1]";

        // Act
        var expr = TestHelpers.ParseExpression<ArrayAccessExpression>(source);

        // Assert
        var target = Assert.IsType<IdentifierExpression>(expr.Target);
        Assert.Equal("arr", target.Name);

        Assert.IsType<BinaryExpression>(expr.Index);
    }

    [Fact]
    public void Parse_MemberAccess_Works()
    {
        // Arrange
        const string source = "list.length";

        // Act
        var expr = TestHelpers.ParseExpression<MemberAccessExpression>(source);

        // Assert
        var obj = Assert.IsType<IdentifierExpression>(expr.Object);
        Assert.Equal("list", obj.Name);
        Assert.Equal("length", expr.MemberName);
    }

    [Fact]
    public void Parse_NewObject_Works()
    {
        // Arrange
        const string source = "new User(\"Ivan\")";

        // Act
        var expr = TestHelpers.ParseExpression<NewObjectExpression>(source);

        // Assert
        Assert.Equal("User", expr.ClassName);
        Assert.Single(expr.Arguments);
        var arg = Assert.IsType<LiteralExpression>(expr.Arguments[0]);
        Assert.Equal("Ivan", arg.Value);
    }

    [Fact]
    public void Parse_LogicalPrecedence_Works()
    {
        // Arrange
        const string source = "a || b && c";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.OR, expr.Operator.Type);

        var right = Assert.IsType<BinaryExpression>(expr.Right);
        Assert.Equal(TokenType.AND, right.Operator.Type);
    }

    [Fact]
    public void Parse_ChainedAssignment_Works()
    {
        // Arrange
        const string source = "a = b = 5";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
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
        // Arrange
        const string source = "(5 + 3) * 2 - 10 / 5";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.MINUS, expr.Operator.Type);

        var leftMult = Assert.IsType<BinaryExpression>(expr.Left);
        Assert.Equal(TokenType.STAR, leftMult.Operator.Type);

        var rightDiv = Assert.IsType<BinaryExpression>(expr.Right);
        Assert.Equal(TokenType.SLASH, rightDiv.Operator.Type);
    }

    [Fact]
    public void Parse_ComparisonWithArithmetic_Works()
    {
        // Arrange
        const string source = "1 + 2 > 3 - 4";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.GREATER, expr.Operator.Type);

        var left = Assert.IsType<BinaryExpression>(expr.Left);
        Assert.Equal(TokenType.PLUS, left.Operator.Type);
        Assert.Equal(1, Assert.IsType<LiteralExpression>(left.Left).Value);
        Assert.Equal(2, Assert.IsType<LiteralExpression>(left.Right).Value);

        var right = Assert.IsType<BinaryExpression>(expr.Right);
        Assert.Equal(TokenType.MINUS, right.Operator.Type);
        Assert.Equal(3, Assert.IsType<LiteralExpression>(right.Left).Value);
        Assert.Equal(4, Assert.IsType<LiteralExpression>(right.Right).Value);
    }

    [Fact]
    public void Parse_NestedFunctionCallAndMemberAccess_Works()
    {
        // Arrange
        const string source = "users[0].getName().length";

        // Act
        var expr = TestHelpers.ParseExpression<MemberAccessExpression>(source);

        // Assert
        Assert.Equal("length", expr.MemberName);

        var callExpr = Assert.IsType<CallExpression>(expr.Object);
        var memberAccess = Assert.IsType<MemberAccessExpression>(callExpr.Callee);
        var arrayAccess = Assert.IsType<ArrayAccessExpression>(memberAccess.Object);
        var arr = Assert.IsType<IdentifierExpression>(arrayAccess.Target);

        Assert.Equal("users", arr.Name);
        var index = Assert.IsType<LiteralExpression>(arrayAccess.Index);
        Assert.Equal(0, index.Value);
    }

    [Fact]
    public void Parse_NewArrayExpression_Works()
    {
        // Arrange
        const string source = "new int[10]";

        // Act
        var expr = TestHelpers.ParseExpression<NewArrayExpression>(source);

        // Assert
        Assert.Equal("int", expr.ElementType);
        var size = Assert.IsType<LiteralExpression>(expr.SizeExpression);
        Assert.Equal(10, size.Value);
    }

    [Fact]
    public void Parse_AssignmentToArrayAndMember_Works()
    {
        // Arrange
        const string source1 = "arr[0] = 5";
        const string source2 = "user.age = 30";

        // Act
        var expr1 = TestHelpers.ParseExpression<BinaryExpression>(source1);
        var expr2 = TestHelpers.ParseExpression<BinaryExpression>(source2);

        // Assert array assignment
        var target1 = Assert.IsType<ArrayAccessExpression>(expr1.Left);
        Assert.Equal("arr", ((IdentifierExpression)target1.Target).Name);
        Assert.Equal(5, ((LiteralExpression)expr1.Right).Value);

        // Assert member assignment
        var target2 = Assert.IsType<MemberAccessExpression>(expr2.Left);
        Assert.Equal("user", ((IdentifierExpression)target2.Object).Name);
        Assert.Equal("age", target2.MemberName);
        Assert.Equal(30, ((LiteralExpression)expr2.Right).Value);
    }

    [Fact]
    public void Parse_UnaryLogicalCombinations_Works()
    {
        // Arrange
        const string source = "!!a && !b";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.AND, expr.Operator.Type);

        var left = Assert.IsType<UnaryExpression>(expr.Left);
        Assert.Equal(TokenType.NOT, left.Operator.Type);
        var leftInner = Assert.IsType<UnaryExpression>(left.Operand);
        Assert.Equal(TokenType.NOT, leftInner.Operator.Type);
        Assert.Equal("a", ((IdentifierExpression)leftInner.Operand).Name);

        var right = Assert.IsType<UnaryExpression>(expr.Right);
        Assert.Equal(TokenType.NOT, right.Operator.Type);
        Assert.Equal("b", ((IdentifierExpression)right.Operand).Name);
    }

    [Fact]
    public void Parse_ComplexNewAndAccess_Works()
    {
        // Arrange
        const string source = "new User()[0].name";

        // Act
        var expr = TestHelpers.ParseExpression<MemberAccessExpression>(source);

        // Assert
        Assert.Equal("name", expr.MemberName);

        var arrayAccess = Assert.IsType<ArrayAccessExpression>(expr.Object);
        var newExpr = Assert.IsType<NewObjectExpression>(arrayAccess.Target);
        Assert.Equal("User", newExpr.ClassName);

        var index = Assert.IsType<LiteralExpression>(arrayAccess.Index);
        Assert.Equal(0, index.Value);
    }

    [Fact]
    public void Parse_BinaryExpression_LeftAssociativity()
    {
        // Arrange
        const string source = "10 - 5 - 2";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert: Структура должна быть ((10 - 5) - 2)
        // Корневой оператор - это второй минус
        Assert.Equal(TokenType.MINUS, expr.Operator.Type);

        // Справа должна быть 2
        var right = Assert.IsType<LiteralExpression>(expr.Right);
        Assert.Equal(2, right.Value);

        // Слева должно быть выражение (10 - 5)
        var left = Assert.IsType<BinaryExpression>(expr.Left);
        Assert.Equal(TokenType.MINUS, left.Operator.Type);

        Assert.Equal(10, Assert.IsType<LiteralExpression>(left.Left).Value);
        Assert.Equal(5, Assert.IsType<LiteralExpression>(left.Right).Value);
    }

    [Fact]
    public void Parse_Precedence_Not_vs_Equal()
    {
        // !a == b должно парситься как (!a) == b, а не !(a == b)

        // Arrange
        const string source = "!a == b";

        // Act
        var expr = TestHelpers.ParseExpression<BinaryExpression>(source);

        // Assert
        Assert.Equal(TokenType.EQUAL, expr.Operator.Type);

        var left = Assert.IsType<UnaryExpression>(expr.Left);
        Assert.Equal(TokenType.NOT, left.Operator.Type);
    }

    [Fact]
    public void Parse_ImmediateAccess_OnNewArray()
    {
        // new int[5][0] — создание и сразу доступ

        // Arrange
        const string source = "new int[5][0]";

        // Act
        var expr = TestHelpers.ParseExpression<ArrayAccessExpression>(source);

        // Assert
        var newArray = Assert.IsType<NewArrayExpression>(expr.Target);
        Assert.Equal("int", newArray.ElementType);

        var index = Assert.IsType<LiteralExpression>(expr.Index);
        Assert.Equal(0, index.Value);
    }

    [Fact]
    public void Parse_ImmediateMethodCall_OnNewObject()
    {
        // Arrange
        const string source = "new User().getName()";

        // Act
        var expr = TestHelpers.ParseExpression<CallExpression>(source);

        // Assert
        var memberAccess = Assert.IsType<MemberAccessExpression>(expr.Callee);
        Assert.Equal("getName", memberAccess.MemberName);

        Assert.IsType<NewObjectExpression>(memberAccess.Object);
    }
}
