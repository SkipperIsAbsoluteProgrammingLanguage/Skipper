using Skipper.BaitCode.Generator;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.Semantic;
using Skipper.VM.Interpreter;
using Skipper.VM.Jit;
using Xunit;

namespace Skipper.VM.Tests;

public static class TestsHelpers
{
    private static readonly Lock ConsoleLock = new();
    public static Value Run(string source)
    {
        var program = Compile(source);
        var runtime = new RuntimeContext();
        var vm = new VirtualMachine(program, runtime);
        return vm.Run("main");
    }

    public static Value Run(BytecodeProgram program)
    {
        var runtime = new RuntimeContext();
        var vm = new VirtualMachine(program, runtime);
        return vm.Run("main");
    }

    public static (Value Result, JitVirtualMachine Vm) RunJit(BytecodeProgram program, int hotThreshold)
    {
        var runtime = new RuntimeContext();
        var vm = new JitVirtualMachine(program, runtime, hotThreshold);
        return (vm.Run("main"), vm);
    }
    
    public static (Value Interpreted, Value Jitted) RunInterpretedAndJit(BytecodeProgram program, int hotThreshold = 1)
    {
        var interp = Run(program);
        var (jit, _) = RunJit(program, hotThreshold);
        return (interp, jit);
    }

    public static BytecodeProgram CreateProgram(List<Instruction> code, List<object>? constants = null)
    {
        var program = new BytecodeProgram();
        if (constants != null)
        {
            program.ConstantPool.AddRange(constants);
        }

        var func = new BytecodeFunction(0, "main", null!, [])
        {
            Code = code
        };

        program.Functions.Add(func);
        return program;
    }

    public static string CaptureOutput(Action action)
    {
        lock (ConsoleLock)
        {
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            try
            {
                action();
                return stringWriter.ToString();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }

    private static BytecodeProgram Compile(string source)
    {
        var lexer = new Lexer.Lexer.Lexer(source);
        var lexerResult = lexer.TokenizeWithDiagnostics();
        Assert.False(lexerResult.HasErrors, "Lexer errors:\n" + string.Join("\n", lexerResult.Diagnostics));

        var parser = new Parser.Parser.Parser(lexerResult.Tokens);
        var parserResult = parser.Parse();
        Assert.False(parserResult.HasErrors, "Parser errors:\n" + string.Join("\n", parserResult.Diagnostics));

        var semantic = new SemanticAnalyzer();
        semantic.VisitProgram(parserResult.Root);
        Assert.False(semantic.HasErrors, "Semantic errors:\n" + string.Join("\n", semantic.Diagnostics));

        var generator = new BytecodeGenerator();
        return generator.Generate(parserResult.Root);
    }
}
