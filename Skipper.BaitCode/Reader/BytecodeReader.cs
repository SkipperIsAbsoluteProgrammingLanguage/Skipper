using System.Text.Json;
using System.Text.Json.Serialization;
using Skipper.BaitCode.Objects;

namespace Skipper.BaitCode.Reader;

public static class BytecodeReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static BytecodeProgram LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BytecodeProgram>(json, JsonOptions)
               ?? throw new InvalidOperationException("Не удалось десериализовать BytecodeProgram");
    }
}
