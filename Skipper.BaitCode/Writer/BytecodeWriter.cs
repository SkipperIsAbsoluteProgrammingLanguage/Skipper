using System.Text.Json;
using System.Text.Json.Serialization;
using Skipper.BaitCode.Objects;

namespace Skipper.BaitCode.Writer;

/// <summary>
/// Сериализация байткода в файл в безопасном формате JSON
/// </summary>
public class BytecodeWriter
{
    private readonly BytecodeProgram _program;

    public BytecodeWriter(BytecodeProgram program)
    {
        _program = program;
    }

    public void SaveToFile(string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        string json = JsonSerializer.Serialize(_program, options);
        File.WriteAllText(path, json);
    }

    public static BytecodeProgram LoadFromFile(string path)
    {
        string json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        return JsonSerializer.Deserialize<BytecodeProgram>(json, options) 
               ?? throw new InvalidOperationException("Не получилось десериализовать BytecodeProgram");
    }
}