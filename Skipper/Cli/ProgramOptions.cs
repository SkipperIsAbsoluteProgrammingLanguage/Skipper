namespace Skipper.Cli;

public readonly record struct ProgramOptions(
    string Path,
    bool UseJit,
    int JitThreshold,
    bool Trace,
    int MemMb);
