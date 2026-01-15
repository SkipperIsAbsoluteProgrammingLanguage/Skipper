using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Parser.AST;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.VM.Tests;

public class NativeApiTests
{
    // Compiler Tests (Bytecode Generation)

    [Fact]
    public void Generator_Print_EmitsCallNative0()
    {
        const string code = "fn main() { print(\"Hello\"); }";
        var (program, _) = Compile(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(0, Convert.ToInt32(instr.Operands[0]));
    }

    [Fact]
    public void Generator_Time_EmitsCallNative1()
    {
        const string code = "fn main() { int t = time(); }";
        var (program, _) = Compile(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(1, Convert.ToInt32(instr.Operands[0]));
    }

    [Fact]
    public void Generator_Random_EmitsCallNative2()
    {
        const string code = "fn main() { int r = random(100); }";
        var (program, _) = Compile(code);
        var mainFunc = program.Functions.First(f => f.Name == "main");

        var instr = mainFunc.Code.FirstOrDefault(i => i.OpCode == OpCode.CALL_NATIVE);

        Assert.NotNull(instr);
        Assert.Equal(2, Convert.ToInt32(instr.Operands[0]));
    }

    // Runtime Tests: Basic Functionality

    [Fact]
    public void VM_Print_WritesToConsole()
    {
        var output = CaptureOutput(() =>
        {
            const string code = """
                                fn main() {
                                    print(12345);
                                    print("TestMessage");
                                }
                                """;
            RunScript(code);
        });

        Assert.Contains("12345", output);
        Assert.Contains("TestMessage", output);
    }

    [Fact]
    public void Integration_PrintCalculationResult()
    {
        var output = CaptureOutput(() =>
        {
            const string code = """
                                fn main() {
                                    int a = 10;
                                    int b = 20;
                                    print(a + b);
                                }
                                """;
            RunScript(code);
        });

        Assert.Contains("30", output);
    }

    [Fact]
    public void VM_Time_ReturnsValidTimestamp()
    {
        const string code = "fn main() { return time(); }";
        var result = RunScript(code);

        Assert.Equal(ValueKind.Int, result.Kind);
        Assert.True(result.AsInt() >= 0);
    }

    [Fact]
    public void VM_Time_IncreasesDuringExecution()
    {
        const string code = """
                            fn main() {
                                int start = time();
                                int sum = 0;
                                for(int i=0; i<1000; i=i+1) { sum = sum + 1; }
                                int end = time();
                                return end - start;
                            }
                            """;
        var result = RunScript(code);
        Assert.True(result.AsInt() >= 0);
    }

    [Fact]
    public void VM_Random_ReturnsValueInRange()
    {
        const string code = "fn main() { return random(10); }";

        for (var i = 0; i < 50; i++)
        {
            var result = RunScript(code);
            Assert.InRange(result.AsInt(), 0, 9);
        }
    }

    // Runtime Tests: Data Types

    [Fact]
    public void VM_Print_Booleans()
    {
        var output = CaptureOutput(() =>
        {
            const string code = """
                                fn main() {
                                    print(true);
                                    print(false);
                                }
                                """;
            RunScript(code);
        });

        Assert.Contains("True", output);
        Assert.Contains("False", output);
    }

    [Fact]
    public void VM_Print_Doubles_And_Negatives()
    {
        var output = CaptureOutput(() =>
        {
            const string code = """
                                fn main() {
                                    print(3.1415);
                                    print(-100);
                                    print(-0.5);
                                }
                                """;
            RunScript(code);
        });

        Assert.Contains("3.1415", output);
        Assert.Contains("-100", output);
        Assert.Contains("-0.5", output);
    }

    [Fact]
    public void VM_Print_EmptyString()
    {
        var output = CaptureOutput(() =>
        {
            RunScript("fn main() { print(\"\"); }");
        });
        Assert.NotNull(output);
    }

    // Runtime Tests: Stability & Edge Cases

    [Fact]
    public void VM_NativeCall_StackBalance()
    {
        var output = CaptureOutput(() =>
        {
            const string code = """
                                fn main() {
                                    for (int i = 0; i < 100; i = i + 1) {
                                        print(i);
                                    }
                                    print("Done");
                                }
                                """;
            RunScript(code);
        });

        Assert.Contains("99", output);
        Assert.Contains("Done", output);
    }

    [Fact]
    public void VM_Print_Null_FromUninitializedField()
    {
        var output = CaptureOutput(() =>
        {
            const string code = """
                                class Container { Container inner; }
                                fn main() {
                                    Container c = new Container();
                                    print(c.inner);
                                }
                                """;
            RunScript(code);
        });

        Assert.Contains("0", output);
    }

    [Fact]
    public void VM_Print_Object_DoesNotCrash()
    {
        var output = CaptureOutput(() =>
        {
            const string code = """
                                class Box { int x; }
                                fn main() {
                                    Box b = new Box();
                                    print(b);
                                    print("Done");
                                }
                                """;
            RunScript(code);
        });

        Assert.Contains("Done", output);
    }

    // Helpers

    private Value RunScript(string source)
    {
        var (program, _) = Compile(source);
        var runtime = new RuntimeContext();
        var vm = new VirtualMachine(program, runtime);
        return vm.Run("main");
    }

    [Fact]
    public void VM_StringConcatenation_PrintsCombinedString()
    {
        var output = CaptureOutput(() =>
        {
            // "Hello " + "World" -> Должно создать новую строку
            const string code = """
                                fn main() {
                                    print("Hello " + "World");
                                }
                                """;
            RunScript(code);
        });

        Assert.Contains("Hello World", output);
    }

    private (BytecodeProgram Program, ProgramNode AST) Compile(string source)
    {
        var lexer = new Lexer.Lexer.Lexer(source);
        var tokens = lexer.Tokenize();

        var parser = new Parser.Parser.Parser(tokens);
        var parseResult = parser.Parse();

        if (parseResult.HasErrors)
        {
            throw new Exception($"Parse errors: {string.Join(", ", parseResult.Diagnostics.Select(d => d.Message))}");
        }

        var generator = new BytecodeGenerator();
        return (generator.Generate(parseResult.Root), parseResult.Root);
    }

    private static string CaptureOutput(Action action)
    {
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            action();
            return stringWriter.ToString();
        } finally
        {
            Console.SetOut(originalOut);
        }
    }
}