using System.Text.Json.Serialization;
using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

// Результирующий класс, который после парсинга сериализуется
public sealed class BytecodeProgram
{
    // Таблица типов
    public List<BytecodeType> Types { get; set; } = [];

    // Все функции программы (включая методы классов)
    [JsonInclude]
    public List<BytecodeFunction> Functions { get; private set; } = [];

    // Все классы программы
    [JsonInclude]
    public List<BytecodeClass> Classes { get; private set; } = [];

    // Глобальные переменные
    [JsonInclude]
    public List<BytecodeVariable> Globals { get; private set; } = [];

    // ID функции инициализации глобалов (если есть)
    public int GlobalInitFunctionId { get; set; } = -1;

    // Общий пул констант (числа, строки, bool, имена классов)
    [JsonInclude]
    public List<object> ConstantPool { get; private set; } = [];

    // ID функции-точки входа
    public int EntryFunctionId { get; set; }
}
