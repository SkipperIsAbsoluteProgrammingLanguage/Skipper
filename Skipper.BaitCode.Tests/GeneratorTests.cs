using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class GeneratorTests
{
    // Хелпер для генерации
    private static BytecodeProgram Generate(string source)
    {
        var lexer = new Lexer.Lexer.Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser.Parser.Parser(tokens);
        var result = parser.Parse();

        if (result.HasErrors)
            throw new Exception($"Parser errors: {string.Join(", ", result.Diagnostics.Select(d => d.Message))}");

        var generator = new BytecodeGenerator();
        return generator.Generate(result.Root);
    }

    private static List<Instruction> GetInstructions(BytecodeProgram program, string funcName)
    {
        var func = program.Functions.FirstOrDefault(f => f.Name == funcName);
        Assert.NotNull(func);
        return func!.Code;
    }

    // --- 1. Арифметика и Примитивы ---

    [Fact]
    public void Arithmetic_ComplexExpression_GeneratesCorrectOrder()
    {
        // Проверка приоритета операций на уровне байткода (обратная польская запись)
        // 1 + 2 * 3 -> 1, 2, 3, MUL, ADD
        const string code = "fn main() { return 1 + 2 * 3; }";

        var program = Generate(code);
        var ops = GetInstructions(program, "main").Select(i => i.OpCode).ToList();

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
        const string code = "fn main() { return -5; }";
        var program = Generate(code);
        var ops = GetInstructions(program, "main").Select(i => i.OpCode).ToList();

        // PUSH 5, NEG, RETURN
        Assert.Equal(OpCode.PUSH, ops[0]);
        Assert.Equal(OpCode.NEG, ops[1]);
    }

    [Fact]
    public void Logic_Operators_Work()
    {
        const string code = "fn main() { return true && !false; }";
        var program = Generate(code);
        var ops = GetInstructions(program, "main").Select(i => i.OpCode).ToList();

        // PUSH true, PUSH false, NOT, AND
        Assert.Contains(OpCode.NOT, ops);
        Assert.Contains(OpCode.AND, ops);
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
        var program = Generate(code);
        var inst = GetInstructions(program, "main");

        // 1. int x = 10 -> PUSH 10, STORE 0
        Assert.Equal(OpCode.STORE, inst[1].OpCode);
        var xSlot = (int)inst[1].Operands[0];

        // 2. x = 20 -> PUSH 20, STORE 0 (тот же слот)
        Assert.Equal(OpCode.STORE, inst[3].OpCode);
        Assert.Equal(xSlot, inst[3].Operands[0]);

        // 3. int y = x -> LOAD 0, STORE 1 (новый слот)
        Assert.Equal(OpCode.LOAD, inst[4].OpCode);
        Assert.Equal(xSlot, inst[4].Operands[0]);
        Assert.Equal(OpCode.STORE, inst[5].OpCode);
        var ySlot = (int)inst[5].Operands[0];

        Assert.NotEqual(xSlot, ySlot);
    }

    [Fact]
    public void Variables_Scopes_DoNotOverlapIncorrectly()
    {
        // Проверка корректности работы менеджера слотов
        const string code = """
                            fn main() {
                                int a = 1;
                                {
                                    int b = 2;
                                }
                                int c = 3; 
                            }
                            """;
        var program = Generate(code);
        var inst = GetInstructions(program, "main");

        var storeOps = inst.Where(i => i.OpCode == OpCode.STORE).ToList();

        // a -> slot 0
        // b -> slot 1
        // c -> slot 2 (или 1, если оптимизатор слотов реализован, но в текущем коде просто инкремент)
        // Главное, чтобы слоты были валидными числами
        Assert.Equal(3, storeOps.Count);
        Assert.True((int)storeOps[0].Operands[0] >= 0);
        Assert.True((int)storeOps[1].Operands[0] > (int)storeOps[0].Operands[0]);
    }

    // --- 3. Управление потоком (Control Flow) ---

    [Fact]
    public void ControlFlow_IfElse_GeneratesCorrectJumps()
    {
        const string code = """
                            fn main() {
                                if (true) { return 1; } else { return 0; }
                            }
                            """;
        var inst = GetInstructions(Generate(code), "main");

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
        // Этот тест упадет, если VisitWhileStatement останется TODO
        const string code = """
                            fn main() {
                                while (true) {
                                    int x = 1;
                                }
                            }
                            """;
        var inst = GetInstructions(Generate(code), "main");

        // Ожидаем:
        // LABEL_START:
        // Condition...
        // JUMP_IF_FALSE -> END
        // Body...
        // JUMP -> LABEL_START
        // END:

        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP_IF_FALSE);
        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP); // Прыжок назад

        // Прыжок назад должен иметь индекс меньше текущего
        var backJump = inst.Last(i => i.OpCode == OpCode.JUMP);
        var target = (int)backJump.Operands[0];
        var currentIndex = inst.IndexOf(backJump);
        Assert.True(target < currentIndex, "While loop must jump backwards");
    }

    [Fact]
    public void ControlFlow_For_GeneratesLoop()
    {
        // Этот тест упадет, если VisitForStatement останется TODO
        const string code = "fn main() { for(int i=0; i<10; i=i+1) {} }";
        var inst = GetInstructions(Generate(code), "main");

        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP_IF_FALSE);
        Assert.Contains(inst, i => i.OpCode == OpCode.JUMP);
    }

    // --- 4. Функции ---

    [Fact]
    public void Function_Call_WithArguments()
    {
        const string code = """
                            fn sum(int a, int b) -> int { return a + b; }
                            fn main() { sum(10, 20); }
                            """;
        var program = Generate(code);
        var mainInst = GetInstructions(program, "main");

        // Проверяем вызов
        var callOp = mainInst.First(i => i.OpCode == OpCode.CALL);
        var funcId = (int)callOp.Operands[0];

        Assert.Equal("sum", program.Functions[funcId].Name);

        // Проверяем аргументы внутри sum
        var sumInst = GetInstructions(program, "sum");
        // LOAD 0 (a), LOAD 1 (b)
        var loads = sumInst.Where(i => i.OpCode == OpCode.LOAD).ToList();
        Assert.True(loads.Count >= 2);
        Assert.Equal(0, loads[0].Operands[0]);
        Assert.Equal(1, loads[1].Operands[0]);
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
        var program = Generate(code);
        var inst = GetInstructions(program, "main");

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
                            }
                            """;
        var program = Generate(code);
        var inst = GetInstructions(program, "main");

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
        var inst = GetInstructions(Generate(code), "main");

        Assert.Contains(inst, i => i.OpCode == OpCode.NEW_ARRAY);
        Assert.Contains(inst, i => i.OpCode == OpCode.SET_ELEMENT);
        Assert.Contains(inst, i => i.OpCode == OpCode.GET_ELEMENT);
    }
}