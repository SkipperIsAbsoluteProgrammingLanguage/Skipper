using Skipper.BaitCode.Objects.Instructions;
using Xunit;

namespace Skipper.VM.Tests;

public class VmArithmeticTests
{
    [Fact]
    public void Run_Add_TwoNumbers_ReturnsSum()
    {
        // Arrange: 10 + 20 = 30
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // 10
            new(OpCode.PUSH, 1), // 20
            new(OpCode.ADD),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [10, 20]);
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(30, result.AsInt());
    }

    [Fact]
    public void Run_ComplexMath_RespectsStackOrder()
    {
        // Arrange: (10 * 2) - 5 = 15
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // 10
            new(OpCode.PUSH, 1), // 2
            new(OpCode.MUL), // 20
            new(OpCode.PUSH, 2), // 5
            new(OpCode.SUB), // 20 - 5 = 15
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [10, 2, 5]);
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(15, result.AsInt());
    }

    [Fact]
    public void Run_Comparison_ReturnsBool()
    {
        // Arrange: 10 > 5 -> true
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // 10
            new(OpCode.PUSH, 1), // 5
            new(OpCode.CMP_GT),
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [10, 5]);
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.True(result.AsBool());
    }

    [Fact]
    public void Run_Variables_StoreAndLoad()
    {
        // Arrange: x = 42; return x;
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // [42]
            new(OpCode.STORE_LOCAL, 0, 0), // Locals[0] = 42 (funcId 0, slot 0)
            new(OpCode.PUSH, 1), // Мусор
            new(OpCode.POP), // Очистка
            new(OpCode.LOAD_LOCAL, 0, 0), // Загрузка Locals[0]
            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [42, 99]);
        var result = TestsHelpers.Run(program);

        // Arrange
        Assert.Equal(42, result.AsInt());
    }

    [Fact]
    public void Run_JumpIfFalse_Branching()
    {
        // Arrange: if (false) return 100 else return 200
        List<Instruction> code =
        [
            new(OpCode.PUSH, 0), // false
            new(OpCode.JUMP_IF_FALSE, 4), // Прыжок на 4 (если false)
            new(OpCode.PUSH, 1), // 100
            new(OpCode.RETURN), // (3)
            new(OpCode.PUSH, 2), // 200 (индекс 4)
            new(OpCode.RETURN) // (5)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [false, 100, 200]);
        var result = TestsHelpers.Run(program);

        // Arrange
        Assert.Equal(200, result.AsInt());
    }
}