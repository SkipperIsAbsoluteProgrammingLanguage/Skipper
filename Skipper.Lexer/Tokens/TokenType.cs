// ReSharper disable InconsistentNaming

namespace Skipper.Lexer.Tokens;

public enum TokenType
{
    /// <summary>
    /// Конец файла (End Of File)
    /// </summary>
    EOF,

    /// <summary>
    /// Неизвестный символ (ошибка)
    /// </summary>
    BAD,

    /// <summary>
    /// Целое число
    /// </summary>
    NUMBER,

    // Операторы
    PLUS, // +
    MINUS, // -
    STAR, // *
    SLASH, // /
    MODULO, // %

    // Операторы сравнения
    EQUAL,
    NOT_EQUAL,
    LESS,
    GREATER,
    LESS_EQUAL,
    GREATER_EQUAL,

    // Логические операторы
    AND, // &&
    OR, // ||
    NOT, // !

    // Побитовые операторы
    BIT_AND, // &
    BIT_OR, // |

    /// <summary>
    /// Присваивание
    /// </summary>
    ASSIGN,

    /// <summary>
    /// Стрелка для возвращаемого типа
    /// </summary>
    ARROW,

    // Тернарный оператор
    QUESTION_MARK,
    COLON,

    // Литералы
    DOUBLE_LITERAL,
    CHAR_LITERAL,
    STRING_LITERAL,
    BOOL_LITERAL,

    SEMICOLON, // ;
    COMMA, // ,
    DOT, // .
    BRACE_OPEN, // {
    BRACE_CLOSE, // }
    BRACKET_OPEN, // [
    BRACKET_CLOSE, // ]
    LPAREN, // (
    RPAREN, // )

    /// <summary>
    /// Имена переменных и функций
    /// </summary>
    IDENTIFIER,

    // Ключевые слова
    KEYWORD_FN,
    KEYWORD_INT,
    KEYWORD_DOUBLE,
    KEYWORD_BOOL,
    KEYWORD_CHAR,
    KEYWORD_STRING,
    KEYWORD_RETURN,
    KEYWORD_IF,
    KEYWORD_ELSE,
    KEYWORD_WHILE,
    KEYWORD_FOR,
    KEYWORD_PUBLIC,
    KEYWORD_CLASS,
    KEYWORD_NEW,
    KEYWORD_VOID
}