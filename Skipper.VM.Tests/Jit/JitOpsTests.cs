using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Types;
using Skipper.Runtime;
using Skipper.Runtime.Values;
using Skipper.VM.Jit;
using Xunit;

namespace Skipper.VM.Tests.Jit;

public class JitOpsTests
{
    private static JitExecutionContext CreateContext(BytecodeProgram program, RuntimeContext runtime)
    {
        var compiler = new BytecodeJitCompiler();
        return new JitExecutionContext(program, runtime, compiler, hotThreshold: 1, trace: false);
    }

    [Fact]
    public void FromConst_SupportsPrimitiveTypes()
    {
        // Arrange
        var program = new BytecodeProgram();
        var runtime = new RuntimeContext();
        var ctx = CreateContext(program, runtime);

        // Act & Assert
        Assert.Equal(ValueKind.Null, JitOps.FromConst(ctx, null!).Kind);
        Assert.Equal(ValueKind.Int, JitOps.FromConst(ctx, 1).Kind);
        Assert.Equal(ValueKind.Long, JitOps.FromConst(ctx, 2L).Kind);
        Assert.Equal(ValueKind.Double, JitOps.FromConst(ctx, 3.0).Kind);
        Assert.Equal(ValueKind.Bool, JitOps.FromConst(ctx, true).Kind);
        Assert.Equal(ValueKind.Char, JitOps.FromConst(ctx, 'a').Kind);
        Assert.Equal(ValueKind.ObjectRef, JitOps.FromConst(ctx, "hi").Kind);
    }

    [Fact]
    public void FromConst_UnsupportedType_Throws()
    {
        // Arrange
        var ctx = CreateContext(new BytecodeProgram(), new RuntimeContext());

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => JitOps.FromConst(ctx, DateTime.UtcNow));
    }

    [Fact]
    public void Add_StringConcats_Work()
    {
        // Arrange
        var runtime = new RuntimeContext();
        var ctx = CreateContext(new BytecodeProgram(), runtime);
        var left = Value.FromObject(runtime.AllocateString("a"));
        var right = Value.FromObject(runtime.AllocateString("b"));

        // Act
        var result = JitOps.Add(ctx, left, right);
        var text = runtime.ReadStringFromMemory(result.AsObject());

        // Assert
        Assert.Equal("ab", text);
    }

    [Fact]
    public void Add_StringAndIntConcats_Work()
    {
        // Arrange
        var runtime = new RuntimeContext();
        var ctx = CreateContext(new BytecodeProgram(), runtime);
        var str = Value.FromObject(runtime.AllocateString("x="));
        var num = Value.FromInt(5);

        // Act
        var left = JitOps.Add(ctx, str, num);
        var right = JitOps.Add(ctx, num, str);

        // Assert
        Assert.Equal("x=5", runtime.ReadStringFromMemory(left.AsObject()));
        Assert.Equal("5x=", runtime.ReadStringFromMemory(right.AsObject()));
    }

    [Fact]
    public void Add_DoubleLongAndInt_Work()
    {
        // Arrange
        var ctx = CreateContext(new BytecodeProgram(), new RuntimeContext());

        // Act
        var d = JitOps.Add(ctx, Value.FromDouble(1.5), Value.FromInt(1));
        var l = JitOps.Add(ctx, Value.FromLong(2L), Value.FromInt(3));
        var i = JitOps.Add(ctx, Value.FromInt(4), Value.FromInt(5));

        // Assert
        Assert.Equal(2.5, d.AsDouble(), 10);
        Assert.Equal(5L, l.AsLong());
        Assert.Equal(9, i.AsInt());
    }

    [Fact]
    public void SubMulDivMod_Branches_Work()
    {
        // Arrange
        CreateContext(new BytecodeProgram(), new RuntimeContext());

        // Act
        var d = JitOps.Sub(Value.FromDouble(3.5), Value.FromDouble(1.0));
        var l = JitOps.Mul(Value.FromLong(2), Value.FromLong(3));
        var i = JitOps.Div(Value.FromInt(9), Value.FromInt(3));
        var m = JitOps.Mod(Value.FromInt(10), Value.FromInt(4));

        // Assert
        Assert.Equal(2.5, d.AsDouble(), 10);
        Assert.Equal(6L, l.AsLong());
        Assert.Equal(3, i.AsInt());
        Assert.Equal(2, m.AsInt());
    }

    [Fact]
    public void SubMulDivMod_LongAndDouble_Branches_Work()
    {
        // Act
        var subLong = JitOps.Sub(Value.FromLong(9), Value.FromLong(4));
        var mulDouble = JitOps.Mul(Value.FromDouble(1.5), Value.FromDouble(2.0));
        var divDouble = JitOps.Div(Value.FromDouble(5.0), Value.FromInt(2));
        var divLong = JitOps.Div(Value.FromLong(9), Value.FromLong(3));
        var modDouble = JitOps.Mod(Value.FromDouble(5.5), Value.FromDouble(2.0));
        var modLong = JitOps.Mod(Value.FromLong(10), Value.FromLong(4));

        // Assert
        Assert.Equal(5L, subLong.AsLong());
        Assert.Equal(3.0, mulDouble.AsDouble(), 10);
        Assert.Equal(2.5, divDouble.AsDouble(), 10);
        Assert.Equal(3L, divLong.AsLong());
        Assert.Equal(1.5, modDouble.AsDouble(), 10);
        Assert.Equal(2L, modLong.AsLong());
    }

    [Fact]
    public void DivAndMod_ByZero_Throw()
    {
        // Arrange
        CreateContext(new BytecodeProgram(), new RuntimeContext());

        // Act & Assert
        Assert.Throws<DivideByZeroException>(() => JitOps.Div(Value.FromInt(1), Value.FromInt(0)));
        Assert.Throws<DivideByZeroException>(() => JitOps.Mod(Value.FromLong(1), Value.FromLong(0)));
    }

    [Fact]
    public void Neg_HandlesNumericKinds()
    {
        // Arrange & Act
        var d = JitOps.Neg(Value.FromDouble(1.5));
        var l = JitOps.Neg(Value.FromLong(5));
        var i = JitOps.Neg(Value.FromInt(3));

        // Assert
        Assert.Equal(-1.5, d.AsDouble(), 10);
        Assert.Equal(-5L, l.AsLong());
        Assert.Equal(-3, i.AsInt());
    }

    [Fact]
    public void Comparisons_HandleNumericAndNonNumeric()
    {
        // Arrange
        var a = Value.FromInt(5);
        var b = Value.FromDouble(5.0);
        var obj = Value.FromObject((nint)123);
        var obj2 = Value.FromObject((nint)123);

        // Act & Assert
        Assert.True(JitOps.CmpEq(a, b).AsBool());
        Assert.True(JitOps.CmpNe(a, Value.FromInt(6)).AsBool());
        Assert.True(JitOps.CmpLt(Value.FromInt(1), Value.FromInt(2)).AsBool());
        Assert.True(JitOps.CmpGt(Value.FromInt(3), Value.FromInt(2)).AsBool());
        Assert.True(JitOps.CmpLe(Value.FromInt(2), Value.FromInt(2)).AsBool());
        Assert.True(JitOps.CmpGe(Value.FromInt(2), Value.FromInt(2)).AsBool());
        Assert.True(JitOps.CmpNe(obj, Value.Null()).AsBool());
        Assert.True(JitOps.CmpEq(obj, obj2).AsBool());
    }

    [Fact]
    public void Comparison_LongBranch_IsUsed()
    {
        // Arrange
        var left = Value.FromLong(2);
        var right = Value.FromLong(3);

        // Act
        var result = JitOps.CmpLt(left, right);

        // Assert
        Assert.True(result.AsBool());
    }

    [Fact]
    public void LogicalOps_Work()
    {
        // Arrange
        var t = Value.FromBool(true);
        var f = Value.FromBool(false);

        // Act & Assert
        Assert.True(JitOps.And(t, t).AsBool());
        Assert.False(JitOps.And(t, f).AsBool());
        Assert.True(JitOps.Or(f, t).AsBool());
        Assert.True(JitOps.Not(f).AsBool());
        Assert.True(JitOps.IsTrue(t));
    }

    [Fact]
    public void NewArray_NegativeLength_Throws()
    {
        // Arrange
        var runtime = new RuntimeContext();
        var ctx = CreateContext(new BytecodeProgram(), runtime);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => JitOps.NewArray(ctx, Value.FromInt(-1)));
    }

    [Fact]
    public void NewArray_TooLarge_ThrowsOutOfMemory()
    {
        // Arrange
        var runtime = new RuntimeContext();
        var ctx = CreateContext(new BytecodeProgram(), runtime);

        // Act & Assert
        Assert.Throws<OutOfMemoryException>(() => JitOps.NewArray(ctx, Value.FromInt(200000)));
    }

    [Fact]
    public void NewObject_And_FieldAccess_Work()
    {
        // Arrange
        var program = new BytecodeProgram();
        var cls = new BytecodeClass(0, "Point");
        cls.Fields.Add("x", new BytecodeClassField(0, new PrimitiveType("int")));
        program.Classes.Add(cls);
        var runtime = new RuntimeContext();
        var ctx = CreateContext(program, runtime);

        // Act
        var obj = JitOps.NewObject(ctx, 0);
        JitOps.SetField(ctx, obj, 0, Value.FromInt(7));
        var val = JitOps.GetField(ctx, obj, 0);

        // Assert
        Assert.Equal(7, val.AsInt());
    }

    [Fact]
    public void NewObject_TooLarge_ThrowsOutOfMemory()
    {
        // Arrange
        var program = new BytecodeProgram();
        var cls = new BytecodeClass(0, "Big");
        for (var i = 0; i < 200000; i++)
        {
            cls.Fields.Add("f" + i, new BytecodeClassField(i, new PrimitiveType("int")));
        }
        program.Classes.Add(cls);

        var runtime = new RuntimeContext();
        var ctx = CreateContext(program, runtime);

        // Act & Assert
        Assert.Throws<OutOfMemoryException>(() => JitOps.NewObject(ctx, 0));
    }

    [Fact]
    public void NewObject_AfterGc_Succeeds()
    {
        // Arrange
        const int heapSize = 1024 * 1024;
        var runtime = new RuntimeContext(heapSize);
        _ = runtime.AllocateObject(heapSize - sizeof(long), 0);

        var program = new BytecodeProgram();
        var cls = new BytecodeClass(0, "Small");
        cls.Fields.Add("x", new BytecodeClassField(0, new PrimitiveType("int")));
        program.Classes.Add(cls);

        var ctx = CreateContext(program, runtime);

        // Act
        var obj = JitOps.NewObject(ctx, 0);

        // Assert
        Assert.Equal(ValueKind.ObjectRef, obj.Kind);
    }

    [Fact]
    public void NewArray_AfterGc_Succeeds()
    {
        // Arrange
        const int heapSize = 1024 * 1024;
        var runtime = new RuntimeContext(heapSize);
        _ = runtime.AllocateObject(heapSize - sizeof(long), 0);

        var ctx = CreateContext(new BytecodeProgram(), runtime);

        // Act
        var arr = JitOps.NewArray(ctx, Value.FromInt(1));

        // Assert
        Assert.Equal(ValueKind.ObjectRef, arr.Kind);
    }

    [Fact]
    public void ElementAccess_And_NullChecks_Work()
    {
        // Arrange
        var runtime = new RuntimeContext();
        var ctx = CreateContext(new BytecodeProgram(), runtime);
        var arrPtr = runtime.AllocateArray(2);
        var arr = Value.FromObject(arrPtr);

        // Act
        JitOps.SetElement(ctx, arr, Value.FromInt(1), Value.FromInt(9));
        var val = JitOps.GetElement(ctx, arr, Value.FromInt(1));

        // Assert
        Assert.Equal(9, val.AsInt());
        Assert.Throws<NullReferenceException>(() => JitOps.GetElement(ctx, Value.Null(), Value.FromInt(0)));
        Assert.Throws<NullReferenceException>(() => JitOps.SetField(ctx, Value.Null(), 0, Value.FromInt(1)));
    }
}
