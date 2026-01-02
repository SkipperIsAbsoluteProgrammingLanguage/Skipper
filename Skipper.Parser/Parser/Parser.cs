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

    private List<ParserDiagnostic> _diagnostics = null!;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    public ParserResult Parse()
    {
        _diagnostics = [];
        var declarations = new List<Declaration>();

        while (!IsAtEnd)
        {
            try
            {
                declarations.Add(ParseDeclaration());
            }
            catch (ParserException ex)
            {
                Report(ex.Message, ex.Token);
                Synchronize();
            }
        }

        var root = new ProgramNode(declarations);
        return new ParserResult(root, _diagnostics);
    }

    private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens.Last();

    private Token Previous => _position > 0 ? _tokens[_position - 1] : _tokens[0];

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
        {
            _position++;
        }

        return Previous;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw new ParserException(message, Current);
    }

    private void Report(string message, Token? token)
    {
        _diagnostics.Add(
            new ParserDiagnostic(ParserDiagnosticLevel.Error, message, token)
        );
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd)
        {
            if (Previous.Type == TokenType.SEMICOLON ||
                Current.Type is
                    TokenType.KEYWORD_CLASS or
                    TokenType.KEYWORD_FN or
                    TokenType.KEYWORD_INT or
                    TokenType.KEYWORD_BOOL or
                    TokenType.KEYWORD_FLOAT or
                    TokenType.KEYWORD_STRING or
                    TokenType.KEYWORD_IF or
                    TokenType.KEYWORD_WHILE or
                    TokenType.KEYWORD_FOR or
                    TokenType.KEYWORD_RETURN)
            {
                return;
            }

            Advance();
        }
    }

    private Declaration ParseDeclaration()
    {
        if (Match(TokenType.KEYWORD_CLASS))
        {
            return ParseClassDeclaration();
        }

        if (Match(TokenType.KEYWORD_FN))
        {
            return ParseFunctionDeclaration();
        }

        var msg = $"Unexpected token at top level: {Current.Text}";
        throw new ParserException(msg, Current);
    }

    private ClassDeclaration ParseClassDeclaration()
    {
        var name = Consume(TokenType.IDENTIFIER, "Expected class name.").Text;
        Consume(TokenType.BRACE_OPEN, "Expected '{' before class body.");

        var members = new List<Declaration>();
        while (!Check(TokenType.BRACE_CLOSE) && !IsAtEnd)
        {
            if (Match(TokenType.KEYWORD_FN))
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
        var isPublic = Match(TokenType.KEYWORD_PUBLIC);
        var type = ParseType();
        var name = Consume(TokenType.IDENTIFIER, "Expected field name.").Text;

        Expression? initializer = null;
        if (Match(TokenType.ASSIGN))
        {
            initializer = ParseExpression();
        }

        Consume(TokenType.SEMICOLON, "Expected ';' after field declaration.");
        return new VariableDeclaration(type, name, initializer, isPublic);
    }

    private FunctionDeclaration ParseFunctionDeclaration()
    {
        var isPublic = Match(TokenType.KEYWORD_PUBLIC);
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

        var returnType = "void";
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
            return ParseArrayModifiers(Previous.Text);

        throw new ParserException("Expected type name.", Current);
    }
    
    private string ParseTypeWithoutArrayModifiers()
    {
        if (Match(TokenType.KEYWORD_INT)) return "int";
        if (Match(TokenType.KEYWORD_FLOAT)) return "float";
        if (Match(TokenType.KEYWORD_BOOL)) return "bool";
        if (Match(TokenType.KEYWORD_CHAR)) return "char";
        if (Match(TokenType.KEYWORD_STRING)) return "string";
        if (Match(TokenType.IDENTIFIER)) return Previous.Text;

        throw new ParserException("Expected type name.", Current);
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
        if (Match(TokenType.BRACE_OPEN))
            return ParseBlock();

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
            catch (ParserException ex)
            {
                Report(ex.Message, ex.Token);
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
        Statement? elseBranch = null;
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

        Statement? initializer;
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

        Expression? condition = null;
        if (!Check(TokenType.SEMICOLON))
        {
            condition = ParseExpression();
        }

        Consume(TokenType.SEMICOLON, "Expected ';' after loop condition.");

        Expression? increment = null;
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
        Expression? value = null;
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

    private Expression ParseExpression() => ParseAssignment();

    private Expression ParseAssignment()
    {
        var expr = ParseTernary();

        if (Match(TokenType.ASSIGN))
        {
            var equals = Previous;
            var value = ParseAssignment();

            if (expr is IdentifierExpression or ArrayAccessExpression or MemberAccessExpression)
            {
                return new BinaryExpression(expr, equals, value);
            }

            Report("Invalid assignment target.", equals);
        }

        return expr;
    }

    private Expression ParseTernary()
    {
        var condition = ParseLogicalOr();

        if (Match(TokenType.QUESTION_MARK))
        {
            var question = Previous;

            var thenBranch = ParseExpression();

            Consume(TokenType.COLON, "Expected ':' in ternary expression.");

            var elseBranch = ParseTernary(); 

            return new TernaryExpression(condition, thenBranch, elseBranch, question);
        }

        return condition;
    }

    private Expression ParseLogicalOr()
    {
        var expr = ParseLogicalAnd();
        while (Match(TokenType.OR))
        {
            var op = Previous;
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
            var op = Previous;
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
            var op = Previous;
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
            var op = Previous;
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
            var op = Previous;
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
            var op = Previous;
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenType.NOT, TokenType.MINUS))
        {
            var op = Previous;
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

    private CallExpression FinishCall(Expression callee)
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
        {
            return ParseNewExpression();
        }

        if (Match(TokenType.NUMBER,
                TokenType.FLOAT_LITERAL,
                TokenType.STRING_LITERAL,
                TokenType.CHAR_LITERAL,
                TokenType.BOOL_LITERAL))
        {
            var token = Previous;
            if (token.Type is TokenType.NUMBER or TokenType.FLOAT_LITERAL)
                return new LiteralExpression(token.GetNumericValue(), token);
            if (token.Type == TokenType.BOOL_LITERAL)
                return new LiteralExpression(token.GetBoolValue(), token);

            return new LiteralExpression(token.Type == TokenType.STRING_LITERAL ? token.GetStringValue() : token.Text,
                token);
        }

        if (Match(TokenType.IDENTIFIER))
        {
            return new IdentifierExpression(Previous);
        }

        if (Match(TokenType.LPAREN))
        {
            var expr = ParseExpression();
            Consume(TokenType.RPAREN, "Expected ')' after expression.");
            return expr;
        }

        throw new ParserException("Expected expression.", Current);
    }

    private Expression ParseNewExpression()
    {
        var type = ParseTypeWithoutArrayModifiers();

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

        throw new ParserException("Expected array size or constructor call after 'new'.", Current);
    }
}