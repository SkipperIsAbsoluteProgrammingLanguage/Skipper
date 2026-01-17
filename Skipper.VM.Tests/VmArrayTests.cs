using Skipper.BaitCode.Objects.Instructions;
using Xunit;

namespace Skipper.VM.Tests;

public class VmArrayTests
{
    [Fact]
    public void Run_ArrayOperations_ReadWrite()
    {
        // Логика теста (упрощенная без Length):
        // int size = 5;
        // int[] arr = new int[size];
        // arr[1] = 42;
        // return arr[1]; // 42

        // Arrange
        List<Instruction> code =
        [
            // 1. Создаем массив
            new(OpCode.PUSH, 0), // Stack: [5]
            new(OpCode.NEW_ARRAY), // Stack: [ArrRef]
            new(OpCode.STORE_LOCAL, 0, 0), // Locals[0] = ArrRef

            // 2. arr[1] = 42
            // Порядок стека для SET_ELEMENT: ArrRef, Index, Value
            new(OpCode.LOAD_LOCAL, 0, 0), // Stack: [ArrRef]
            new(OpCode.PUSH, 1), // Stack: [ArrRef, 1]
            new(OpCode.PUSH, 2), // Stack: [ArrRef, 1, 42]
            new(OpCode.SET_ELEMENT), // Stack: []

            // 3. Читаем arr[1]
            new(OpCode.LOAD_LOCAL, 0, 0), // Stack: [ArrRef]
            new(OpCode.PUSH, 1), // Stack: [ArrRef, 1]
            new(OpCode.GET_ELEMENT), // Stack: [42]

            new(OpCode.RETURN)
        ];

        // Act
        var program = TestsHelpers.CreateProgram(code, [
            5, // idx 0: size
            1, // idx 1: index
            42 // idx 2: value
        ]);
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(42, result.AsInt());
    }
}