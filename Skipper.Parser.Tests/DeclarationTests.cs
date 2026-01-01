using Skipper.Parser.AST.Declarations;
using Xunit;

namespace Skipper.Parser.Tests;

public class DeclarationTests
{
    [Fact]
    public void Parse_FunctionDeclaration_Complete()
    {
        const string source = "fn public sum(int a, int b) -> int { return a + b; }";
        var program = TestHelpers.Parse(source);

        Assert.Single(program.Declarations);
        var func = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);

        Assert.Equal("sum", func.Name);
        Assert.Equal("int", func.ReturnType);
        Assert.True(func.IsPublic);

        Assert.Equal(2, func.Parameters.Count);
        Assert.Equal("a", func.Parameters[0].Name);
        Assert.Equal("int", func.Parameters[0].TypeName);
    }

    [Fact]
    public void Parse_ClassDeclaration_WithMembers()
    {
        const string source = """

                                              class Point {
                                                  public int x;
                                                  int y;
                                                  fn print() { }
                                              }
                                          
                              """;
        var program = TestHelpers.Parse(source);

        var cls = Assert.IsType<ClassDeclaration>(program.Declarations[0]);
        Assert.Equal("Point", cls.Name);
        Assert.Equal(3, cls.Members.Count);

        // Поле x
        var fieldX = Assert.IsType<VariableDeclaration>(cls.Members[0]);
        Assert.Equal("x", fieldX.Name);
        Assert.True(fieldX.IsPublic);

        // Поле y
        var fieldY = Assert.IsType<VariableDeclaration>(cls.Members[1]);
        Assert.Equal("y", fieldY.Name);
        Assert.False(fieldY.IsPublic); // по умолчанию false, если не указано

        // Метод print
        var method = Assert.IsType<FunctionDeclaration>(cls.Members[2]);
        Assert.Equal("print", method.Name);
    }

    [Fact]
    public void Parse_MultiDimensionalArrayType_Works()
    {
        const string source = "fn process(int[][] matrix) { }";
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        Assert.Equal("int[][]", func.Parameters[0].TypeName);
    }
}