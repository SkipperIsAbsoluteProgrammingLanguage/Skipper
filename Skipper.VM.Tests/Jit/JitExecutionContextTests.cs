using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Execution;
using Skipper.VM.Jit;
using Xunit;

namespace Skipper.VM.Tests.Jit;

public class JitExecutionContextTests
{
    private static JitExecutionContext CreateContext(BytecodeProgram program, RuntimeContext runtime, int hotThreshold = 1, bool trace = false)
    {
        var compiler = new BytecodeJitCompiler();
        return new JitExecutionContext(program, runtime, compiler, hotThreshold, trace);
    }

    [Fact]
    public void Stack_Underflow_Throws()
    {
        // Arrange
        var ctx = CreateContext(new BytecodeProgram(), new RuntimeContext());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ctx.PopStack());
        Assert.Throws<InvalidOperationException>(() => ctx.PeekStack());
    }

    [Fact]
    public void LocalAccess_WithoutLocals_Throws()
    {
        // Arrange
        var ctx = CreateContext(new BytecodeProgram(), new RuntimeContext());
        IInterpreterContext ic = ctx;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ic.LoadLocal(0));
        Assert.Throws<InvalidOperationException>(() => ic.StoreLocal(0, Value.FromInt(1)));
    }

    [Fact]
    public void InvalidFunctionMethodClassIds_Throw()
    {
        // Arrange
        var program = new BytecodeProgram();
        var ctx = CreateContext(program, new RuntimeContext());
        IInterpreterContext ic = ctx;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ic.CallFunction(999));
        Assert.Throws<InvalidOperationException>(() => ic.CallMethod(0, 999));
        Assert.Throws<InvalidOperationException>(() => ic.GetClassById(999));
    }

    [Fact]
    public void EnsureStackCapacity_Grows()
    {
        // Arrange
        var ctx = CreateContext(new BytecodeProgram(), new RuntimeContext());

        // Act
        for (var i = 0; i < 600; i++)
        {
            ctx.PushStack(Value.FromInt(i));
        }

        // Assert
        Assert.Equal(600, ctx.StackCount);
    }

    [Fact]
    public void EnsureStackCapacity_GrowsBeyondDouble()
    {
        // Arrange
        var ctx = CreateContext(new BytecodeProgram(), new RuntimeContext());
        var stackField = typeof(JitExecutionContext).GetField("_evalStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ensureMethod = typeof(JitExecutionContext).GetMethod("EnsureStackCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var initial = ((Value[])stackField!.GetValue(ctx)!).Length;
        var needed = initial * 2 + 5;

        // Act
        _ = ensureMethod!.Invoke(ctx, [needed]);
        var resized = ((Value[])stackField.GetValue(ctx)!).Length;

        // Assert
        Assert.Equal(needed, resized);
    }

    [Fact]
    public void Trace_LogsJitCompileAndExecute()
    {
        // Arrange
        var program = new BytecodeProgram();
        program.ConstantPool.Add(1);
        var func = new BytecodeFunction(0, "main", new PrimitiveType("int"), [])
        {
            Code = [new Instruction(OpCode.PUSH, 0), new Instruction(OpCode.RETURN)]
        };
        program.Functions.Add(func);

        var runtime = new RuntimeContext();
        var jitVm = new JitVirtualMachine(program, runtime, hotThreshold: 1, trace: true);

        // Act
        var output = TestsHelpers.CaptureOutput(() => jitVm.Run("main"));

        // Assert
        Assert.Contains("[JIT] Compiling", output);
        Assert.Contains("[JIT] Execute", output);
    }

    [Fact]
    public void StoreLocal_CoercesIntToLong()
    {
        // Arrange
        var program = new BytecodeProgram();
        var runtime = new RuntimeContext();
        var ctx = CreateContext(program, runtime);

        var func = new BytecodeFunction(0, "f", new PrimitiveType("void"), []);
        func.Locals.Add(new BytecodeVariable(0, "x", new PrimitiveType("long")));

        var locals = new Value[1];
        var funcField = typeof(ExecutionContextBase).GetField("CurrentFunc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var localsField = typeof(ExecutionContextBase).GetField("CurrentLocals", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        funcField!.SetValue(ctx, func);
        localsField!.SetValue(ctx, locals);

        // Act
        ((IInterpreterContext)ctx).StoreLocal(0, Value.FromInt(7));

        // Assert
        Assert.Equal(ValueKind.Long, locals[0].Kind);
        Assert.Equal(7L, locals[0].AsLong());
    }

    [Fact]
    public void StoreLocal_LongValue_IsPreserved()
    {
        // Arrange
        var program = new BytecodeProgram();
        var runtime = new RuntimeContext();
        var ctx = CreateContext(program, runtime);

        var func = new BytecodeFunction(0, "f", new PrimitiveType("void"), []);
        func.Locals.Add(new BytecodeVariable(0, "x", new PrimitiveType("long")));

        var locals = new Value[1];
        var funcField = typeof(ExecutionContextBase).GetField("CurrentFunc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var localsField = typeof(ExecutionContextBase).GetField("CurrentLocals", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        funcField!.SetValue(ctx, func);
        localsField!.SetValue(ctx, locals);

        // Act
        ((IInterpreterContext)ctx).StoreLocal(0, Value.FromLong(9));

        // Assert
        Assert.Equal(ValueKind.Long, locals[0].Kind);
        Assert.Equal(9L, locals[0].AsLong());
    }

    [Fact]
    public void StoreGlobal_LongValue_IsPreserved()
    {
        // Arrange
        var program = new BytecodeProgram();
        program.Globals.Add(new BytecodeVariable(0, "g", new PrimitiveType("long")));
        var ctx = CreateContext(program, new RuntimeContext());
        IInterpreterContext ic = ctx;

        // Act
        ic.StoreGlobal(0, Value.FromLong(12));
        var loaded = ic.LoadGlobal(0);

        // Assert
        Assert.Equal(ValueKind.Long, loaded.Kind);
        Assert.Equal(12L, loaded.AsLong());
    }

    [Fact]
    public void CoerceToType_LongType_NonNumeric_ReturnsValue()
    {
        // Arrange
        var method = typeof(ExecutionContextBase).GetMethod("CoerceToType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var type = new PrimitiveType("long");
        var value = Value.FromBool(true);

        // Act
        var result = (Value)method!.Invoke(null, [type, value])!;

        // Assert
        Assert.Equal(ValueKind.Bool, result.Kind);
    }

    [Fact]
    public void EnumerateRoots_IncludesStackAndGlobals()
    {
        // Arrange
        var program = new BytecodeProgram();
        program.Globals.Add(new BytecodeVariable(0, "g", new PrimitiveType("int")));
        var runtime = new RuntimeContext();
        var ctx = CreateContext(program, runtime);
        IInterpreterContext ic = ctx;

        var obj1 = runtime.AllocateArray(1);
        var obj2 = runtime.AllocateArray(1);
        var obj3 = runtime.AllocateArray(1);
        var obj4 = runtime.AllocateArray(1);

        ctx.PushStack(Value.FromObject(obj1));
        ic.StoreGlobal(0, Value.FromObject(obj2));

        var currentLocalsField = typeof(ExecutionContextBase).GetField("CurrentLocals", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        currentLocalsField!.SetValue(ctx, new[] { Value.FromObject(obj3) });

        var callStackField = typeof(ExecutionContextBase).GetField("CallStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var callStack = (Stack<CallFrame>)callStackField!.GetValue(ctx)!;
        callStack.Push(new CallFrame(new BytecodeFunction(1, "f", new PrimitiveType("void"), []), new[] { Value.FromObject(obj4) }));

        // Act
        var roots = ctx.EnumerateRoots().ToArray();

        // Assert
        Assert.Contains(obj1, roots);
        Assert.Contains(obj2, roots);
        Assert.Contains(obj3, roots);
        Assert.Contains(obj4, roots);
    }

    [Fact]
    public void InterpreterContext_ExposesCoreMembers()
    {
        // Arrange
        var program = new BytecodeProgram();
        var runtime = new RuntimeContext();
        var ctx = CreateContext(program, runtime);
        IInterpreterContext ic = ctx;

        // Act
        _ = ic.Program;
        _ = ic.Runtime;
        var trace = ic.Trace;
        var hasStack = ic.HasStack();

        // Assert
        Assert.False(trace);
        Assert.False(hasStack);
    }

    [Fact]
    public void InterpreterContext_LoadGlobal_And_CallNative_Work()
    {
        // Arrange
        var program = new BytecodeProgram();
        program.Globals.Add(new BytecodeVariable(0, "g", new PrimitiveType("int")));
        var ctx = CreateContext(program, new RuntimeContext());
        IInterpreterContext ic = ctx;

        // Act
        ic.StoreGlobal(0, Value.FromInt(7));
        var loaded = ic.LoadGlobal(0);
        ic.CallNative(1);

        // Assert
        Assert.Equal(7, loaded.AsInt());
        Assert.True(ctx.StackCount > 0);
        _ = ctx.PopStack();
    }
}
