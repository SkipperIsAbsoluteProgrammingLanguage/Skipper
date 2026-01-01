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
        const string source = """

                                              fn test() {
                                                  if (x > 0) { return 1; } else { return 0; }
                                              }
                                          
                              """;
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        var ifStmt = Assert.IsType<IfStatement>(func.Body.Statements[0]);

        // Condition
        Assert.IsType<BinaryExpression>(ifStmt.Condition);

        // Then
        var thenBlock = Assert.IsType<BlockStatement>(ifStmt.ThenBranch);
        Assert.IsType<ReturnStatement>(thenBlock.Statements[0]);

        // Else
        var elseBlock = Assert.IsType<BlockStatement>(ifStmt.ElseBranch);
        Assert.IsType<ReturnStatement>(elseBlock.Statements[0]);
    }

    [Fact]
    public void Parse_WhileStatement_Works()
    {
        const string source = "fn test() { while(true) { x = x + 1; } }";
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        var whileStmt = Assert.IsType<WhileStatement>(func.Body.Statements[0]);
        var cond = Assert.IsType<LiteralExpression>(whileStmt.Condition);
        Assert.True((bool)cond.Value);
    }

    [Fact]
    public void Parse_ForStatement_Works()
    {
        // for (int i = 0; i < 10; i = i + 1) ...
        const string source = "fn test() { for (int i = 0; i < 10; i = i + 1) { } }";
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        var forStmt = Assert.IsType<ForStatement>(func.Body.Statements[0]);

        // Init
        var init = Assert.IsType<VariableDeclaration>(forStmt.Initializer);
        Assert.Equal("i", init.Name);

        // Condition
        Assert.IsType<BinaryExpression>(forStmt.Condition);

        // Increment
        Assert.IsType<BinaryExpression>(forStmt.Increment);
    }

    [Fact]
    public void Parse_VariableDeclaration_Works()
    {
        const string source = "fn test() { int x = 100; }";
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        var varDecl = Assert.IsType<VariableDeclaration>(func.Body.Statements[0]);
        Assert.Equal("int", varDecl.TypeName);
        Assert.Equal("x", varDecl.Name);

        var init = Assert.IsType<LiteralExpression>(varDecl.Initializer);
        Assert.Equal(100L, init.Value);
    }

    [Fact]
    public void Parse_InfiniteForLoop_Works()
    {
        // for (;;) { } - валидная конструкция
        const string source = "fn test() { for (;;) { } }";
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];
        var forStmt = Assert.IsType<ForStatement>(func.Body.Statements[0]);

        Assert.Null(forStmt.Initializer);
        Assert.Null(forStmt.Condition);
        Assert.Null(forStmt.Increment);
    }

    [Fact]
    public void Parse_VoidReturn_Works()
    {
        // return; (без значения)
        const string source = "fn test() { return; }";
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];
        var retStmt = Assert.IsType<ReturnStatement>(func.Body.Statements[0]);

        Assert.Null(retStmt.Value);
    }

    [Fact]
    public void Parse_NestedStatements_Works()
    {
        // Проверка вложенности: if внутри while
        const string source = """

                                      fn test() {
                                          while (running) {
                                              if (stop) { return; }
                                          }
                                      }
                                  
                              """;
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        var whileStmt = Assert.IsType<WhileStatement>(func.Body.Statements[0]);
        var blockInsideWhile = Assert.IsType<BlockStatement>(whileStmt.Body);
        Assert.IsType<IfStatement>(blockInsideWhile.Statements[0]);
    }
}