using Skipper.Parser.AST.Declarations;
using Xunit;

namespace Skipper.Parser.Tests;

public class DeclarationTests
{
    [Fact]
    public void Parse_FunctionDeclaration_Complete()
    {
        // Arrange
        const string source = "fn public sum(int a, int b) -> int { return a + b; }";

        // Act
        var program = TestHelpers.Parse(source);

        // Assert
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
    public void Parse_Function_WithLongTypes_Works()
    {
        // Arrange
        const string source = "fn sum(long a, long b) -> long { return a + b; }";

        // Act
        var program = TestHelpers.Parse(source);

        // Assert
        var func = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        Assert.Equal("long", func.ReturnType);
        Assert.Equal(2, func.Parameters.Count);
        Assert.Equal("long", func.Parameters[0].TypeName);
        Assert.Equal("long", func.Parameters[1].TypeName);
    }

    [Fact]
    public void Parse_ClassDeclaration_WithMembers()
    {
        // Arrange
        const string source = """
                              class Point {
                                  public int x;
                                  int y;
                                  fn print() { }
                              }
                              """;

        // Act
        var program = TestHelpers.Parse(source);

        // Assert
        var cls = Assert.IsType<ClassDeclaration>(program.Declarations[0]);
        Assert.Equal("Point", cls.Name);
        Assert.Equal(3, cls.Members.Count);

        var fieldX = Assert.IsType<VariableDeclaration>(cls.Members[0]);
        Assert.Equal("x", fieldX.Name);
        Assert.True(fieldX.IsPublic);

        var fieldY = Assert.IsType<VariableDeclaration>(cls.Members[1]);
        Assert.Equal("y", fieldY.Name);
        Assert.False(fieldY.IsPublic);

        var method = Assert.IsType<FunctionDeclaration>(cls.Members[2]);
        Assert.Equal("print", method.Name);
    }

    [Fact]
    public void Parse_MultiDimensionalArrayType_Works()
    {
        // Arrange
        const string source = "fn process(int[][] matrix) { }";

        // Act
        var program = TestHelpers.Parse(source);

        // Assert
        var func = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        Assert.Equal("int[][]", func.Parameters[0].TypeName);
    }

    [Fact]
    public void Parse_Function_NoParameters_VoidReturn()
    {
        // Arrange
        const string source = "fn run() { }";

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        Assert.Equal("run", func.Name);
        Assert.Equal("void", func.ReturnType); // Предполагаем дефолт void, если парсер это ставит
        Assert.Empty(func.Parameters);
    }

    [Fact]
    public void Parse_Function_ReturningArray()
    {
        // Arrange
        const string source = "fn getMatrix() -> int[][] { }";

        // Act
        var program = TestHelpers.Parse(source);
        var func = (FunctionDeclaration)program.Declarations[0];

        // Assert
        Assert.Equal("int[][]", func.ReturnType);
    }
}
