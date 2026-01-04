using Skipper.Runtime.Objects;
using Skipper.Runtime.Values;
using Xunit;

namespace Skipper.Runtime.Tests;

public unsafe class GcTests
{
    [Fact]
    public void SingleObject_IsCollected_WhenNoRoots()
    {
        var rt = new RuntimeContext();

        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            []
        );

        rt.Heap.Allocate(desc, 16);

        rt.Collect();

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
        rt.Roots.Add(Value.FromObject(a));

        rt.Collect();

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

        rt.Roots.Add(Value.FromObject(a));
        rt.Collect();

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

        rt.Roots.Add(Value.FromObject(a));
        rt.Collect();

        WritePtr(a, 0);
        rt.Collect();

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

        rt.Collect();

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

        rt.Roots.Add(Value.FromObject(a));
        rt.Collect();

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

        rt.Roots.Add(Value.FromObject(a));
        rt.Collect();

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

        rt.Roots.Add(Value.FromObject(a));
        rt.Roots.Add(Value.FromObject(b));

        rt.Collect();

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
        rt.Roots.Add(Value.FromObject(a));

        rt.Collect();
        rt.Collect();
        rt.Collect();

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

        rt.Roots.Add(Value.FromInt(123));
        rt.Roots.Add(Value.FromBool(true));

        rt.Collect();

        Assert.Empty(rt.Heap.Objects);
    }

    private static void WritePtr(nint target, nint value)
    {
        *(nint*)target = value;
    }
}