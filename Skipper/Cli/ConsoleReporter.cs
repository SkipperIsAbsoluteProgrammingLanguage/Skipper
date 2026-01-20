namespace Skipper.Cli;

public sealed class ConsoleReporter : IReporter
{
    public void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine("==============================");
        Console.WriteLine(title);
        Console.WriteLine("==============================");
    }

    public void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine("------------------------------");
        Console.WriteLine(title);
        Console.WriteLine("------------------------------");
    }

    public void Indent(int level, string text)
    {
        Console.WriteLine($"{new string(' ', level * 2)}{text}");
    }

    public void Line(string text)
    {
        Console.WriteLine(text);
    }

    public void PrintDiagnostics<T>(IEnumerable<T> diagnostics)
    {
        Console.WriteLine("[FAIL] Diagnostics:");
        foreach (var diag in diagnostics)
        {
            Indent(1, $"- {diag}");
        }
    }
}
