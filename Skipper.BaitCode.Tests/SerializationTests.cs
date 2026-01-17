using Skipper.BaitCode.Writer;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class SerializationTests
{
    [Fact]
    public void Serialization_FullCycle_PreservesData()
    {
        // Arrange
        const string code = """
                            class User {
                                int id;
                            }
                            fn add(int a, int b) -> int {
                                return a + b;
                            }
                            fn main() { 
                                int x = 10;
                                User u = new User();
                            }
                            """;

        var originalProgram = TestHelpers.Generate(code);
        var tempFile = Path.GetTempFileName();
        var writer = new BytecodeWriter(originalProgram);

        try
        {
            // Act
            writer.SaveToFile(tempFile);
            var loadedProgram = BytecodeWriter.LoadFromFile(tempFile);

            // Assert

            // 1. Проверяем функции (main, add)
            Assert.NotEmpty(loadedProgram.Functions);
            Assert.Equal(originalProgram.Functions.Count, loadedProgram.Functions.Count);

            var mainOrig = originalProgram.Functions.First(f => f.Name == "main");
            var mainLoaded = loadedProgram.Functions.First(f => f.Name == "main");

            Assert.Equal(mainOrig.Code.Count, mainLoaded.Code.Count);
            Assert.Equal(mainOrig.Code[0].OpCode, mainLoaded.Code[0].OpCode);

            // 2. Проверяем типы и классы
            Assert.NotEmpty(loadedProgram.Types);
            Assert.NotEmpty(loadedProgram.Classes);
            Assert.Equal("User", loadedProgram.Classes[0].Name);

            // 3. Проверяем константы
            Assert.Equal(originalProgram.ConstantPool.Count, loadedProgram.ConstantPool.Count);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Serialization_ArrayTypes_RestoredCorrectly()
    {
        // Arrange
        const string code = "fn main() { int[] arr; }";
        var original = TestHelpers.Generate(code);
        var tempFile = Path.GetTempFileName();

        try
        {
            new BytecodeWriter(original).SaveToFile(tempFile);
            var loaded = BytecodeWriter.LoadFromFile(tempFile);

            // Assert
            // Ищем тип массива в таблице типов
            var arrayType = loaded.Types.OfType<Types.ArrayType>().FirstOrDefault();
            Assert.NotNull(arrayType);

            // Проверяем вложенный тип
            var elemType = arrayType.ElementType as Types.PrimitiveType;
            Assert.NotNull(elemType);
            Assert.Equal("int", elemType.Name);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Serialization_ComplexInstructions_RestoredCorrectly()
    {
        // Arrange: CALL и JUMP (инструкции с операндами)
        const string code = """
                            fn foo() {}
                            fn main() { 
                                foo();      // CALL func_id
                                if (true) {} // JUMP_IF_FALSE
                            }
                            """;
        var original = TestHelpers.Generate(code);
        var tempFile = Path.GetTempFileName();

        try
        {
            new BytecodeWriter(original).SaveToFile(tempFile);
            var loaded = BytecodeWriter.LoadFromFile(tempFile);

            var mainFunc = loaded.Functions.First(f => f.Name == "main");

            // Проверяем CALL
            var callInstr = mainFunc.Code.FirstOrDefault(i => i.OpCode == Objects.Instructions.OpCode.CALL);
            Assert.NotNull(callInstr);
            Assert.NotEmpty(callInstr.Operands); // Операнд (ID функции) должен быть

            // Проверяем JUMP
            var jumpInstr = mainFunc.Code.FirstOrDefault(i => i.OpCode == Objects.Instructions.OpCode.JUMP_IF_FALSE);
            Assert.NotNull(jumpInstr);

            // Нюанс JSON: Числа восстанавливаются как JsonElement. Проверяем, что операнд читаем.
            var op0 = jumpInstr.Operands[0];
            Assert.True(int.TryParse(op0.ToString(), out _), $"Operand {op0} should be parseable as int");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Serialization_EmptyProgram_DoesNotCrash()
    {
        // Arrange
        const string code = "fn main() {}";
        var original = TestHelpers.Generate(code);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            new BytecodeWriter(original).SaveToFile(tempFile);
            var loaded = BytecodeWriter.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(loaded);
            Assert.Single(loaded.Functions); // Только main
            Assert.Empty(loaded.Classes);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}