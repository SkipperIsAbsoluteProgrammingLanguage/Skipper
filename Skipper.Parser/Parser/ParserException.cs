using Skipper.Lexer.Tokens;

namespace Skipper.Parser.Parser;

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