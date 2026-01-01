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
        Assert.Equal(100L, init.Value);
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
}