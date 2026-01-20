using Skipper.Cli;

if (!CliParser.TryParseArgs(args, out var options, out var error))
{
    Console.WriteLine(error);
    return 1;
}

IReporter reporter = options.Trace ? new ConsoleReporter() : new NullReporter();
reporter.Header("Skipper Compiler");

return CompilationPipeline.Run(options);
