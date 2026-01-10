using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class GeneratorTests
{
    // --- 1. Арифметика и Примитивы ---

    [Fact]
    public void Arithmetic_ComplexExpression_GeneratesCorrectOrder()
    {
        // Проверка приоритета операций на уровне байткода (обратная польская запись)
        // 1 + 2 * 3 -> 1, 2, 3, MUL, ADD
        const string code = """
                            fn main() {
                                return 1 + 2 * 3;
                            }
                            """;

        var program = TestHelpers.Generate(code);
        var ops = TestHelpers.GetInstructions(program, "main").Select(i => i.OpCode).ToList();

        // Ожидаем: PUSH, PUSH, PUSH, MUL, ADD, RETURN
        Assert.Equal(OpCode.PUSH, ops[0]);
        Assert.Equal(OpCode.PUSH, ops[1]);
        Assert.Equal(OpCode.PUSH, ops[2]);
        Assert.Equal(OpCode.MUL, ops[3]);
        Assert.Equal(OpCode.ADD, ops[4]);
    }

    [Fact]
    public void Unary_Operators_Work()
    {
        const string code = """
                            fn main() { 
                                return -5;
                            }
                            """;
        var program = TestHelpers.Generate(code);
        var ops = TestHelpers.GetInstructions(program, "main").Select(i => i.OpCode).ToList();

        // PUSH 5, NEG, RETURN
        Assert.Equal(OpCode.PUSH, ops[0]);
        Assert.Equal(OpCode.NEG, ops[1]);
    }

    [Fact]
    public void Logic_Operators_Work()
    {
        const string code = "fn main() { return true && !false; }";
        var program = TestHelpers.Generate(code);
        var ops = TestHelpers.GetInstructions(program, "main").Select(i => i.OpCode).ToList();

        // PUSH true, PUSH false, NOT, AND
        Assert.Equal(OpCode.NOT, ops[2]);
        Assert.Equal(OpCode.AND, ops[3]);
    }

    // --- 2. Переменные и Области видимости ---

    [Fact]
    public void Variables_DeclarationAndAssignment()
    {
        const string code = """
                            fn main() {
                                int x = 10;
                                x = 20;
                                int y = x;
                            }
                            """;
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        // 1. int x = 10 -> PUSH 0, STORE_LOCAL 0, 0 (создание с инициализацией)
        Assert.Equal(OpCode.STORE_LOCAL, inst[1].OpCode);
        var xSlot = (int)inst[1].Operands[1];

        // 2. x = 20 -> PUSH 1, DUP, STORE_LOCAL 0, 0, POP (присваивание копированием)
        Assert.Equal(OpCode.STORE_LOCAL, inst[4].OpCode);
        Assert.Equal(xSlot, inst[4].Operands[1]);

        // 3. int y = x -> LOAD_LOCAL 0, 0 STORE_LOCAL 0, 1 (создание с инициализацией)
        Assert.Equal(OpCode.LOAD_LOCAL, inst[6].OpCode);
        Assert.Equal(xSlot, inst[6].Operands[1]);
        Assert.Equal(OpCode.STORE_LOCAL, inst[7].OpCode);
        var ySlot = (int)inst[7].Operands[1];

        Assert.NotEqual(xSlot, ySlot);
    }

    [Fact]
    public void Variables_Scopes_DoNotOverlapIncorrectly()
    {
        // Проверка корректности работы менеджера слотов
        const string code = """
                            fn main() {
                                {
                                    int b = 2;
                                }
                                int b = 1;
                                {
                                    int b = 2;
                                }
                            }
                            """;
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        var storeOps = inst.Where(i => i.OpCode == OpCode.STORE_LOCAL).ToList();

        // b -> slot 0
        // b -> slot 1
        // b -> slot 2
        Assert.Equal(3, storeOps.Count);
        Assert.NotEqual(storeOps[0].Operands[1], storeOps[1].Operands[1]);
        Assert.NotEqual(storeOps[0].Operands[1], storeOps[2].Operands[1]);
        Assert.NotEqual(storeOps[1].Operands[1], storeOps[2].Operands[1]);
    }

    // --- 3. Управление потоком (Control Flow) ---

    [Fact]
    public void ControlFlow_IfElse_GeneratesCorrectJumps()
    {
        const string code = """
                            fn main() {
                                if (true) {
                                    return 1;
                                } else {
                                    return 0;
                                }
                            }
                            """;
        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        // JUMP_IF_FALSE должен прыгать на начало блока else
        var jumpIfFalse = inst.First(i => i.OpCode == OpCode.JUMP_IF_FALSE);
        var elseLabel = (int)jumpIfFalse.Operands[0];

        // JUMP (в конце if) должен прыгать в конец
        var jump = inst.First(i => i.OpCode == OpCode.JUMP);
        var endLabel = (int)jump.Operands[0];

        Assert.True(elseLabel > 0);
        Assert.True(endLabel > elseLabel);
    }

    [Fact]
    public void ControlFlow_While_GeneratesLoop()
    {
        const string code = """
                            fn main() {
                                while (true) {
                                    int x = 1;
                                }
                            }
                            """;
        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        /*
         * PUSH 0
         * JUMP_IF_FALSE 5
         * PUSH 1
         * STORE_LOCAL 0, 0
         * JUMP 0
         */

        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP_IF_FALSE);
        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP);

        // Прыжок назад должен иметь индекс меньше текущего
        var backJump = inst.Last(i => i.OpCode == OpCode.JUMP);
        var target = (int)backJump.Operands[0];
        var currentIndex = inst.IndexOf(backJump);
        Assert.True(target < currentIndex, "While loop must jump backwards");
    }

    [Fact]
    public void ControlFlow_For_GeneratesLoop()
    {
        const string code = """
                            fn main() {
                                for(int i=0; i<10; i=i+1) {
                                    int x = 1;
                                }
                            }
                            """;
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP_IF_FALSE);
        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP);
    }

    // --- 4. Функции ---

    [Fact]
    public void Function_Call_WithArguments()
    {
        const string code = """
                            fn sum(int a, int b) -> int {
                                return a + b;
                            }
                            fn main() {
                                sum(10, 20);
                            }
                            """;
        var program = TestHelpers.Generate(code);
        var mainInst = TestHelpers.GetInstructions(program, "main");

        // Проверяем вызов
        var callOp = mainInst.First(i => i.OpCode == OpCode.CALL);
        var funcId = (int)callOp.Operands[0];

        Assert.Equal("sum", program.Functions[funcId].Name);

        // Проверяем аргументы внутри sum
        var sumInst = TestHelpers.GetInstructions(program, "sum");
        // LOAD_LOCAL 0, 0; LOAD_LOCAL 0, 1
        var loads = sumInst.Where(i => i.OpCode == OpCode.LOAD_LOCAL).ToList();
        Assert.True(loads.Count >= 2);
        Assert.Equal(0, loads[0].Operands[1]);
        Assert.Equal(1, loads[1].Operands[1]);
    }

    // --- 5. Классы и Объекты (OOP) ---

    [Fact]
    public void Class_Instantiation_GeneratesNewObject()
    {
        // Упадет, если VisitClassDeclaration или VisitNewObject не реализованы
        const string code = """
                            class Point { int x; int y; }
                            fn main() {
                                Point p = new Point();
                            }
                            """;
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        Assert.Contains(inst, i => i.OpCode == OpCode.NEW_OBJECT);
    }

    [Fact]
    public void Class_FieldAccess_GeneratesGetSetField()
    {
        const string code = """
                            class Box { int val; }
                            fn main() {
                                Box b = new Box();
                                b.val = 55;   // SET_FIELD
                                int x = b.val; // GET_FIELD
                                int a = 15;
                                int l = 18;
                                int c = 13;
                                a = l = c;
                            }
                            """;
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        Assert.Contains(inst, i => i.OpCode == OpCode.SET_FIELD);
        Assert.Contains(inst, i => i.OpCode == OpCode.GET_FIELD);
    }

    // --- 6. Массивы ---

    [Fact]
    public void Arrays_CreationAndAccess()
    {
        const string code = """
                            fn main() {
                                int[] arr = new int[5];
                                arr[0] = 1;
                                int x = arr[0];
                            }
                            """;
        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        Assert.Contains(inst, i => i.OpCode == OpCode.NEW_ARRAY);
        Assert.Contains(inst, i => i.OpCode == OpCode.SET_ELEMENT);
        Assert.Contains(inst, i => i.OpCode == OpCode.GET_ELEMENT);
    }

    [Fact]
    public void Class_MethodAccess_TryAccess()
    {
        const string code = """
                            class Box { 
                                fn sum(int a, int b) -> int { 
                                    return a + b;
                                }
                            }

                            fn main() {
                                Box b = new Box();
                                b.sum(1, 2);
                            }
                            """;
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        Assert.Contains(inst, i => i.OpCode == OpCode.CALL_METHOD);
    }

    [Fact]
    public void ControlFlow_TernaryOperator_GeneratesCorrectJumps()
    {
        const string code = """
                            fn main() {
                                return true ? 1 : 0;
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        /*
         * PUSH true
         * JUMP_IF_FALSE else
         * PUSH 1
         * JUMP end
         * else:
         * PUSH 0
         * end:
         * RETURN
         */

        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP_IF_FALSE);
        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP);

        var jif = inst.First(i => i.OpCode == OpCode.JUMP_IF_FALSE);
        var jmp = inst.First(i => i.OpCode == OpCode.JUMP);

        var jifIndex = inst.IndexOf(jif);
        var jmpIndex = inst.IndexOf(jmp);

        Assert.True(jifIndex < jmpIndex, "JUMP_IF_FALSE must occur before JUMP");

        var elseTarget = (int)jif.Operands[0];
        var endTarget = (int)jmp.Operands[0];

        Assert.True(elseTarget > jifIndex, "Else target must be forward");
        Assert.True(endTarget > elseTarget, "End target must be after else");
    }
    
    [Fact]
    public void ExpressionStatement_PopsResult()
    {
        const string code = """
                            fn main() {
                                1 + 2;
                                3 + 4;
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        // после каждого выражения должен быть POP
        var pops = inst.Where(i => i.OpCode == OpCode.POP).ToList();
        Assert.Equal(2, pops.Count);
    }

    [Fact]
    public void Assignment_IsExpression_ReturnsValue()
    {
        const string code = """
                            fn main() {
                                int a;
                                int b;
                                a = b = 10;
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        // должно быть ровно 2 DUP (b=10 и a=...)
        Assert.Equal(2, inst.Count(i => i.OpCode == OpCode.DUP));
    }
    
    [Fact]
    public void ConstantPool_DoesNotDuplicateSameLiteral()
    {
        const string code = """
                            fn main() {
                                int a = 5;
                                int b = 5;
                                return 5;
                            }
                            """;

        var program = TestHelpers.Generate(code);

        var values = program.ConstantPool.Where(v => (int)v == 5).ToList();
        Assert.Single(values);
    }

    [Fact]
    public void Push_OperandPointsToConstantPool()
    {
        const string code = "fn main() { return 42; }";

        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        var push = inst.First(i => i.OpCode == OpCode.PUSH);
        var id = (int)push.Operands[0];

        Assert.InRange(id, 0, program.ConstantPool.Count - 1);
        Assert.Equal(42, program.ConstantPool[id]);
    }

    [Fact]
    public void Generator_IsDeterministic()
    {
        const string code = "fn main() { return 1 + 2; }";

        var p1 = TestHelpers.Generate(code);
        var p2 = TestHelpers.Generate(code);

        var i1 = TestHelpers.GetInstructions(p1, "main");
        var i2 = TestHelpers.GetInstructions(p2, "main");

        Assert.Equal(i1.Count, i2.Count);

        for (var i = 0; i < i1.Count; i++)
        {
            Assert.Equal(i1[i].OpCode, i2[i].OpCode);
            Assert.Equal(i1[i].Operands, i2[i].Operands);
        }
    }

    [Fact]
    public void Return_IsLastInstruction()
    {
        const string code = """
                            fn main() {
                                int x = 1;
                                return x;
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        Assert.Equal(OpCode.RETURN, inst.Last().OpCode);
    }

    [Fact]
    public void VoidReturn_HasNoValue()
    {
        const string code = "fn main() { return; }";

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");
        var ret = inst.Last();

        Assert.Empty(ret.Operands);
    }

    [Fact]
    public void Call_PushesArgumentsBeforeCall()
    {
        const string code = """
                            fn f(int a, int b) {}
                            fn main() { f(1, 2); }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        var callIndex = inst.FindIndex(i => i.OpCode == OpCode.CALL);

        Assert.Equal(OpCode.PUSH, inst[callIndex - 2].OpCode);
        Assert.Equal(OpCode.PUSH, inst[callIndex - 1].OpCode);
    }

    [Fact]
    public void ArrayAssignment_OrderIsCorrect()
    {
        const string code = """
                            fn main() {
                                int[] a = new int[3];
                                a[1] = 10;
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        var set = inst.First(i => i.OpCode == OpCode.SET_ELEMENT);
        var idx = inst.IndexOf(set);

        Assert.Equal(OpCode.DUP, inst[idx - 1].OpCode);
    }

    [Fact]
    public void Snapshot_SimpleFunction()
    {
        const string code = "fn main() { return 1 + 2; }";

        var inst = TestHelpers.GetInstructions(
            TestHelpers.Generate(code),
            "main"
        );

        var snapshot = inst.Select(i => i.ToString());

        Assert.Equal([
            "PUSH 0",
            "PUSH 1",
            "ADD",
            "RETURN"
        ], snapshot);
    }
    
    [Fact]
    public void DeepScopes_DoNotReuseSlots()
    {
        const string code = """
                            fn main() {
                                { { { int x = 1; } } }
                                { int x = 2; }
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");
        var slots = inst
            .Where(i => i.OpCode == OpCode.STORE_LOCAL)
            .Select(i => (int)i.Operands[1])
            .ToList();

        Assert.Equal(slots.Count, slots.Distinct().Count());
    }

    [Fact]
    public void ArrayType_IsRegisteredCorrectly()
    {
        const string code = """
                            fn main() {
                                int[] a = new int[5];
                            }
                            """;

        var program = TestHelpers.Generate(code);
        Assert.Contains(program.Types, t => t is ArrayType);
    }

    [Fact]
    public void MethodCall_PushesObjectBeforeArguments()
    {
        const string code = """
                            class A {
                                fn f(int x) -> int { return x; }
                            }
                            fn main() {
                                A a = new A();
                                a.f(10);
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        var callIdx = inst.FindIndex(i => i.OpCode == OpCode.CALL_METHOD);

        // перед CALL_METHOD должен быть PUSH аргумента
        Assert.Equal(OpCode.PUSH, inst[callIdx - 1].OpCode);
    }

    [Fact]
    public void While_HasSingleBackwardJump()
    {
        const string code = """
                            fn main() {
                                while (true) { }
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        var jumps = inst.Where(i => i.OpCode == OpCode.JUMP).ToList();
        Assert.Single(jumps);

        var idx = inst.IndexOf(jumps[0]);
        var target = (int)jumps[0].Operands[0];

        Assert.True(target < idx);
    }

    [Fact]
    public void IfWithoutElse_HasNoUnconditionalJump()
    {
        const string code = """
                            fn main() {
                                if (true) {
                                    int x = 1;
                                }
                            }
                            """;

        var inst = TestHelpers.GetInstructions(TestHelpers.Generate(code), "main");

        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP_IF_FALSE);
        Assert.DoesNotContain(inst, i => i.OpCode == OpCode.JUMP);
    }

    
}