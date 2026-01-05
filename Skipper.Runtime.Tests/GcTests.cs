using Skipper.Runtime.GC;
using Skipper.Runtime.Memory;
using Skipper.Runtime.Objects;
using Skipper.Runtime.Values;
using Skipper.Runtime.Abstractions;
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

        Assert.Equal(3, _heap.Objects.Count());
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

        Assert.Equal(2, _heap.Objects.Count());
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
        _heap.Allocate(desc, sizeof(nint)); // Unreachable

        WritePtr(a, b);

        _rootProvider.Add(Value.FromObject(a));
        _gc.Collect(_rootProvider);

        Assert.Equal(2, _heap.Objects.Count());
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

        Assert.Equal(3, _heap.Objects.Count());
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

    [Fact]
    public void MixedLayout_GcFollowsOnlyDescriptors()
    {
        // [Int (8 bytes), Ref (8 bytes)]
        var desc = new ObjectDescriptor(
            ObjectKind.Class,
            [sizeof(long)] // Смещение 8
        );

        var parent = _heap.Allocate(desc, 16);
        var child = _heap.Allocate(desc, 16);

        *(long*)parent = 12345;
        *(nint*)(parent + sizeof(long)) = child;

        _rootProvider.Add(Value.FromObject(parent));
        _gc.Collect(_rootProvider);

        Assert.Equal(2, _heap.Objects.Count());
        Assert.Equal(12345, *(long*)parent);
    }

    [Fact]
    public void SelfReference_IsKeptAlive()
    {
        var desc = new ObjectDescriptor(ObjectKind.Class, [0]);

        var obj = _heap.Allocate(desc, sizeof(nint));

        WritePtr(obj, obj);

        _rootProvider.Add(Value.FromObject(obj));
        _gc.Collect(_rootProvider);

        Assert.Single(_heap.Objects);
    }

    [Fact]
    public void InvalidPointer_InRoots_DoesNotCrash()
    {
        nint invalidPtr = (nint)0xDEADBEEF;

        _rootProvider.Add(Value.FromObject(invalidPtr));

        var exception = Record.Exception(() => _gc.Collect(_rootProvider));

        Assert.Null(exception);
    }

    [Fact]
    public void ObjectWithNullReference_DoesNotCrash()
    {
        var desc = new ObjectDescriptor(ObjectKind.Class, [0]);
        var obj = _heap.Allocate(desc, sizeof(nint));

        WritePtr(obj, 0);

        _rootProvider.Add(Value.FromObject(obj));

        var exception = Record.Exception(() => _gc.Collect(_rootProvider));

        Assert.Null(exception);
        Assert.Single(_heap.Objects);
    }

    [Fact]
    public void DiamondGraph_IsKeptAlive()
    {
        var desc = new ObjectDescriptor(ObjectKind.Class, [0, sizeof(nint)]);

        var a = _heap.Allocate(desc, sizeof(nint) * 2);
        var b = _heap.Allocate(desc, sizeof(nint) * 2);
        var c = _heap.Allocate(desc, sizeof(nint) * 2);
        var d = _heap.Allocate(desc, sizeof(nint) * 2);

        // A -> B, C
        WritePtr(a, b);
        WritePtr(a + sizeof(nint), c);

        // B -> D
        WritePtr(b, d);

        // C -> D
        WritePtr(c, d);

        _rootProvider.Add(Value.FromObject(a));
        _gc.Collect(_rootProvider);

        Assert.Equal(4, _heap.Objects.Count());
    }

    [Fact]
    public void DeepLinkedList_DoesNotCauseStackOverflow()
    {
        var desc = new ObjectDescriptor(ObjectKind.Class, [0]);

        nint head = _heap.Allocate(desc, sizeof(nint));
        nint current = head;

        for (int i = 0; i < 10000; i++)
        {
            nint next = _heap.Allocate(desc, sizeof(nint));
            WritePtr(current, next);
            current = next;
        }

        _rootProvider.Add(Value.FromObject(head));

        _gc.Collect(_rootProvider);

        Assert.Equal(10001, _heap.Objects.Count());
    }

    [Fact]
    public void MultipleReferencesToSameObject_ProcessedCorrectly()
    {
        var desc = new ObjectDescriptor(ObjectKind.Class, [0, sizeof(nint)]);

        var parent = _heap.Allocate(desc, sizeof(nint) * 2);
        var child = _heap.Allocate(desc, sizeof(nint) * 2);

        WritePtr(parent, child);
        WritePtr(parent + sizeof(nint), child);

        _rootProvider.Add(Value.FromObject(parent));
        _gc.Collect(_rootProvider);

        Assert.Equal(2, _heap.Objects.Count());
    }

    private static void WritePtr(nint target, nint value)
    {
        *(nint*)target = value;
    }
}