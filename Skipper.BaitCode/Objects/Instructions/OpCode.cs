namespace Skipper.BaitCode.Objects.Instructions;

public enum OpCode
{
    // Базовые операции
    PUSH,               // [const_id]
    POP,
    STORE_LOCAL,        // [functionId, slot]
    LOAD_LOCAL,         // [functionId, slot]
    STORE_GLOBAL,       // [slot]
    LOAD_GLOBAL,        // [slot]
    DUP,
    SWAP,

    // Арифметика
    ADD,                // a b -> (a + b)
    SUB,                // a b -> (a - b)
    MUL,                // a b -> (a * b)
    DIV,                // a b -> (a / b)
    MOD,                // a b -> (a % b)
    NEG,                // a -> (-a)

    // Сравнения
    CMP_EQ,             // a b -> (a == b)
    CMP_NE,             // a b -> (a != b)
    CMP_LT,             // a b -> (a < b)
    CMP_GT,             // a b -> (a > b)
    CMP_LE,             // a b -> (a <= b)
    CMP_GE,             // a b -> (a >= b)

    // Логика
    AND,                // a b -> (a && b)
    OR,                 // a b -> (a || b)
    NOT,                // a -> (!a)

    // Управление
    JUMP,               // [offset]
    JUMP_IF_TRUE,       // [offset]
    JUMP_IF_FALSE,      // [offset]
    CALL,               // [func_id, arg_count]
    CALL_METHOD,         // [classId, methodId]
    RETURN,

    // Объекты
    NEW_OBJECT,         // [object_id]
    NEW_ARRAY,          // [array_id]
    GET_FIELD,          // [classId, field_id]
    SET_FIELD,          // [classId, field_id]
    GET_ELEMENT,
    SET_ELEMENT,

    CALL_NATIVE         // [native_id]
}
