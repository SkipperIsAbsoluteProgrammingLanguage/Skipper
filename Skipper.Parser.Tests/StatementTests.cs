using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Xunit;

namespace Skipper.Parser.Tests;

public class StatementTests
{
    [Fact]
    public void Parse_IfStatement_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  if (x > 0) { return 1; } else { return 0; }
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var ifStmt = Assert.IsType<IfStatement>(func.Body.Statements[0]);
        Assert.IsType<BinaryExpression>(ifStmt.Condition);

        var thenBlock = Assert.IsType<BlockStatement>(ifStmt.ThenBranch);
        Assert.IsType<ReturnStatement>(thenBlock.Statements[0]);

        var elseBlock = Assert.IsType<BlockStatement>(ifStmt.ElseBranch);
        Assert.IsType<ReturnStatement>(elseBlock.Statements[0]);
    }

    [Fact]
    public void Parse_IfWithoutElse_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  if (x > 0) { return 1; }
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var ifStmt = Assert.IsType<IfStatement>(func.Body.Statements[0]);
        Assert.Null(ifStmt.ElseBranch);
        Assert.IsType<BlockStatement>(ifStmt.ThenBranch);
    }

    [Fact]
    public void Parse_WhileStatement_Works()
    {
        // Arrange
        const string source = "fn test() { while(true) { x = x + 1; } }";

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var whileStmt = Assert.IsType<WhileStatement>(func.Body.Statements[0]);
        var cond = Assert.IsType<LiteralExpression>(whileStmt.Condition);
        Assert.True((bool)cond.Value);
    }

    [Fact]
    public void Parse_ForStatement_Works()
    {
        // Arrange
        const string source = "fn test() { for (int i = 0; i < 10; i = i + 1) { } }";

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var forStmt = Assert.IsType<ForStatement>(func.Body.Statements[0]);

        var init = Assert.IsType<VariableDeclaration>(forStmt.Initializer);
        Assert.Equal("i", init.Name);

        Assert.IsType<BinaryExpression>(forStmt.Condition);

        Assert.IsType<BinaryExpression>(forStmt.Increment);
    }

    [Fact]
    public void Parse_VariableDeclaration_Works()
    {
        // Arrange
        const string source = "fn test() { int x = 100; }";

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var varDecl = Assert.IsType<VariableDeclaration>(func.Body.Statements[0]);
        Assert.Equal("int", varDecl.TypeName);
        Assert.Equal("x", varDecl.Name);

        var init = Assert.IsType<LiteralExpression>(varDecl.Initializer);
        Assert.Equal(100, init.Value);
    }

    [Fact]
    public void Parse_InfiniteForLoop_Works()
    {
        // Arrange
        const string source = "fn test() { for (;;) { } }";

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var forStmt = Assert.IsType<ForStatement>(func.Body.Statements[0]);

        Assert.Null(forStmt.Initializer);
        Assert.Null(forStmt.Condition);
        Assert.Null(forStmt.Increment);
    }

    [Fact]
    public void Parse_VoidReturn_Works()
    {
        // Arrange
        const string source = "fn test() { return; }";

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var retStmt = Assert.IsType<ReturnStatement>(func.Body.Statements[0]);
        Assert.Null(retStmt.Value);
    }

    [Fact]
    public void Parse_NestedStatements_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  while (running) {
                                      if (stop) { return; }
                                  }
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var whileStmt = Assert.IsType<WhileStatement>(func.Body.Statements[0]);
        var blockInsideWhile = Assert.IsType<BlockStatement>(whileStmt.Body);
        Assert.IsType<IfStatement>(blockInsideWhile.Statements[0]);
    }

    [Fact]
    public void Parse_IfElseIf_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  if (x > 0) { return 1; } else if (x < 0) { return -1; } else { return 0; }
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var firstIf = Assert.IsType<IfStatement>(func.Body.Statements[0]);
        Assert.IsType<BlockStatement>(firstIf.ThenBranch);

        var secondIf = Assert.IsType<IfStatement>(firstIf.ElseBranch);
        Assert.IsType<BlockStatement>(secondIf.ThenBranch);

        var finalElse = Assert.IsType<BlockStatement>(secondIf.ElseBranch);
        Assert.Single(finalElse.Statements);
    }

    [Fact]
    public void Parse_BlockWithMultipleStatements_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  int x = 1;
                                  int y = 2;
                                  x = x + y;
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];
        var block = func.Body;

        // Assert
        Assert.Equal(3, block.Statements.Count);
        Assert.IsType<VariableDeclaration>(block.Statements[0]);
        Assert.IsType<VariableDeclaration>(block.Statements[1]);
        Assert.IsType<ExpressionStatement>(block.Statements[2]);
    }
    
    [Fact]
    public void Parse_TernaryExpression_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  return x > 0 ? 1 : 0;
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var ret = Assert.IsType<ReturnStatement>(func.Body.Statements[0]);
        var ternary = Assert.IsType<TernaryExpression>(ret.Value);

        Assert.IsType<BinaryExpression>(ternary.Condition);
        Assert.IsType<LiteralExpression>(ternary.ThenBranch);
        Assert.IsType<LiteralExpression>(ternary.ElseBranch);
    }
    
    [Fact]
    public void Parse_TernaryInVariableInitializer_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  int x = a > b ? a : b;
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var varDecl = Assert.IsType<VariableDeclaration>(func.Body.Statements[0]);
        var ternary = Assert.IsType<TernaryExpression>(varDecl.Initializer);

        Assert.IsType<BinaryExpression>(ternary.Condition);
        Assert.IsType<IdentifierExpression>(ternary.ThenBranch);
        Assert.IsType<IdentifierExpression>(ternary.ElseBranch);
    }
    
    [Fact]
    public void Parse_NestedTernary_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  return a ? b : c ? d : e;
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var ret = Assert.IsType<ReturnStatement>(func.Body.Statements[0]);
        var outer = Assert.IsType<TernaryExpression>(ret.Value);

        // outer condition: a
        var outerCondition = Assert.IsType<IdentifierExpression>(outer.Condition);
        Assert.Equal("a", outerCondition.Name);

        // outer then: b
        var outerThen = Assert.IsType<IdentifierExpression>(outer.ThenBranch);
        Assert.Equal("b", outerThen.Name);

        // outer else: (c ? d : e)
        var inner = Assert.IsType<TernaryExpression>(outer.ElseBranch);

        // inner condition: c
        var innerCondition = Assert.IsType<IdentifierExpression>(inner.Condition);
        Assert.Equal("c", innerCondition.Name);

        // inner then: d
        var innerThen = Assert.IsType<IdentifierExpression>(inner.ThenBranch);
        Assert.Equal("d", innerThen.Name);

        // inner else: e
        var innerElse = Assert.IsType<IdentifierExpression>(inner.ElseBranch);
        Assert.Equal("e", innerElse.Name);
    }
    
    [Fact]
    public void Parse_AssignmentWithTernary_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  x = cond ? 1 : 2;
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var exprStmt = Assert.IsType<ExpressionStatement>(func.Body.Statements[0]);
        var assign = Assert.IsType<BinaryExpression>(exprStmt.Expression);

        var ternary = Assert.IsType<TernaryExpression>(assign.Right);
        Assert.IsType<LiteralExpression>(ternary.ThenBranch);
        Assert.IsType<LiteralExpression>(ternary.ElseBranch);
    }
    
    [Fact]
    public void Parse_TernaryWithExpressions_Works()
    {
        // Arrange
        const string source = """
                              fn test() {
                                  return x > 0 ? x + 1 : x - 1;
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        var ret = Assert.IsType<ReturnStatement>(func.Body.Statements[0]);
        var ternary = Assert.IsType<TernaryExpression>(ret.Value);

        Assert.IsType<BinaryExpression>(ternary.ThenBranch);
        Assert.IsType<BinaryExpression>(ternary.ElseBranch);
    }
}