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
        var program = JsonSerializer.Deserialize<BytecodeProgram>(json, JsonOptions)
                      ?? throw new InvalidOperationException("Не удалось десериализовать BytecodeProgram");
        NormalizeProgram(program);
        return program;
    }

    private static void NormalizeProgram(BytecodeProgram program)
    {
        NormalizeConstantPool(program.ConstantPool);

        foreach (var function in program.Functions)
        {
            foreach (var instruction in function.Code)
            {
                if (instruction.Operands.Count == 0)
                {
                    continue;
                }

                var changed = false;
                var converted = new object[instruction.Operands.Count];
                for (var i = 0; i < instruction.Operands.Count; i++)
                {
                    var operand = instruction.Operands[i];
                    if (operand is JsonElement element)
                    {
                        converted[i] = ConvertJsonElement(element)!;
                        changed = true;
                    }
                    else
                    {
                        converted[i] = operand;
                    }
                }

                if (changed)
                {
                    instruction.Operands = converted;
                }
            }
        }
    }

    private static void NormalizeConstantPool(List<object> pool)
    {
        for (var i = 0; i < pool.Count; i++)
        {
            if (pool[i] is JsonElement element)
            {
                pool[i] = ConvertJsonElement(element)!;
            }
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => ConvertNumber(element),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new NotSupportedException($"Unsupported JSON constant kind: {element.ValueKind}")
        };
    }

    private static object ConvertNumber(JsonElement element)
    {
        if (element.TryGetInt32(out var i))
        {
            return i;
        }

        if (element.TryGetInt64(out var l))
        {
            return l;
        }

        return element.GetDouble();
    }
}
