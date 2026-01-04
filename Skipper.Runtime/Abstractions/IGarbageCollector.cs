namespace Skipper.Runtime.Abstractions;

public interface IGarbageCollector
{
    void Collect(IRootProvider roots);
}