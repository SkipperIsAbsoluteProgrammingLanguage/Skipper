using System.Linq.Expressions;
using Skipper.Lexer.Tokens;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Skipper.Parser.Visitor;
using Skipper.BaitCode.IdManager;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using BinaryExpression = Skipper.Parser.AST.Expressions.BinaryExpression;
using Expression = Skipper.Parser.AST.Expressions.Expression;
using NewArrayExpression = Skipper.Parser.AST.Expressions.NewArrayExpression;
using UnaryExpression = Skipper.Parser.AST.Expressions.UnaryExpression;

namespace Skipper.BaitCode.Generator;

public class BytecodeGenerator : IAstVisitor<BytecodeGenerator>
{
    private readonly BytecodeProgram _program = new();

    private BytecodeFunction? _currentFunction;
    private BytecodeClass? _currentClass;
    private readonly Stack<LocalSlotManager> _locals = new();
    private readonly Dictionary<string, BytecodeType> _resolvedTypes = new();
    private readonly Dictionary<string, PrimitiveType> _primitiveTypes = new();

    public BytecodeProgram Generate(ProgramNode program)
    {
        VisitProgram(program);

        return _program;
    }

    // Добавляет операцию в текущую обрабатываемую функцию
    private void Emit(OpCode opCode, params object[] operands)
    {
        if (_currentFunction == null)
            throw new InvalidOperationException("No function declared in scope");

        _currentFunction.Code.Add(new Instruction(opCode, operands));
    }

    // Обход начиная с результата работы парсера AST (корневой узел)
    public BytecodeGenerator VisitProgram(ProgramNode node)
    {
        foreach (var decl in node.Declarations)
        {
            decl.Accept(this);
        }
        return this;
    }

    private LocalSlotManager Locals => _locals.Peek();
    private void EnterScope() => Locals.EnterScope();
    private void ExitScope() => Locals.ExitScope();

    // --- Declarations ---
    public BytecodeGenerator VisitFunctionDeclaration(FunctionDeclaration node)
    {
        var function = new BytecodeFunction(
            id: _program.Functions.Count,
            name: node.Name,
            returnType: ResolveType(node.ReturnType),
            parameters: node.Parameters
                .Select(p => (p.Name, ResolveType(p.TypeName)))
                .ToList()
        );
        
        _program.Functions.Add(function);
        _currentFunction = function;
        
        _locals.Push(new LocalSlotManager());
        EnterScope();
        
        foreach (var param in node.Parameters)
            param.Accept(this);

        node.Body.Accept(this);

        ExitScope();
        _locals.Pop();

        _currentFunction = null;

        return this;
    }

    public BytecodeGenerator VisitParameterDeclaration(ParameterDeclaration node)
    {
        Locals.Declare(node.Name);
        return this;
    }

    public BytecodeGenerator VisitVariableDeclaration(VariableDeclaration node)
    {
        var slot = Locals.Declare(node.Name);

        if (node.Initializer != null)
        {
            node.Initializer.Accept(this);
            Emit(OpCode.STORE, slot);
        }

        return this;
    }

    public BytecodeGenerator VisitClassDeclaration(ClassDeclaration node)
    {
        var cls = new BytecodeClass(
            classId: _program.Classes.Count,
            name: node.Name
        );

        _program.Classes.Add(cls);
        _currentClass = cls;

        foreach (var member in node.Members)
            member.Accept(this);

        _currentClass = null;
        return this;
    }

    // --- Statements ---
    public BytecodeGenerator VisitBlockStatement(BlockStatement node)
    {
        EnterScope();

        foreach (var stmt in node.Statements)
            stmt.Accept(this);

        ExitScope();
        return this;
    }

    public BytecodeGenerator VisitExpressionStatement(ExpressionStatement node)
    {
        node.Expression.Accept(this);
        Emit(OpCode.POP);
        return this;
    }

    public BytecodeGenerator VisitReturnStatement(ReturnStatement node)
    {
        node.Value?.Accept(this);
        Emit(OpCode.RETURN);
        return this;
    }

    public BytecodeGenerator VisitIfStatement(IfStatement node) // TODO: EmitPlaceholder + Patch
    {
        node.Condition.Accept(this);

        var jumpFalse = EmitPlaceholder(OpCode.JUMP_IF_FALSE);

        node.ThenBranch.Accept(this);

        if (node.ElseBranch != null)
        {
            var jumpEnd = EmitPlaceholder(OpCode.JUMP);
            Patch(jumpFalse);
            node.ElseBranch.Accept(this);
            Patch(jumpEnd);
        }
        else
        {
            Patch(jumpFalse);
        }

        return this;
    }

    private int EmitPlaceholder(OpCode opCode)
    {
        if (_currentFunction == null)
            throw new NullReferenceException("No function declared in scope");

        var placeholderIndex = _currentFunction.Code.Count;
        Emit(opCode, 0); // 0 будет заменено позже
        return placeholderIndex;
    }

    private void Patch(int instructionIndex)
    {
        if (_currentFunction == null)
            throw new NullReferenceException("No function declared in scope");
        
        var instr = _currentFunction.Code[instructionIndex];
        _currentFunction.Code[instructionIndex] = new Instruction(
            instr.OpCode,
            _currentFunction.Code.Count
        );
    }

    /*
     * Запоминаем позицию начала цикла
     * Генерируем условие
     * JUMP_IF_FALSE → выход
     * Тело
     * JUMP → начало
     * Patch выхода
     *  (loop_start):
        evaluate condition
        JUMP_IF_FALSE -> loop_end
        body
        JUMP -> loop_start
        (loop_end):
     */
    public BytecodeGenerator VisitWhileStatement(WhileStatement node) // TODO
    {
        // начало цикла
        if (_currentFunction == null)
        {
            throw new NullReferenceException("No function declared in scope");
        }
        
        var loopStart = _currentFunction.Code.Count;

        // условие
        node.Condition.Accept(this);

        // если false → выход
        var jumpExit = EmitPlaceholder(OpCode.JUMP_IF_FALSE);

        // тело цикла
        node.Body.Accept(this);

        // прыжок обратно к условию
        Emit(OpCode.JUMP, loopStart);

        // патчим выход
        Patch(jumpExit);

        return this;
    }

    /*
     * enter scope
        initializer

        (loop_start):
            condition
            JUMP_IF_FALSE -> loop_end
            body
            increment
            JUMP -> loop_start

        (loop_end):
        exit scope

     */
    public BytecodeGenerator VisitForStatement(ForStatement node)
    {
        // for вводит собственную область видимости
        EnterScope();

        // initializer
        node.Initializer?.Accept(this);

        // начало цикла
        int loopStart = _currentFunction!.Code.Count;

        // condition (если есть)
        int jumpExit = -1;
        if (node.Condition != null)
        {
            node.Condition.Accept(this);
            jumpExit = EmitPlaceholder(OpCode.JUMP_IF_FALSE);
        }

        // тело
        node.Body.Accept(this);

        // increment
        if (node.Increment != null)
        {
            node.Increment.Accept(this);
            Emit(OpCode.POP); // т.к. increment — expression
        }

        // назад к началу
        Emit(OpCode.JUMP, loopStart);

        // выход
        if (jumpExit != -1)
            Patch(jumpExit);

        ExitScope();
        return this;
    }

    // --- Expressions ---
    public BytecodeGenerator VisitBinaryExpression(BinaryExpression node)
    {
        // Присваивание
        if (node.Operator.Type == TokenType.ASSIGN)
        {
            // 1. вычисляем r-value
            node.Right.Accept(this);

            // 2. дублируем, т.к. assignment — expression
            Emit(OpCode.DUP);

            // 3. сохраняем в l-value
            EmitStore(node.Left);

            return this;
        }
        
        node.Left.Accept(this);
        node.Right.Accept(this);
    
        // Для остальных операций
        switch (node.Operator.Type)
        {
            case TokenType.PLUS: Emit(OpCode.ADD); break;
            case TokenType.MINUS: Emit(OpCode.SUB); break;
            case TokenType.STAR: Emit(OpCode.MUL); break;
            case TokenType.SLASH: Emit(OpCode.DIV); break;
            case TokenType.MODULO: Emit(OpCode.MOD); break;
            case TokenType.EQUAL: Emit(OpCode.CMP_EQ); break;
            case TokenType.NOT_EQUAL: Emit(OpCode.CMP_NE); break;
            case TokenType.LESS: Emit(OpCode.CMP_LT); break;
            case TokenType.GREATER: Emit(OpCode.CMP_GT); break;
            case TokenType.LESS_EQUAL: Emit(OpCode.CMP_LE); break;
            case TokenType.GREATER_EQUAL: Emit(OpCode.CMP_GE); break;
            case TokenType.AND: Emit(OpCode.AND); break;
            case TokenType.OR: Emit(OpCode.OR); break;
            default:
                throw new NotSupportedException($"Operator {node.Operator.Type} not supported");
        }
    
        return this;
    }
    
    private void EmitStore(Expression target)
    {
        switch (target)
        {
            // x = value
            case IdentifierExpression id:
            {
                var slot = Locals.Resolve(id.Name);
                Emit(OpCode.STORE, slot);
                break;
            }

            // obj.field = value
            case MemberAccessExpression ma:
            {
                // stack: value
                // нужно: object, value

                // вычисляем объект
                ma.Object.Accept(this);

                // stack: value, object
                Emit(OpCode.SWAP);
                // stack: object, value

                var field = ResolveField(ma);
                Emit(OpCode.SET_FIELD, field.FieldId);
                break;
            }

            // arr[index] = value
            case ArrayAccessExpression aa:
            {
                // stack: value

                aa.Target.Accept(this); // array
                aa.Index.Accept(this);  // index

                // stack: value, array, index
                Emit(OpCode.SWAP);      // value <-> index
                // stack: index, array, value
                Emit(OpCode.SWAP);      // index <-> array
                // stack: array, index, value

                Emit(OpCode.SET_ELEMENT);
                break;
            }

            default:
                throw new InvalidOperationException(
                    $"Expression '{target.NodeType}' cannot be assigned to");
        }
    }

    public BytecodeGenerator VisitUnaryExpression(UnaryExpression node)
    {
        node.Operand.Accept(this);
        switch (node.Operator.Type)
        {
            case TokenType.NOT: Emit(OpCode.NOT); break;
            case TokenType.MINUS: Emit(OpCode.NEG); break;
            default:
                throw new NotSupportedException($"Operator {node.Operator.Type} not supported");
        }

        return this;
    }

    public BytecodeGenerator VisitLiteralExpression(LiteralExpression node)
    {
        int constId = AddConstant(node.Value);
        Emit(OpCode.PUSH, constId);
        return this;
    }

    public BytecodeGenerator VisitIdentifierExpression(IdentifierExpression node)
    {
        var slot = Locals.Resolve(node.Name);
        Emit(OpCode.LOAD, slot);
        return this;
    }

    public BytecodeGenerator VisitCallExpression(CallExpression node)
    {
        foreach (var arg in node.Arguments)
            arg.Accept(this);

        if (node.Callee is IdentifierExpression id)
        {
            var functionId = ResolveFunction(id.Name);
            Emit(OpCode.CALL, functionId);
        }

        return this;
    }

    public BytecodeGenerator VisitTernaryExpression(TernaryExpression node)
    {
        node.Condition.Accept(this);
        var jumpFalse = EmitPlaceholder(OpCode.JUMP_IF_FALSE);

        node.Condition.Accept(this);
        var jumpEnd = EmitPlaceholder(OpCode.JUMP);

        Patch(jumpFalse);
        node.ElseBranch.Accept(this);
        Patch(jumpEnd);
        
        return this;
    }

    public BytecodeGenerator VisitArrayAccessExpression(ArrayAccessExpression node)
    {
        node.Target.Accept(this);
        node.Index.Accept(this);
        Emit(OpCode.GET_ELEMENT);
        return this;
    }

    public BytecodeGenerator VisitMemberAccessExpression(MemberAccessExpression node)
    {
        node.Object.Accept(this);

        var objectType = ResolveExpressionType(node.Object);
        var (_, fieldId, _) = ResolveMember(node);

        Emit(OpCode.GET_FIELD, fieldId);
        return this;
    }

    public BytecodeGenerator VisitNewArrayExpression(NewArrayExpression node)
    {
        node.SizeExpression.Accept(this);
        Emit(OpCode.NEW_ARRAY);
        return this;
    }

    public BytecodeGenerator VisitNewObjectExpression(NewObjectExpression node)
    {
        foreach (var arg in node.Arguments)
            arg.Accept(this);
        
        var cls = _program.Classes.FirstOrDefault(c => c.Name == node.ClassName);
        if (cls == null)
            throw new InvalidOperationException($"Unknown class '{node.ClassName}'");

        Emit(OpCode.NEW_OBJECT, cls.ClassId);
        return this;
    }

    // Разрешение типа
    private BytecodeType ResolveType(string typeName)
    {
        if (_resolvedTypes.TryGetValue(typeName, out var cached))
            return cached;

        BytecodeType result;

        // Массив
        if (typeName.EndsWith("[]"))
        {
            var elementName = typeName[..^2];
            var elementType = ResolveType(elementName);

            result = new ArrayType(elementType);
        }
        // Другие примитивы
        else
        {
            result = typeName switch
            {
                "int"    => GetOrCreatePrimitive("int"),
                "double" => GetOrCreatePrimitive("double"),
                "bool"   => GetOrCreatePrimitive("bool"),
                "char"   => GetOrCreatePrimitive("char"),
                "string" => GetOrCreatePrimitive("string"),
                "void"   => GetOrCreatePrimitive("void"),
                _        => ResolveClassType(typeName)
            };
        }

        // Регистрация типа в Program
        result.TypeId = _program.Types.Count;
        _program.Types.Add(result);
        _resolvedTypes[typeName] = result;

        return result;
    }
    
    private BytecodeType ResolveExpressionType(Expression expr)
    {
        return expr switch
        {
            LiteralExpression l        => ResolveType(expr.GetType().Name),
            IdentifierExpression id    => ResolveIdentifierType(id.Name),
            ArrayAccessExpression a    => ((ArrayType)ResolveExpressionType(a.Target)).ElementType,
            MemberAccessExpression m   => ResolveMember(m).Type,
            CallExpression c           => ResolveCallType(c),
            NewObjectExpression n      => ResolveType(n.ClassName),
            NewArrayExpression a       => new ArrayType(ResolveType(a.ElementType)),
            _ => throw new NotSupportedException($"Type resolution not supported for {expr.NodeType}")
        };
    }
    
    private PrimitiveType GetOrCreatePrimitive(string name)
    {
        if (_primitiveTypes.TryGetValue(name, out var t))
            return t;

        var type = new PrimitiveType(name);
        _primitiveTypes[name] = type;
        return type;
    }

    private BytecodeType ResolveClassType(string name)
    {
        var cls = _program.Classes.FirstOrDefault(c => c.Name == name);
        return cls == null ?
            throw new InvalidOperationException($"Unknown class type '{name}'")
            : new ClassType(cls.ClassId, cls.Name);
    }
    
    private BytecodeClass ResolveClass(string name)
    {
        var cls = _program.Classes.FirstOrDefault(c => c.Name == name);
        return cls ?? throw new InvalidOperationException($"Unknown class type '{name}'");
    }

    private int ResolveFunction(string name)
    {
        var func = _program.Functions.FirstOrDefault(f => f.Name == name);
        if (func == null)
            throw new InvalidOperationException($"Function '{name}' not found");
        return func.FunctionId;
    }
    
    private BytecodeType ResolveIdentifierType(string name)
    {
        if (!_resolvedTypes.TryGetValue(name, out var type))
            throw new InvalidOperationException($"Unknown identifier '{name}'");

        return type;
    }

    private BytecodeType ResolveCallType(CallExpression node)
    {
        switch (node.Callee)
        {
            case IdentifierExpression id:
            {
                var funcId = ResolveFunction(id.Name);
                return _program.Functions[funcId].ReturnType;
            }

            case MemberAccessExpression member:
            {
                var (id, type, isField) = ResolveMember(member);

                if (isField)
                    throw new InvalidOperationException(
                        $"'{member.MemberName}' is a field, not a method");

                return type;
            }

            default:
                throw new InvalidOperationException("Invalid call target");
        }
    }

    private (int FieldId, BytecodeType Type) ResolveField(MemberAccessExpression node)
    {
        if (node.Object is not IdentifierExpression idExpr)
        {
            throw new InvalidOperationException("Invalid call target");
        }

        var bytecodeClass = ResolveClass(idExpr.Name);
        
        return bytecodeClass.Fields.TryGetValue(node.MemberName, out var field) ? (field.FieldId, field.Type)
            : throw new InvalidOperationException($"Member '{node.MemberName}' not found");
    }
    
    private (int Id, BytecodeType Type, bool IsField) ResolveMember(MemberAccessExpression node)
    {
        var objectType = ResolveExpressionType(node.Object);

        if (objectType is not ClassType clsType)
            throw new InvalidOperationException("Member access on non-class type");

        var cls = _program.Classes.First(c => c.ClassId == clsType.ClassId);

        if (cls.Fields.TryGetValue(node.MemberName, out var field))
            return (field.FieldId, field.Type, true);

        if (cls.Methods.TryGetValue(node.MemberName, out var methodId))
            return (methodId, ResolveMethodReturnType(methodId), false);

        throw new InvalidOperationException($"Member '{node.MemberName}' not found");
    }

    private BytecodeType ResolveMethodReturnType(int methodId)
    {
        if (methodId < 0 || methodId >= _program.Functions.Count)
            throw new InvalidOperationException($"Invalid method id {methodId}");

        return _program.Functions[methodId].ReturnType;
    }

    private int AddConstant(object value)
    {
        _program.ConstantPool.Add(value);
        return _program.ConstantPool.Count - 1;
    }
}