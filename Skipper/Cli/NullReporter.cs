namespace Skipper.Cli;

public sealed class NullReporter : IReporter
{
    public void Header(string title) { }
    public void Section(string title) { }
    public void Indent(int level, string text) { }
    public void Line(string text) { }
    public void PrintDiagnostics<T>(IEnumerable<T> diagnostics) { }
}
