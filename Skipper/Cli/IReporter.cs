namespace Skipper.Cli;

public interface IReporter
{
    void Header(string title);
    void Section(string title);
    void Indent(int level, string text);
    void Line(string text);
    void PrintDiagnostics<T>(IEnumerable<T> diagnostics);
}
