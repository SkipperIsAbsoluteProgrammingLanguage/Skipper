using Skipper.Lexer.Tokens;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Skipper.Parser.Visitor;
using Skipper.Semantic.Symbols;
using Skipper.Semantic.TypeSymbols;

namespace Skipper.Semantic;

public sealed class SemanticAnalyzer : IAstVisitor<TypeSymbol>
{
    private Scope _currentScope = new(null);

    private readonly Dictionary<string, ClassTypeSymbol> _classes = new();

    private ClassTypeSymbol? _currentClass;
    private bool _inMethodBody;
    private TypeSymbol _currentReturnType = BuiltinTypeSymbol.Void;

    private readonly List<SemanticDiagnostic> _diagnostics = [];
    public IReadOnlyList<SemanticDiagnostic> Diagnostics => _diagnostics;
    public bool HasErrors => _diagnostics.Count != 0;

    private void ReportError(string message, Token? token = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(SemanticDiagnosticLevel.Error, message, token));
    }

    private void EnterScope() => _currentScope = new Scope(_currentScope);

    private void ExitScope() => _currentScope = _currentScope.Parent ?? _currentScope;

    private static readonly Dictionary<string, BuiltinTypeSymbol> SBuiltinTypes = new()
    {
        ["int"] = BuiltinTypeSymbol.Int,
        ["long"] = BuiltinTypeSymbol.Long,
        ["double"] = BuiltinTypeSymbol.Double,
        ["bool"] = BuiltinTypeSymbol.Bool,
        ["char"] = BuiltinTypeSymbol.Char,
        ["string"] = BuiltinTypeSymbol.String,
        ["void"] = BuiltinTypeSymbol.Void,
    };

    private bool TryDeclare(Symbol symbol, Token? token, string errorMessage)
    {
        if (_currentScope.Declare(symbol))
        {
            return true;
        }

        ReportError(errorMessage, token);
        return false;
    }

    private void ValidateArguments(IReadOnlyList<ParameterSymbol> parameters, IReadOnlyList<Expression> arguments, Token? tokenForError)
    {
        if (parameters.Count != arguments.Count)
        {
            ReportError($"Expected {parameters.Count} arguments, got {arguments.Count}", tokenForError);
        }

        for (var i = 0; i < Math.Min(parameters.Count, arguments.Count); i++)
        {
            var at = arguments[i].Accept(this);
            var pt = parameters[i].Type;
            if (!TypeSystem.AreAssignable(at, pt))
            {
                ReportError($"Cannot convert argument {i} from '{at}' to '{pt}'", arguments[i].Token);
            }
        }
    }

    private TypeSymbol? ResolveAssignmentTargetType(Expression target)
    {
        TypeSymbol? leftTargetType = null;

        if (target is IdentifierExpression id)
        {
            var sym = _currentScope.Resolve(id.Name);
            if (sym != null)
            {
                leftTargetType = sym.Type;
            }
            else if (_currentClass != null && _currentClass.Class.Fields.TryGetValue(id.Name, out var field))
            {
                leftTargetType = field.Type;
            }
            else
            {
                ReportError($"Unknown identifier '{id.Name}'", id.Token);
            }
        }
        else if (target is MemberAccessExpression ma)
        {
            var objType = ma.Object.Accept(this);
            if (objType is ClassTypeSymbol ctype)
            {
                if (ctype.Class.Fields.TryGetValue(ma.MemberName, out var f))
                {
                    leftTargetType = f.Type;
                }
                else
                {
                    ReportError($"Member '{ma.MemberName}' not found on type '{objType}'", ma.Object.Token);
                }
            }
            else
            {
                ReportError($"Member access on non-class type '{objType}'", ma.Object.Token);
            }
        }
        else if (target is ArrayAccessExpression aa)
        {
            var t = aa.Target.Accept(this);
            if (t is ArrayTypeSymbol arr)
            {
                leftTargetType = arr.ElementType;
            }
            else
            {
                ReportError($"Indexing non-array type '{t}'", aa.Target.Token);
            }
        }

        return leftTargetType;
    }

    private static bool IsNumeric(TypeSymbol type)
    {
        return type == BuiltinTypeSymbol.Int ||
               type == BuiltinTypeSymbol.Long ||
               type == BuiltinTypeSymbol.Double;
    }

    private static TypeSymbol GetNumericResult(TypeSymbol left, TypeSymbol right)
    {
        if (left == BuiltinTypeSymbol.Double || right == BuiltinTypeSymbol.Double)
        {
            return BuiltinTypeSymbol.Double;
        }

        if (left == BuiltinTypeSymbol.Long || right == BuiltinTypeSymbol.Long)
        {
            return BuiltinTypeSymbol.Long;
        }

        return BuiltinTypeSymbol.Int;
    }

    private static TokenType GetCompoundBaseOp(TokenType op)
    {
        return op switch
        {
            TokenType.PLUS_ASSIGN => TokenType.PLUS,
            TokenType.MINUS_ASSIGN => TokenType.MINUS,
            TokenType.STAR_ASSIGN => TokenType.STAR,
            TokenType.SLASH_ASSIGN => TokenType.SLASH,
            TokenType.MODULO_ASSIGN => TokenType.MODULO,
            _ => throw new InvalidOperationException($"Unsupported compound operator '{op}'")
        };
    }

    private TypeSymbol ResolveArithmeticResult(TokenType op, TypeSymbol lt, TypeSymbol rt, Token opToken)
    {
        switch (op)
        {
            case TokenType.PLUS:
            {
                if (IsNumeric(lt) && IsNumeric(rt))
                {
                    return GetNumericResult(lt, rt);
                }

                if (lt == BuiltinTypeSymbol.String && rt == BuiltinTypeSymbol.String)
                {
                    return BuiltinTypeSymbol.String;
                }

                if ((lt == BuiltinTypeSymbol.String && (rt == BuiltinTypeSymbol.Int || rt == BuiltinTypeSymbol.Long || rt == BuiltinTypeSymbol.Double)) ||
                    ((lt == BuiltinTypeSymbol.Int || lt == BuiltinTypeSymbol.Long || lt == BuiltinTypeSymbol.Double) && rt == BuiltinTypeSymbol.String))
                {
                    return BuiltinTypeSymbol.String;
                }

                ReportError($"Operator '{opToken.Text}' requires numeric operands", opToken);
                return BuiltinTypeSymbol.Void;
            }

            case TokenType.MINUS:
            case TokenType.STAR:
            case TokenType.SLASH:
            case TokenType.MODULO:
            {
                if (IsNumeric(lt) && IsNumeric(rt))
                {
                    return GetNumericResult(lt, rt);
                }

                ReportError($"Operator '{opToken.Text}' requires numeric operands", opToken);
                return BuiltinTypeSymbol.Void;
            }

            default:
                ReportError($"Unsupported binary operator '{opToken.Text}'", opToken);
                return BuiltinTypeSymbol.Void;
        }
    }

    private TypeSymbol ResolveTypeByName(string name, Token? token = null)
    {
        if (name.EndsWith("[]"))
        {
            var elementName = name[..^2];
            var element = ResolveTypeByName(elementName, token);
            return TypeFactory.Array(element);
        }

        if (SBuiltinTypes.TryGetValue(name, out var bt))
        {
            return bt;
        }

        if (_classes.TryGetValue(name, out var cls))
        {
            return cls;
        }

        return ReportUnknownType(name, token);
    }

    private BuiltinTypeSymbol ReportUnknownType(string name, Token? token)
    {
        ReportError($"Unknown type '{name}'", token);
        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitProgram(ProgramNode node)
    {
        foreach (var decl in node.Declarations.Where(x => x.NodeType == AstNodeType.ClassDeclaration))
        {
            if (_classes.ContainsKey(decl.Name))
            {
                ReportError($"Class '{decl.Name}' already defined", decl.Token);
                continue;
            }

            var classSymbol = new ClassSymbol(decl.Name);
            _classes[decl.Name] = classSymbol.ClassType;
        }

        _currentScope = new Scope(null);

        foreach (var decl in node.Declarations)
        {
            if (decl.NodeType == AstNodeType.FunctionDeclaration)
            {
                var fn = (FunctionDeclaration)decl;
                if (_currentScope.Resolve(fn.Name) is not null)
                {
                    ReportError($"Function '{fn.Name}' already declared", fn.Token);
                    break;
                }

                var returnType = ResolveTypeByName(fn.ReturnType, fn.Token);
                var parameters = new List<ParameterSymbol>();
                var seen = new HashSet<string>();
                foreach (var p in fn.Parameters)
                {
                    if (!seen.Add(p.Name))
                    {
                        ReportError($"Parameter '{p.Name}' already declared", p.Token);
                    }

                    var pt = ResolveTypeByName(p.TypeName, p.Token);
                    parameters.Add(new ParameterSymbol(p.Name, pt));
                }

                var function = new FunctionSymbol(fn.Name, returnType, parameters);
                TryDeclare(function, fn.Token, $"Function '{fn.Name}' already declared in scope");
            }
            else if (decl.NodeType == AstNodeType.VariableDeclaration)
            {
                var v = (VariableDeclaration)decl;
                var t = ResolveTypeByName(v.TypeName, v.Token);
                var variable = new VariableSymbol(v.Name, t);
                TryDeclare(variable, v.Token, $"Variable '{v.Name}' already declared in this scope");
            }
        }

        foreach (var decl in node.Declarations)
        {
            decl.Accept(this);
        }

        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitFunctionDeclaration(FunctionDeclaration node)
    {
        var returnType = ResolveTypeByName(node.ReturnType, node.Token);

        var oldReturn = _currentReturnType;
        _currentReturnType = returnType;

        EnterScope();

        var seenParams = new HashSet<string>();
        foreach (var p in node.Parameters)
        {
            if (!seenParams.Add(p.Name))
            {
                ReportError($"Parameter '{p.Name}' already declared", p.Token);
            }

            var pt = ResolveTypeByName(p.TypeName, p.Token);
            var psym = new ParameterSymbol(p.Name, pt);
            TryDeclare(psym, p.Token, $"Parameter '{p.Name}' already declared");
        }

        node.Body.Accept(this);

        if (_currentReturnType != BuiltinTypeSymbol.Void && !StatementAlwaysReturns(node.Body))
        {
            ReportError(
                $"Not all code paths return a value for function '{node.Name}' returning '{_currentReturnType}'",
                node.Token);
        }

        ExitScope();
        _currentReturnType = oldReturn;

        return returnType;
    }

    public TypeSymbol VisitVariableDeclaration(VariableDeclaration node)
    {
        var type = ResolveTypeByName(node.TypeName, node.Token);

        if (node.Initializer != null)
        {
            var initType = node.Initializer.Accept(this);
            if (!TypeSystem.AreAssignable(initType, type))
            {
                ReportError($"Cannot assign '{initType}' to variable of type '{type}'", node.Token);
            }
        }

        if (_currentClass != null && !_inMethodBody)
        {
            var cls = _currentClass.Class;
            if (cls.Fields.ContainsKey(node.Name) || cls.Methods.ContainsKey(node.Name))
            {
                ReportError($"Member '{node.Name}' already declared in class '{cls.Name}'", node.Token);
            }
            else
            {
                cls.Fields[node.Name] = new FieldSymbol(node.Name, type);
            }

            return type;
        }

        var variable = new VariableSymbol(node.Name, type);
        TryDeclare(variable, node.Token, $"Variable '{node.Name}' already declared in this scope");

        return type;
    }

    public TypeSymbol VisitClassDeclaration(ClassDeclaration node)
    {
        if (!_classes.TryGetValue(node.Name, out var classType))
        {
            ReportError($"Unknown class '{node.Name}'", node.Token);
            return BuiltinTypeSymbol.Void;
        }

        var cls = classType.Class;

        foreach (var member in node.Members)
        {
            if (member.NodeType == AstNodeType.VariableDeclaration)
            {
                var field = (VariableDeclaration)member;
                var fieldType = ResolveTypeByName(field.TypeName, field.Token);
                if (cls.Fields.ContainsKey(field.Name) || cls.Methods.ContainsKey(field.Name))
                {
                    ReportError($"Member '{field.Name}' already declared in class '{cls.Name}'", field.Token);
                    continue;
                }

                cls.Fields[field.Name] = new FieldSymbol(field.Name, fieldType);
            }
            else if (member.NodeType == AstNodeType.FunctionDeclaration)
            {
                var method = (FunctionDeclaration)member;
                if (cls.Methods.ContainsKey(method.Name))
                {
                    ReportError($"Method '{method.Name}' already declared in class '{cls.Name}'", method.Token);
                    continue;
                }

                var rtype = ResolveTypeByName(method.ReturnType, method.Token);
                var parameters = new List<ParameterSymbol>();
                foreach (var p in method.Parameters)
                {
                    var pt = ResolveTypeByName(p.TypeName, p.Token);
                    parameters.Add(new ParameterSymbol(p.Name, pt));
                }

                cls.Methods[method.Name] = new MethodSymbol(method.Name, rtype, parameters);
            }
            else
            {
                ReportError($"Unsupported class member '{member.NodeType}'", member.Token);
            }
        }

        var outerClass = _currentClass;
        _currentClass = classType;

        foreach (var member in node.Members)
        {
            if (member.NodeType != AstNodeType.FunctionDeclaration)
            {
                continue;
            }

            var m = (FunctionDeclaration)member;

            var methodSym = cls.Methods[m.Name];

            var oldReturn = _currentReturnType;
            _currentReturnType = methodSym.Type;

            EnterScope();

            foreach (var p in m.Parameters)
            {
                var pt = ResolveTypeByName(p.TypeName, p.Token);
                TryDeclare(new ParameterSymbol(p.Name, pt), p.Token, $"Parameter '{p.Name}' already declared");
            }

            _inMethodBody = true;
            m.Body.Accept(this);
            _inMethodBody = false;

            if (methodSym.Type != BuiltinTypeSymbol.Void && !StatementAlwaysReturns(m.Body))
            {
                ReportError($"Not all code paths return a value for method '{m.Name}' returning '{methodSym.Type}'",
                    m.Token);
            }

            ExitScope();

            _currentReturnType = oldReturn;
        }

        _currentClass = outerClass;

        return classType;
    }

    public TypeSymbol VisitParameterDeclaration(ParameterDeclaration node)
    {
        var t = ResolveTypeByName(node.TypeName, node.Token);
        var sym = new ParameterSymbol(node.Name, t);
        TryDeclare(sym, node.Token, $"Parameter '{node.Name}' already declared");

        return t;
    }

    public TypeSymbol VisitBlockStatement(BlockStatement node)
    {
        EnterScope();
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }

        ExitScope();
        return BuiltinTypeSymbol.Void;
    }

    private static bool StatementAlwaysReturns(Statement stmt)
    {
        while (true)
        {
            if (stmt.NodeType == AstNodeType.ReturnStatement)
            {
                return true;
            }

            if (stmt.NodeType == AstNodeType.BlockStatement)
            {
                var bs = (BlockStatement)stmt;
                if (bs.Statements.Count == 0) return false;
                stmt = bs.Statements[^1];
                continue;
            }

            if (stmt.NodeType == AstNodeType.IfStatement)
            {
                var ifs = (IfStatement)stmt;
                if (ifs.ElseBranch == null) return false;
                return StatementAlwaysReturns(ifs.ThenBranch) && StatementAlwaysReturns(ifs.ElseBranch);
            }

            return false;
        }
    }

    public TypeSymbol VisitIfStatement(IfStatement node)
    {
        var cond = node.Condition.Accept(this);
        if (!TypeSystem.AreAssignable(cond, BuiltinTypeSymbol.Bool))
        {
            ReportError($"Condition expression must be 'bool', got '{cond}'", node.Condition.Token);
        }

        node.ThenBranch.Accept(this);
        node.ElseBranch?.Accept(this);
        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitWhileStatement(WhileStatement node)
    {
        var cond = node.Condition.Accept(this);
        if (!TypeSystem.AreAssignable(cond, BuiltinTypeSymbol.Bool))
        {
            ReportError($"Condition expression must be 'bool', got '{cond}'", node.Condition.Token);
        }

        node.Body.Accept(this);
        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitForStatement(ForStatement node)
    {
        EnterScope();
        node.Initializer?.Accept(this);

        if (node.Condition != null)
        {
            var cond = node.Condition.Accept(this);
            if (!TypeSystem.AreAssignable(cond, BuiltinTypeSymbol.Bool))
            {
                ReportError($"Condition expression must be 'bool', got '{cond}'", node.Condition.Token);
            }
        }

        node.Body.Accept(this);
        ExitScope();
        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitReturnStatement(ReturnStatement node)
    {
        if (node.Value == null)
        {
            if (_currentReturnType != BuiltinTypeSymbol.Void)
            {
                ReportError($"Return statement missing a value for function returning '{_currentReturnType}'",
                    node.Token);
            }

            return BuiltinTypeSymbol.Void;
        }

        var valueType = node.Value.Accept(this);
        if (!TypeSystem.AreAssignable(valueType, _currentReturnType))
        {
            ReportError($"Cannot return value of type '{valueType}' from function returning '{_currentReturnType}'",
                node.Value.Token);
        }

        return valueType;
    }

    public TypeSymbol VisitExpressionStatement(ExpressionStatement node)
    {
        return node.Expression.Accept(this);
    }

    public TypeSymbol VisitBinaryExpression(BinaryExpression node)
    {
        var lt = node.Left.Accept(this);
        var rt = node.Right.Accept(this);

        switch (node.Operator.Type)
        {
            case TokenType.PLUS:
            case TokenType.MINUS:
            case TokenType.STAR:
            case TokenType.SLASH:
            case TokenType.MODULO:
                return ResolveArithmeticResult(node.Operator.Type, lt, rt, node.Operator);

            case TokenType.EQUAL:
            case TokenType.NOT_EQUAL:
            case TokenType.LESS:
            case TokenType.GREATER:
            case TokenType.LESS_EQUAL:
            case TokenType.GREATER_EQUAL:
            {
                if (IsNumeric(lt) && IsNumeric(rt))
                {
                    return BuiltinTypeSymbol.Bool;
                }

                if (lt == rt || (lt is ArrayTypeSymbol fa && rt is ArrayTypeSymbol fb &&
                                 fa.ElementType == fb.ElementType))
                {
                    return BuiltinTypeSymbol.Bool;
                }

                ReportError($"Cannot compare values of types '{lt}' and '{rt}'", node.Operator);
                return BuiltinTypeSymbol.Bool;
            }

            case TokenType.AND:
            case TokenType.OR:
            {
                if (TypeSystem.AreAssignable(lt, BuiltinTypeSymbol.Bool) &&
                    TypeSystem.AreAssignable(rt, BuiltinTypeSymbol.Bool))
                {
                    return BuiltinTypeSymbol.Bool;
                }

                ReportError($"Logical operators require boolean operands", node.Operator);
                return BuiltinTypeSymbol.Bool;
            }

            case TokenType.ASSIGN:
            {
                var leftTargetType = ResolveAssignmentTargetType(node.Left);
                if (leftTargetType == null)
                {
                    return BuiltinTypeSymbol.Void;
                }

                if (!TypeSystem.AreAssignable(rt, leftTargetType))
                {
                    ReportError($"Cannot assign value of type '{rt}' to '{leftTargetType}'", node.Operator);
                }

                return leftTargetType;
            }

            case TokenType.PLUS_ASSIGN:
            case TokenType.MINUS_ASSIGN:
            case TokenType.STAR_ASSIGN:
            case TokenType.SLASH_ASSIGN:
            case TokenType.MODULO_ASSIGN:
            {
                var leftTargetType = ResolveAssignmentTargetType(node.Left);
                if (leftTargetType == null)
                {
                    return BuiltinTypeSymbol.Void;
                }

                var baseOp = GetCompoundBaseOp(node.Operator.Type);

                var resultType = ResolveArithmeticResult(baseOp, leftTargetType, rt, node.Operator);
                if (!TypeSystem.AreAssignable(resultType, leftTargetType))
                {
                    ReportError($"Cannot assign value of type '{resultType}' to '{leftTargetType}'", node.Operator);
                }

                return leftTargetType;
            }

            default:
                ReportError($"Unsupported binary operator '{node.Operator.Text}'", node.Operator);
                return BuiltinTypeSymbol.Void;
        }
    }

    public TypeSymbol VisitUnaryExpression(UnaryExpression node)
    {
        var ot = node.Operand.Accept(this);
        switch (node.Operator.Type)
        {
            case TokenType.MINUS:
            {
                if (ot == BuiltinTypeSymbol.Int || ot == BuiltinTypeSymbol.Long || ot == BuiltinTypeSymbol.Double)
                {
                    return ot;
                }

                ReportError("Unary '-' requires numeric operand", node.Operator);
                return BuiltinTypeSymbol.Void;
            }

            case TokenType.INCREMENT:
            case TokenType.DECREMENT:
            {
                var leftTargetType = ResolveAssignmentTargetType(node.Operand);
                if (leftTargetType == null)
                {
                    ReportError($"Invalid {node.Operator.Text} target", node.Operator);
                    return BuiltinTypeSymbol.Void;
                }

                if (leftTargetType != BuiltinTypeSymbol.Int &&
                    leftTargetType != BuiltinTypeSymbol.Long &&
                    leftTargetType != BuiltinTypeSymbol.Double)
                {
                    ReportError($"Operator '{node.Operator.Text}' requires numeric operand", node.Operator);
                    return BuiltinTypeSymbol.Void;
                }

                return leftTargetType;
            }

            case TokenType.NOT:
            {
                if (TypeSystem.AreAssignable(ot, BuiltinTypeSymbol.Bool))
                {
                    return BuiltinTypeSymbol.Bool;
                }

                ReportError("Unary '!' requires boolean operand", node.Operator);
                return BuiltinTypeSymbol.Void;
            }

            default:
                ReportError($"Unsupported unary operator '{node.Operator.Text}'", node.Operator);
                return BuiltinTypeSymbol.Void;
        }
    }

    public TypeSymbol VisitLiteralExpression(LiteralExpression node)
    {
        return node.Value switch
        {
            int => BuiltinTypeSymbol.Int,
            long => BuiltinTypeSymbol.Long,
            double => BuiltinTypeSymbol.Double,
            float => BuiltinTypeSymbol.Double,
            bool => BuiltinTypeSymbol.Bool,
            char => BuiltinTypeSymbol.Char,
            string => BuiltinTypeSymbol.String,
            _ => BuiltinTypeSymbol.Void
        };
    }

    public TypeSymbol VisitIdentifierExpression(IdentifierExpression node)
    {
        var sym = _currentScope.Resolve(node.Name);
        if (sym != null)
        {
            return sym.Type;
        }

        // Если не нашли, и мы внутри класса — ищем в полях (неявный this)
        if (_currentClass != null)
        {
            if (_currentClass.Class.Fields.TryGetValue(node.Name, out var field))
            {
                return field.Type;
            }
        }

        ReportError($"Unknown identifier '{node.Name}'", node.Token);
        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitCallExpression(CallExpression node)
    {
        if (node.Callee is IdentifierExpression id)
        {
            if (TryHandleBuiltinCall(id, node.Arguments, out var builtinReturn))
            {
                return builtinReturn;
            }

            var sym = _currentScope.Resolve(id.Name);

            if (sym != null)
            {
                if (sym is FunctionSymbol fs)
                {
                    ValidateArguments(fs.Parameters, node.Arguments, id.Token);
                    return fs.Type;
                }

                ReportError($"'{id.Name}' is not a function", id.Token);
                return BuiltinTypeSymbol.Void;
            }

            if (_currentClass != null)
            {
                if (_currentClass.Class.Methods.TryGetValue(id.Name, out var method))
                {
                    ValidateArguments(method.Parameters, node.Arguments, id.Token);
                    return method.Type;
                }
            }

            ReportError($"Unknown function or method '{id.Name}'", id.Token);
            return BuiltinTypeSymbol.Void;
        }

        if (node.Callee is MemberAccessExpression mae)
        {
            var objType = mae.Object.Accept(this);
            if (objType is ClassTypeSymbol ctype)
            {
                if (!ctype.Class.Methods.TryGetValue(mae.MemberName, out var method))
                {
                    ReportError($"Method '{mae.MemberName}' not found on type '{ctype}'", mae.Object.Token);
                    return BuiltinTypeSymbol.Void;
                }

                ValidateArguments(method.Parameters, node.Arguments, mae.Object.Token);
                return method.Type;
            }

            ReportError($"Cannot call member '{mae.MemberName}' on non-class type '{objType}'", mae.Object.Token);
            return BuiltinTypeSymbol.Void;
        }

        ReportError("Unsupported call expression", node.Callee.Token);
        return BuiltinTypeSymbol.Void;
    }

    private bool TryHandleBuiltinCall(
        IdentifierExpression id,
        IReadOnlyList<Expression> arguments,
        out TypeSymbol returnType)
    {
        returnType = BuiltinTypeSymbol.Void;

        if (id.Name == "print")
        {
            if (arguments.Count == 1)
            {
                arguments[0].Accept(this);
            }
            else if (arguments.Count > 1)
            {
                foreach (var arg in arguments)
                {
                    arg.Accept(this);
                }

                ReportError($"Expected 0 or 1 arguments, got {arguments.Count}", id.Token);
            }

            returnType = BuiltinTypeSymbol.Void;
            return true;
        }

        if (id.Name == "println")
        {
            if (arguments.Count == 1)
            {
                arguments[0].Accept(this);
            }
            else if (arguments.Count > 1)
            {
                foreach (var arg in arguments)
                {
                    arg.Accept(this);
                }

                ReportError($"Expected 0 or 1 arguments, got {arguments.Count}", id.Token);
            }

            returnType = BuiltinTypeSymbol.Void;
            return true;
        }

        if (id.Name == "time")
        {
            foreach (var arg in arguments)
            {
                arg.Accept(this);
            }

            if (arguments.Count != 0)
            {
                ReportError($"Expected 0 arguments, got {arguments.Count}", id.Token);
            }

            returnType = BuiltinTypeSymbol.Int;
            return true;
        }

        if (id.Name == "random")
        {
            if (arguments.Count != 1)
            {
                foreach (var arg in arguments)
                {
                    arg.Accept(this);
                }

                ReportError($"Expected 1 arguments, got {arguments.Count}", id.Token);
                returnType = BuiltinTypeSymbol.Int;
                return true;
            }

            var argType = arguments[0].Accept(this);
            if (!TypeSystem.AreAssignable(argType, BuiltinTypeSymbol.Int))
            {
                ReportError($"Cannot convert argument 0 from '{argType}' to '{BuiltinTypeSymbol.Int}'", arguments[0].Token);
            }

            returnType = BuiltinTypeSymbol.Int;
            return true;
        }

        return false;
    }

    public TypeSymbol VisitTernaryExpression(TernaryExpression node)
    {
        var cond = node.Condition.Accept(this);
        if (!TypeSystem.AreAssignable(cond, BuiltinTypeSymbol.Bool))
        {
            ReportError($"Condition expression must be 'bool', got '{cond}'", node.Condition.Token);
        }

        var t = node.ThenBranch.Accept(this);
        var e = node.ElseBranch.Accept(this);
        if (TypeSystem.AreAssignable(t, e)) return e;
        if (TypeSystem.AreAssignable(e, t)) return t;

        ReportError($"Incompatible types in ternary expression: '{t}' and '{e}'", node.Token);
        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitArrayAccessExpression(ArrayAccessExpression node)
    {
        var target = node.Target.Accept(this);
        var index = node.Index.Accept(this);
        if (!TypeSystem.AreAssignable(index, BuiltinTypeSymbol.Int))
        {
            ReportError($"Array index must be 'int', got '{index}'", node.Index.Token);
        }

        if (target is ArrayTypeSymbol arr)
        {
            return arr.ElementType;
        }

        ReportError($"Type '{target}' is not an array", node.Target.Token);
        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitMemberAccessExpression(MemberAccessExpression node)
    {
        var objType = node.Object.Accept(this);
        if (objType is ClassTypeSymbol ctype)
        {
            if (ctype.Class.Fields.TryGetValue(node.MemberName, out var f))
            {
                return f.Type;
            }

            if (ctype.Class.Methods.TryGetValue(node.MemberName, out var m))
            {
                return m.Type;
            }

            ReportError($"Member '{node.MemberName}' not found on type '{objType}'", node.Object.Token);
            return BuiltinTypeSymbol.Void;
        }

        ReportError($"Member access on non-class type '{objType}'", node.Object.Token);
        return BuiltinTypeSymbol.Void;
    }

    public TypeSymbol VisitNewArrayExpression(NewArrayExpression node)
    {
        var elem = ResolveTypeByName(node.ElementType, node.Token);
        var size = node.SizeExpression.Accept(this);
        if (!TypeSystem.AreAssignable(size, BuiltinTypeSymbol.Int))
        {
            ReportError($"Array size must be 'int', got '{size}'", node.SizeExpression.Token);
        }

        return TypeFactory.Array(elem);
    }

    public TypeSymbol VisitNewObjectExpression(NewObjectExpression node)
    {
        if (!_classes.TryGetValue(node.ClassName, out var cls))
        {
            ReportError($"Unknown class '{node.ClassName}'", node.Token);
            return BuiltinTypeSymbol.Void;
        }

        if (node.Arguments.Count != 0)
        {
            ReportError($"No constructors defined for '{node.ClassName}'", node.Token);
        }

        return cls;
    }
}
