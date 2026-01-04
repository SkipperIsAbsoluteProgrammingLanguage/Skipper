using Skipper.Runtime.Objects;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.Runtime.Tests;

public unsafe class GcTests
{
    private readonly TestRootProvider _rootProvider = new();

    [Fact]
    public void SingleObject_IsCollected_WhenNoRoots()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        rt.Heap.Allocate(desc, 16);

        rt.Collect(_rootProvider);

        Assert.Empty(rt.Heap.Objects);
    }

    [Fact]
    public void RootObject_IsNotCollected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        var a = rt.Heap.Allocate(desc, 16);
        _rootProvider.Add(Value.FromObject(a));

        rt.Collect(_rootProvider);

        Assert.Single(rt.Heap.Objects);
    }

    [Fact]
    public void ChainReferences_AreKeptAlive()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));
        var c = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, c);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        Assert.Equal(3, rt.Heap.Objects.Count());
    }

    [Fact]
    public void UnreachableObject_IsCollected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        WritePtr(a, 0);
        rt.Collect(_rootProvider);

        Assert.Single(rt.Heap.Objects);
    }

    [Fact]
    public void CyclicReferences_WithoutRoots_AreCollected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, a);

        rt.Collect(_rootProvider);

        Assert.Empty(rt.Heap.Objects);
    }

    [Fact]
    public void CyclicReferences_WithRoot_AreKeptAlive()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);
        WritePtr(b, a);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        Assert.Equal(2, rt.Heap.Objects.Count());
    }

    [Fact]
    public void PartialGraph_IsCorrectlyCollected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));
        rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, b);

        _rootProvider.Add(Value.FromObject(a));
        rt.Collect(_rootProvider);

        Assert.Equal(2, rt.Heap.Objects.Count());
    }

    [Fact]
    public void MultipleRoots_AreAllRespected()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [0]
        );

        var a = rt.Heap.Allocate(desc, sizeof(nint));
        var b = rt.Heap.Allocate(desc, sizeof(nint));
        var c = rt.Heap.Allocate(desc, sizeof(nint));

        WritePtr(a, c);

        _rootProvider.Add(Value.FromObject(a));
        _rootProvider.Add(Value.FromObject(b));

        rt.Collect(_rootProvider);

        Assert.Equal(3, rt.Heap.Objects.Count());
    }

    [Fact]
    public void MultipleCollections_WorkCorrectly()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        var a = rt.Heap.Allocate(desc, 16);
        _rootProvider.Add(Value.FromObject(a));

        rt.Collect(_rootProvider);
        rt.Collect(_rootProvider);
        rt.Collect(_rootProvider);

        Assert.Single(rt.Heap.Objects);
    }

    [Fact]
    public void NonObjectValues_AreIgnoredByGC()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        rt.Heap.Allocate(desc, 16);

        _rootProvider.Add(Value.FromInt(123));
        _rootProvider.Add(Value.FromBool(true));

        rt.Collect(_rootProvider);

        Assert.Empty(rt.Heap.Objects);
    }

    private static void WritePtr(nint target, nint value)
    {
        *(nint*)target = value;
    }
}