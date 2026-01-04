namespace Skipper.Runtime.Abstractions;

public interface IRootProvider
{
    IEnumerable<nint> EnumerateRoots();
}