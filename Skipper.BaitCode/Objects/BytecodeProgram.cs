using Skipper.BaitCode.Types;

namespace Skipper.BaitCode.Objects;

// Результирующий класс, который после парсинга сериализуется
public sealed class BytecodeProgram
{
    // Таблица типов
    public List<BytecodeType> Types { get; } = [];
    // Все переменные (числа, строки, bool и т.д.)
    public List<BytecodeVariable> Variables { get; } = [];
    // Все функции программы (включая методы классов)
    public List<BytecodeFunction> Functions { get; } = [];
    // Все классы программы
    public List<BytecodeClass> Classes { get; } = [];

    // ID функции-точки входа
    public int EntryFunctionId { get; set; }
    
    // Общий пул констант (числа, строки, bool, имена классов)
    public List<object> ConstantPool { get; } = [];
}

