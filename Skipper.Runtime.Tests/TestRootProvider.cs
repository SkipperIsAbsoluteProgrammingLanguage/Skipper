using Skipper.Runtime.Abstractions;
using Skipper.Runtime.Values;

namespace Skipper.Runtime.Tests;

public sealed class TestRootProvider : IRootProvider
{
    private readonly List<Value> _roots = [];

    public void Add(Value value) => _roots.Add(value);

    public void Clear() => _roots.Clear();

    public IEnumerable<nint> EnumerateRoots()
    {
        return _roots
            .Where(v => v.Kind == ValueKind.ObjectRef)
            .Select(v => v.AsObject());
    }
}