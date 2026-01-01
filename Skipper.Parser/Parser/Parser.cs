using Skipper.Lexer.Tokens;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;

namespace Skipper.Parser.Parser;

public sealed class Parser
{
    private readonly List<Token> _tokens;
    private int _position;
    public List<ParserDiagnostic> Diagnostics { get; } = [];
    
    public bool HasErrors => Diagnostics.Any(d => d.Level == ParserDiagnosticLevel.Error);

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }
    
    private Token Current => Peek(0);

    private Token Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Count)
        {
            return _tokens.Last(); // EOF
        }
        return _tokens[index];
    }

    private Token Previous() => _position > 0 ? _tokens[_position - 1] : _tokens[0];

    private bool IsAtEnd => Current.Type == TokenType.EOF;

    private bool Check(TokenType type)
    {
        if (IsAtEnd)
        {
            return false;
        }
        return Current.Type == type;
    }

    private bool Match(params TokenType[] types)
    {
        if (!types.Any(Check))
        {
            return false;
        }
        Advance();
        return true;
    }

    private Token Advance()
    {
        if (!IsAtEnd)
            _position++;
        return Previous();
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
            return Advance();

        var error = new ParserDiagnostic(ParserDiagnosticLevel.Error, message, Current);
        Diagnostics.Add(error);
        throw new ParserException(message, Current);
    }

    // === Точка входа ===

    public ProgramNode Parse()
    {
        var declarations = new List<Declaration>();

        while (!IsAtEnd)
        {
            try
            {
                declarations.Add(ParseDeclaration());
            }
            catch (ParserException)
            {
                Synchronize();
            }
        }

        return new ProgramNode(declarations);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd)
        {
            if (Previous().Type == TokenType.SEMICOLON)
                return;

            switch (Current.Type)
            {
                case TokenType.KEYWORD_CLASS:
                case TokenType.KEYWORD_FN:
                case TokenType.KEYWORD_INT:
                case TokenType.KEYWORD_BOOL:
                case TokenType.KEYWORD_FLOAT:
                case TokenType.KEYWORD_STRING:
                case TokenType.KEYWORD_IF:
                case TokenType.KEYWORD_WHILE:
                case TokenType.KEYWORD_FOR:
                case TokenType.KEYWORD_RETURN:
                    return;
            }

            Advance();
        }
    }

    // === Разбор объявлений (Declarations) ===

    private Declaration ParseDeclaration()
    {
        if (Match(TokenType.KEYWORD_CLASS))
        {
            return ParseClassDeclaration();
        }

        if (Check(TokenType.KEYWORD_FN))
        {
            return ParseFunctionDeclaration();
        }

        // Если встретили что-то неожиданное на верхнем уровне
        var msg = $"Unexpected token at top level: {Current.Text}";
        Diagnostics.Add(new ParserDiagnostic(ParserDiagnosticLevel.Error, msg, Current));
        throw new ParserException(msg, Current);
    }

    private ClassDeclaration ParseClassDeclaration()
    {
        var name = Consume(TokenType.IDENTIFIER, "Expected class name.").Text;
        Consume(TokenType.BRACE_OPEN, "Expected '{' before class body.");

        var members = new List<Declaration>();
        while (!Check(TokenType.BRACE_CLOSE) && !IsAtEnd)
        {
            if (Check(TokenType.KEYWORD_FN))
            {
                members.Add(ParseFunctionDeclaration());
            }
            else
            {
                members.Add(ParseFieldDeclaration());
            }
        }

        Consume(TokenType.BRACE_CLOSE, "Expected '}' after class body.");
        return new ClassDeclaration(name, members);
    }

    private VariableDeclaration ParseFieldDeclaration()
    {
        bool isPublic = Match(TokenType.KEYWORD_PUBLIC);
        var type = ParseType();
        var name = Consume(TokenType.IDENTIFIER, "Expected field name.").Text;

        Expression initializer = null;
        if (Match(TokenType.ASSIGN))
        {
            initializer = ParseExpression();
        }

        Consume(TokenType.SEMICOLON, "Expected ';' after field declaration.");
        return new VariableDeclaration(type, name, initializer, isPublic);
    }

    private FunctionDeclaration ParseFunctionDeclaration()
    {
        Consume(TokenType.KEYWORD_FN, "Expected 'fn' keyword.");
        bool isPublic = Match(TokenType.KEYWORD_PUBLIC);
        var name = Consume(TokenType.IDENTIFIER, "Expected function name.").Text;

        Consume(TokenType.LPAREN, "Expected '(' after function name.");
        var parameters = new List<ParameterDeclaration>();
        if (!Check(TokenType.RPAREN))
        {
            do
            {
                var paramType = ParseType();
                var paramName = Consume(TokenType.IDENTIFIER, "Expected parameter name.").Text;
                parameters.Add(new ParameterDeclaration(paramType, paramName));
            } while (Match(TokenType.COMMA));
        }

        Consume(TokenType.RPAREN, "Expected ')' after parameters.");

        string returnType = "void";
        if (Match(TokenType.ARROW))
        {
            returnType = ParseType();
        }

        Consume(TokenType.BRACE_OPEN, "Expected '{' before function body.");
        var body = ParseBlock();

        return new FunctionDeclaration(name, returnType, parameters, body, isPublic);
    }

    private string ParseType()
    {
        if (Match(TokenType.KEYWORD_INT))
            return ParseArrayModifiers("int");
        if (Match(TokenType.KEYWORD_FLOAT))
            return ParseArrayModifiers("float");
        if (Match(TokenType.KEYWORD_BOOL))
            return ParseArrayModifiers("bool");
        if (Match(TokenType.KEYWORD_CHAR))
            return ParseArrayModifiers("char");
        if (Match(TokenType.KEYWORD_STRING))
            return ParseArrayModifiers("string");
        if (Match(TokenType.KEYWORD_VOID))
            return "void";

        if (Match(TokenType.IDENTIFIER))
            return ParseArrayModifiers(Previous().Text);

        const string msg = "Expected type name.";
        Diagnostics.Add(new ParserDiagnostic(ParserDiagnosticLevel.Error, msg, Current));
        throw new ParserException(msg, Current);
    }

    private string ParseArrayModifiers(string baseType)
    {
        while (Match(TokenType.BRACKET_OPEN))
        {
            Consume(TokenType.BRACKET_CLOSE, "Expected ']' after '[' in type declaration.");
            baseType += "[]";
        }

        return baseType;
    }

    // === Разбор инструкций (Statements) ===

    private Statement ParseStatement()
    {
        if (Match(TokenType.KEYWORD_IF))
            return ParseIfStatement();
        if (Match(TokenType.KEYWORD_WHILE))
            return ParseWhileStatement();
        if (Match(TokenType.KEYWORD_FOR))
            return ParseForStatement();
        if (Match(TokenType.KEYWORD_RETURN))
            return ParseReturnStatement();
        if (Check(TokenType.BRACE_OPEN))
        {
            Consume(TokenType.BRACE_OPEN, "Expected '{'.");
            return ParseBlock();
        }

        if (IsTypeStart(Current))
        {
            return ParseVariableDeclaration();
        }

        return ParseExpressionStatement();
    }

    private static bool IsTypeStart(Token token)
    {
        return token.IsAny(
            TokenType.KEYWORD_INT,
            TokenType.KEYWORD_FLOAT,
            TokenType.KEYWORD_BOOL,
            TokenType.KEYWORD_CHAR,
            TokenType.KEYWORD_STRING
        );
    }

    private VariableDeclaration ParseVariableDeclaration()
    {
        var type = ParseType();
        var name = Consume(TokenType.IDENTIFIER, "Expected variable name.").Text;

        Expression? initializer = null;
        if (Match(TokenType.ASSIGN))
        {
            initializer = ParseExpression();
        }

        Consume(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
        return new VariableDeclaration(type, name, initializer);
    }

    private BlockStatement ParseBlock()
    {
        var statements = new List<Statement>();

        while (!Check(TokenType.BRACE_CLOSE) && !IsAtEnd)
        {
            try
            {
                statements.Add(ParseStatement());
            }
            catch (ParserException)
            {
                Synchronize();
            }
        }

        Consume(TokenType.BRACE_CLOSE, "Expected '}' after block.");
        return new BlockStatement(statements);
    }

    private IfStatement ParseIfStatement()
    {
        Consume(TokenType.LPAREN, "Expected '(' after 'if'.");
        var condition = ParseExpression();
        Consume(TokenType.RPAREN, "Expected ')' after if condition.");

        var thenBranch = ParseStatement();
        Statement elseBranch = null;
        if (Match(TokenType.KEYWORD_ELSE))
        {
            elseBranch = ParseStatement();
        }

        return new IfStatement(condition, thenBranch, elseBranch);
    }

    private WhileStatement ParseWhileStatement()
    {
        Consume(TokenType.LPAREN, "Expected '(' after 'while'.");
        var condition = ParseExpression();
        Consume(TokenType.RPAREN, "Expected ')' after while condition.");
        var body = ParseStatement();
        return new WhileStatement(condition, body);
    }

    private ForStatement ParseForStatement()
    {
        Consume(TokenType.LPAREN, "Expected '(' after 'for'.");
        Statement initializer = null;
        if (Match(TokenType.SEMICOLON))
        {
            initializer = null;
        }
        else if (IsTypeStart(Current))
        {
            initializer = ParseVariableDeclaration();
        }
        else
        {
            initializer = ParseExpressionStatement();
        }

        Expression condition = null;
        if (!Check(TokenType.SEMICOLON))
        {
            condition = ParseExpression();
        }

        Consume(TokenType.SEMICOLON, "Expected ';' after loop condition.");

        Expression increment = null;
        if (!Check(TokenType.RPAREN))
        {
            increment = ParseExpression();
        }

        Consume(TokenType.RPAREN, "Expected ')' after for clauses.");

        var body = ParseStatement();
        return new ForStatement(initializer, condition, increment, body);
    }

    private ReturnStatement ParseReturnStatement()
    {
        Expression value = null;
        if (!Check(TokenType.SEMICOLON))
        {
            value = ParseExpression();
        }

        Consume(TokenType.SEMICOLON, "Expected ';' after return value.");
        return new ReturnStatement(value);
    }

    private ExpressionStatement ParseExpressionStatement()
    {
        var expr = ParseExpression();
        Consume(TokenType.SEMICOLON, "Expected ';' after expression.");
        return new ExpressionStatement(expr);
    }

    // === Разбор выражений (Expressions) ===

    private Expression ParseExpression() => ParseAssignment();

    private Expression ParseAssignment()
    {
        var expr = ParseLogicalOr();

        if (Match(TokenType.ASSIGN))
        {
            var equals = Previous();
            var value = ParseAssignment();

            if (expr is IdentifierExpression ||
                expr is ArrayAccessExpression ||
                expr is MemberAccessExpression)
            {
                return new BinaryExpression(expr, equals, value);
            }

            Diagnostics.Add(new ParserDiagnostic(ParserDiagnosticLevel.Error, "Invalid assignment target.", equals));
        }

        return expr;
    }

    private Expression ParseLogicalOr()
    {
        var expr = ParseLogicalAnd();
        while (Match(TokenType.OR))
        {
            var op = Previous();
            var right = ParseLogicalAnd();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private Expression ParseLogicalAnd()
    {
        var expr = ParseEquality();
        while (Match(TokenType.AND))
        {
            var op = Previous();
            var right = ParseEquality();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private Expression ParseEquality()
    {
        var expr = ParseComparison();
        while (Match(TokenType.EQUAL, TokenType.NOT_EQUAL))
        {
            var op = Previous();
            var right = ParseComparison();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private Expression ParseComparison()
    {
        var expr = ParseTerm();
        while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
        {
            var op = Previous();
            var right = ParseTerm();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private Expression ParseTerm()
    {
        var expr = ParseFactor();
        while (Match(TokenType.MINUS, TokenType.PLUS))
        {
            var op = Previous();
            var right = ParseFactor();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private Expression ParseFactor()
    {
        var expr = ParseUnary();
        while (Match(TokenType.SLASH, TokenType.STAR, TokenType.MODULO))
        {
            var op = Previous();
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenType.NOT, TokenType.MINUS))
        {
            var op = Previous();
            var right = ParseUnary();
            return new UnaryExpression(op, right);
        }

        return ParseCallAndAccess();
    }

    private Expression ParseCallAndAccess()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(TokenType.LPAREN))
            {
                expr = FinishCall(expr);
            }
            else if (Match(TokenType.BRACKET_OPEN))
            {
                var index = ParseExpression();
                Consume(TokenType.BRACKET_CLOSE, "Expected ']' after array index.");
                expr = new ArrayAccessExpression(expr, index);
            }
            else if (Match(TokenType.DOT))
            {
                var name = Consume(TokenType.IDENTIFIER, "Expected property name after '.'.");
                expr = new MemberAccessExpression(expr, name.Text);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private Expression FinishCall(Expression callee)
    {
        var arguments = new List<Expression>();
        if (!Check(TokenType.RPAREN))
        {
            do
            {
                arguments.Add(ParseExpression());
            } while (Match(TokenType.COMMA));
        }

        Consume(TokenType.RPAREN, "Expected ')' after arguments.");
        return new CallExpression(callee, arguments);
    }

    private Expression ParsePrimary()
    {
        if (Match(TokenType.KEYWORD_NEW))
            return ParseNewExpression();

        // Исправление: добавили BOOL_LITERAL и правильную обработку значений
        if (Match(TokenType.NUMBER, TokenType.FLOAT_LITERAL, TokenType.STRING_LITERAL, TokenType.CHAR_LITERAL,
                TokenType.BOOL_LITERAL))
        {
            var token = Previous();
            if (token.Type == TokenType.NUMBER)
                return new LiteralExpression(token.GetNumericValue(), token);
            if (token.Type == TokenType.FLOAT_LITERAL)
                return new LiteralExpression(token.GetNumericValue(), token);
            if (token.Type == TokenType.BOOL_LITERAL)
                return new LiteralExpression(token.GetBoolValue(), token);

            // Строки и символы
            return new LiteralExpression(token.Type == TokenType.STRING_LITERAL ? token.GetStringValue() : token.Text,
                token);
        }

        if (Match(TokenType.IDENTIFIER))
        {
            return new IdentifierExpression(Previous());
        }

        if (Match(TokenType.LPAREN))
        {
            var expr = ParseExpression();
            Consume(TokenType.RPAREN, "Expected ')' after expression.");
            return expr;
        }

        // Исправление: обязательно добавляем ошибку в список перед выбросом исключения
        var msg = "Expected expression.";
        Diagnostics.Add(new ParserDiagnostic(ParserDiagnosticLevel.Error, msg, Current));
        throw new ParserException(msg, Current);
    }

    private Expression ParseNewExpression()
    {
        var type = ParseType(); // Съедает "int" или имя типа

        if (Match(TokenType.BRACKET_OPEN))
        {
            var size = ParseExpression();
            Consume(TokenType.BRACKET_CLOSE, "Expected ']' after array size.");
            return new NewArrayExpression(type, size);
        }

        if (Match(TokenType.LPAREN))
        {
            var args = new List<Expression>();
            if (!Check(TokenType.RPAREN))
            {
                do
                {
                    args.Add(ParseExpression());
                } while (Match(TokenType.COMMA));
            }

            Consume(TokenType.RPAREN, "Expected ')' after constructor arguments.");
            return new NewObjectExpression(type, args);
        }

        const string msg = "Expected array size or constructor call after 'new'.";
        Diagnostics.Add(new ParserDiagnostic(ParserDiagnosticLevel.Error, msg, Current));
        throw new ParserException(msg, Current);
    }
}