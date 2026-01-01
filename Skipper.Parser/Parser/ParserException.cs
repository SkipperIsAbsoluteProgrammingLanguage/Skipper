using Skipper.Lexer.Tokens;

namespace Skipper.Parser.Parser;

/// <summary>
/// Исключение, используемое для прерывания процесса парсинга при обнаружении синтаксической ошибки. 
/// Позволяет механизму "Panic Mode" перехватить управление и синхронизировать состояние парсера
/// </summary>
public class ParserException : Exception
{
    /// <summary>
    /// Токен, на котором произошла ошибка.
    /// </summary>
    public Token? Token { get; }

    public ParserException(string message, Token? token = null)
        : base(FormatMessage(message, token))
    {
        Token = token;
    }

    private static string FormatMessage(string message, Token? token)
    {
        if (token == null)
        {
            return message;
        }

        if (token.Type == TokenType.EOF)
        {
            return $"{message} at end of file";
        }

        return $"{message} at '{token.Text}' (line {token.Line}, column {token.Column})";
    }
}