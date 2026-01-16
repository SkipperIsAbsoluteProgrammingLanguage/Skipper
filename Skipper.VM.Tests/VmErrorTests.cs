using Skipper.BaitCode.Objects.Instructions;
using Xunit;

namespace Skipper.VM.Tests;

public class ErrorTests
{
    [Fact]
    public void Run_DivisionByZero_ThrowsException()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // 10
            new(OpCode.PUSH, 1), // 0
            new(OpCode.DIV),
            new(OpCode.RETURN)
        ];

        // Act & Assert
        var program = TestsHelpers.CreateProgram(code, [10, 0]);
        Assert.Throws<DivideByZeroException>(() => TestsHelpers.Run(program));
    }

    [Fact]
    public void Run_NullReference_ThrowsException()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // Загрузка null
            new(OpCode.GET_FIELD, 0, 0), // Попытка чтения поля у null
            new(OpCode.RETURN)
        ];

        // Act  & Assert
        var program = TestsHelpers.CreateProgram(code, [null!]);
        Assert.Throws<NullReferenceException>(() => TestsHelpers.Run(program));
    }

    [Fact]
    public void Run_ArrayIndexOutOfBounds_ThrowsException()
    {
        // Arrange
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // Размер 2
            new(OpCode.NEW_ARRAY),
            new(OpCode.PUSH, 1), // Индекс 5
            new(OpCode.GET_ELEMENT), // Выход за границы массива
            new(OpCode.RETURN)
        ];

        // Act  & Assert
        var program = TestsHelpers.CreateProgram(code, [2, 5]);
        Assert.Throws<IndexOutOfRangeException>(() => TestsHelpers.Run(program));
    }
}
