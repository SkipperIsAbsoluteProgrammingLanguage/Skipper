namespace Skipper.Cli;

public readonly record struct ProgramOptions(
    string Path,
    bool RunFromBytecode,
    bool UseJit,
    int JitThreshold,
    bool Trace,
    int MemMb);
