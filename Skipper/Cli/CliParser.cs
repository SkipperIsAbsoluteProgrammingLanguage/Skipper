namespace Skipper.Cli;

public static class CliParser
{
    private const string Usage = "Usage: Skipper <file.sk> [--jit [N]] [--trace] [--mem N]";

    public static bool TryParseArgs(string[] args, out ProgramOptions options, out string error)
    {
        options = default;
        error = Usage;

        if (args.Length == 0)
        {
            return false;
        }

        var path = args[0];
        var useJit = false;
        var jitThreshold = 50;
        var trace = false;
        var memMb = 1;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--jit")
            {
                useJit = true;
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var threshold))
                {
                    jitThreshold = threshold;
                    i++;
                }

                continue;
            }

            if (arg == "--trace")
            {
                trace = true;
                continue;
            }

            if (arg == "--mem")
            {
                if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out var memValue) || memValue <= 0)
                {
                    error = "Invalid --mem value. Expected positive integer (MB).";
                    return false;
                }

                memMb = memValue;
                i++;
                continue;
            }

            error = $"Unknown argument: {arg}\n{Usage}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            error = Usage;
            return false;
        }

        if (!File.Exists(path))
        {
            error = $"File not found: {path}";
            return false;
        }

        options = new ProgramOptions(path, useJit, jitThreshold, trace, memMb);
        return true;
    }
}
