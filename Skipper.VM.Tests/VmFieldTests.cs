using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Xunit;

namespace Skipper.VM.Tests;

public class VmFieldTests
{
    [Fact]
    public void Run_ObjectFields_ReadWriteMultipleFields()
    {
        // Arrange: class Point { int x; int y; }
        List<Instruction> code =
        [
            // p = new Point()
            new(OpCode.NEW_OBJECT, 0), // Stack: [ref]
            new(OpCode.STORE_LOCAL, 0, 0), // Locals[0] = ref

            // p.x = 10
            new(OpCode.LOAD_LOCAL, 0, 0), // [ref]
            new(OpCode.PUSH, 0), // [ref, 10]
            new(OpCode.SET_FIELD, 0, 0), // [classId=0, fieldIdx=0] -> p.x = 10

            // p.y = 20
            new(OpCode.LOAD_LOCAL, 0, 0), // [ref]
            new(OpCode.PUSH, 1), // [ref, 20]
            new(OpCode.SET_FIELD, 0, 1), // [classId=0, fieldIdx=1] -> p.y = 20

            // Calc p.x + p.y
            new(OpCode.LOAD_LOCAL, 0, 0), // [ref]
            new(OpCode.GET_FIELD, 0, 0), // [10]

            new(OpCode.LOAD_LOCAL, 0, 0), // [ref] (стек был [10, ref])
            new(OpCode.GET_FIELD, 0, 1), // [10, 20]

            new(OpCode.ADD), // [30]
            new(OpCode.RETURN)
        ];

        var program = TestsHelpers.CreateProgram(code, [10, 20]);

        var cls = new BytecodeClass(0, "Point");
        cls.Fields.Add("x", new BytecodeClassField(fieldId: 0, type: new PrimitiveType("int")));
        cls.Fields.Add("y", new BytecodeClassField(fieldId: 1, type: new PrimitiveType("int")));

        program.Classes.Add(cls);

        // Act
        var result = TestsHelpers.Run(program);

        // Assert
        Assert.Equal(30, result.AsInt());
    }
}