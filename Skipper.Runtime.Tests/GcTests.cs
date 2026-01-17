using Skipper.Runtime.GC;
using Skipper.Runtime.Memory;
using Skipper.Runtime.Objects;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.Runtime.Tests;

public unsafe class GcTests
{
    private readonly TestRootProvider _rootProvider;
    private readonly Heap _heap;
    private readonly MarkSweepGc _gc;

    public GcTests()
    {
        _rootProvider = new TestRootProvider();
        _heap = new Heap();
        _gc = new MarkSweepGc(_heap);
    }

    [Fact]
    public void SingleObject_IsCollected_WhenNoRoots()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        _heap.Allocate(desc, 16);

        _gc.Collect(_rootProvider);

        Assert.Empty(_heap.Objects);
    }

    [Fact]
    public void RootObject_IsNotCollected()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        var a = _heap.Allocate(desc, 16);
        _rootProvider.Add(Value.FromObject(a));

        _gc.Collect(_rootProvider);

        Assert.Single(_heap.Objects);
    }

    [Fact]
    public void ChainReferences_AreKeptAlive()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = _heap.Allocate(desc, sizeof(nint));
        var b = _heap.Allocate(desc, sizeof(nint));
        var c = _heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, c);

        _rootProvider.Add(Value.FromObject(a));

        _gc.Collect(_rootProvider);

        Assert.Equal(3, _heap.Objects.Count);
    }

    [Fact]
    public void UnreachableObject_IsCollected()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = _heap.Allocate(desc, sizeof(nint));
        var b = _heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);

        _rootProvider.Add(Value.FromObject(a));
        _gc.Collect(_rootProvider);

        WritePtr(a, 0);
        _gc.Collect(_rootProvider);

        Assert.Single(_heap.Objects);
    }

    [Fact]
    public void CyclicReferences_WithoutRoots_AreCollected()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = _heap.Allocate(desc, sizeof(nint));
        var b = _heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, a);

        _gc.Collect(_rootProvider);

        Assert.Empty(_heap.Objects);
    }

    [Fact]
    public void CyclicReferences_WithRoot_AreKeptAlive()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = _heap.Allocate(desc, sizeof(nint));
        var b = _heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, a);

        _rootProvider.Add(Value.FromObject(a));
        _gc.Collect(_rootProvider);

        Assert.Equal(2, _heap.Objects.Count);
    }

    [Fact]
    public void PartialGraph_IsCorrectlyCollected()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = _heap.Allocate(desc, sizeof(nint));
        var b = _heap.Allocate(desc, sizeof(nint));
        _heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);

        _rootProvider.Add(Value.FromObject(a));
        _gc.Collect(_rootProvider);

        Assert.Equal(2, _heap.Objects.Count);
    }

    [Fact]
    public void MultipleRoots_AreAllRespected()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = _heap.Allocate(desc, sizeof(nint));
        var b = _heap.Allocate(desc, sizeof(nint));
        var c = _heap.Allocate(desc, sizeof(nint));

        WritePtr(a, c);

        _rootProvider.Add(Value.FromObject(a));
        _rootProvider.Add(Value.FromObject(b));

        _gc.Collect(_rootProvider);

        Assert.Equal(3, _heap.Objects.Count);
    }

    [Fact]
    public void MultipleCollections_WorkCorrectly()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        var a = _heap.Allocate(desc, 16);
        _rootProvider.Add(Value.FromObject(a));

        _gc.Collect(_rootProvider);
        _gc.Collect(_rootProvider);
        _gc.Collect(_rootProvider);

        Assert.Single(_heap.Objects);
    }

    [Fact]
    public void NonObjectValues_AreIgnoredByGC()
    {
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        _heap.Allocate(desc, 16);

        _rootProvider.Add(Value.FromInt(123));
        _rootProvider.Add(Value.FromBool(true));

        _gc.Collect(_rootProvider);

        Assert.Empty(_heap.Objects);
    }

    private static void WritePtr(nint target, nint value)
    {
        *(nint*)target = value;
    }
}