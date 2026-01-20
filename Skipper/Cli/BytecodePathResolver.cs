namespace Skipper.Cli;

public static class BytecodePathResolver
{
    public static string GetBytecodePath(string sourcePath)
    {
        var projectRoot = FindProjectRoot();
        var outRoot = Path.Combine(projectRoot, "out");
        var relativePath = Path.GetRelativePath(projectRoot, Path.GetFullPath(sourcePath));
        var sanitizedRelative = SanitizeRelativePath(relativePath);
        var relativeDir = Path.GetDirectoryName(sanitizedRelative) ?? string.Empty;
        var bytecodeDir = Path.Combine(outRoot, relativeDir);
        Directory.CreateDirectory(bytecodeDir);
        return Path.Combine(
            bytecodeDir,
            Path.GetFileNameWithoutExtension(sanitizedRelative) + ".json");
    }

    private static string FindProjectRoot()
    {
        var start = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Skipper.csproj");
            if (File.Exists(candidate))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string SanitizeRelativePath(string relativePath)
    {
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var parts = relativePath.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        var cleaned = new List<string>(parts.Length);
        cleaned.AddRange(parts.Where(part => part != "." && part != ".."));
        return cleaned.Count == 0 ? Path.GetFileName(relativePath) : Path.Combine(cleaned.ToArray());
    }
}
