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
    private int _tempCounter;

    private LocalSlotManager Locals => _locals.Peek();
    private void EnterScope() => Locals.EnterScope();
    private void ExitScope() => Locals.ExitScope();

    public BytecodeProgram Generate(ProgramNode program)
    {
        VisitProgram(program);
        return _program;
    }

    // Добавляет операцию в текущую обрабатываемую функцию
    private void Emit(OpCode opCode, params object[] operands)
    {
        if (_currentFunction == null)
        {
            throw new InvalidOperationException("No function declared in scope");
        }

        _currentFunction.Code.Add(new Instruction(opCode, operands));
    }

    private int AllocateTempLocal()
    {
        if (_currentFunction == null)
        {
            throw new InvalidOperationException("Temp local outside function");
        }

        var slot = _currentFunction.Locals.Count;
        var name = $"__tmp{_tempCounter++}";
        var type = ResolveType("int");
        _currentFunction.Locals.Add(new BytecodeVariable(slot, name, type));
        return slot;
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

    // Объявление функции, её параметров, переменных, инструкций
    public BytecodeGenerator VisitFunctionDeclaration(FunctionDeclaration node)
    {
        var function = new BytecodeFunction(
            functionId: _program.Functions.Count,
            name: node.Name,
            returnType: ResolveType(node.ReturnType),
            parameterTypes: node.Parameters
                .Select(p => new BytecodeFunctionParameter(p.Name, ResolveType(p.TypeName)))
                .ToList()
        );

        _program.Functions.Add(function);
        _currentClass?.Methods.Add(function.Name, function.FunctionId);
        _currentFunction = function;

        _locals.Push(new LocalSlotManager(function));
        EnterScope();

        if (_currentClass != null)
        {
            Locals.Declare("this", new ClassType(_currentClass.ClassId, _currentClass.Name));
        }

        foreach (var param in node.Parameters)
        {
            param.Accept(this);
        }

        node.Body.Accept(this);

        ExitScope();
        _locals.Pop();

        _currentFunction = null;

        return this;
    }

    // Объявление параметров функции
    public BytecodeGenerator VisitParameterDeclaration(ParameterDeclaration node)
    {
        if (_currentFunction == null)
        {
            throw new InvalidOperationException("Parameter outside function");
        }

        Locals.Declare(node.Name, ResolveType(node.TypeName));
        return this;
    }

    /* Объявление переменных
     * Есть 4 типа переменных:
     *
     * Где объявлена	    Где хранится	    Как адресуется
     * Глобальный scope     Program.Globals	    LOAD_GLOBAL id
     * Параметр функции	    Frame.Locals	    LOAD_LOCAL slot
     * Локал функции        Frame.Locals	    LOAD_LOCAL slot
     * Поле класса          Object.Fields	    GET_FIELD fieldId
     */
    public BytecodeGenerator VisitVariableDeclaration(VariableDeclaration node)
    {
        var type = ResolveType(node.TypeName);

        // Глобальный scope
        if (_currentFunction == null && _currentClass == null)
        {
            var id = _program.Globals.Count;
            _program.Globals.Add(new BytecodeVariable(id, node.Name, type));
            if (node.Initializer != null)
            {
                node.Initializer.Accept(this);
                Emit(OpCode.STORE_GLOBAL, id);
            }

            return this;
        }

        // Локал функции
        if (_currentFunction != null)
        {
            var slot = Locals.Declare(node.Name, type);
            if (node.Initializer != null)
            {
                node.Initializer.Accept(this);
                Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, slot);
            }

            return this;
        }

        // Поле класса
        if (_currentClass != null && _currentFunction == null)
        {
            _currentClass.Fields.Add(
                node.Name,
                new BytecodeClassField(
                    fieldId: _currentClass.Fields.Count,
                    type: ResolveType(node.TypeName)
                ));
            return this;
        }

        throw new InvalidOperationException("Invalid variable declaration context");
    }

    // Объявление класса
    public BytecodeGenerator VisitClassDeclaration(ClassDeclaration node)
    {
        var cls = new BytecodeClass(
            classId: _program.Classes.Count,
            name: node.Name
        );

        _program.Classes.Add(cls);
        _currentClass = cls;

        foreach (var member in node.Members)
        {
            member.Accept(this);
        }

        _currentClass = null;
        return this;
    }

    // Обход блока, создание нового скоупа
    public BytecodeGenerator VisitBlockStatement(BlockStatement node)
    {
        EnterScope();

        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }

        ExitScope();
        return this;
    }

    // Обработка выражения, после каждого выражения идёт POP, a = b, также считается выражением и возвращает b
    public BytecodeGenerator VisitExpressionStatement(ExpressionStatement node)
    {
        node.Expression.Accept(this);
        Emit(OpCode.POP);
        return this;
    }

    // Возврат функции
    public BytecodeGenerator VisitReturnStatement(ReturnStatement node)
    {
        node.Value?.Accept(this);
        Emit(OpCode.RETURN);
        return this;
    }

    // Обход if оператора
    public BytecodeGenerator VisitIfStatement(IfStatement node)
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

    // Промежуточное создание байткода, пока что не известно, каким будет параметр
    private int EmitPlaceholder(OpCode opCode)
    {
        if (_currentFunction == null)
        {
            throw new NullReferenceException("No function declared in scope");
        }

        var placeholderIndex = _currentFunction.Code.Count;
        Emit(opCode, 0); // 0 будет заменено позже
        return placeholderIndex;
    }

    // Когда известен параметр для байткода, созданного EmitPlaceholder, этот параметр дополняется
    private void Patch(int instructionIndex)
    {
        if (_currentFunction == null)
        {
            throw new NullReferenceException("No function declared in scope");
        }

        var instr = _currentFunction.Code[instructionIndex];
        _currentFunction.Code[instructionIndex] = new Instruction(instr.OpCode, _currentFunction.Code.Count);
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
    public BytecodeGenerator VisitWhileStatement(WhileStatement node)
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

        if (_currentFunction == null)
        {
            throw new NullReferenceException("No function declared in scope");
        }

        // начало цикла
        var loopStart = _currentFunction.Code.Count;

        // condition (если есть)
        var jumpExit = -1;
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
        {
            Patch(jumpExit);
        }

        ExitScope();
        return this;
    }

    // Бинарное выражение, пример a + b или a = b
    public BytecodeGenerator VisitBinaryExpression(BinaryExpression node)
    {
        // Присваивание
        if (node.Operator.Type == TokenType.ASSIGN)
        {
            EmitAssignment(node.Left, node.Right);
            return this;
        }

        if (IsCompoundAssignment(node.Operator.Type))
        {
            EmitCompoundAssignment(node.Left, node.Right, node.Operator.Type);
            return this;
        }

        node.Left.Accept(this);
        node.Right.Accept(this);

        // Для остальных операций
        switch (node.Operator.Type)
        {
            case TokenType.PLUS:
            case TokenType.MINUS:
            case TokenType.STAR:
            case TokenType.SLASH:
            case TokenType.MODULO:
                EmitBinaryArithmetic(node.Operator.Type);
                break;
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

    // Создаёт байткод для операции присваивания a = b
    private void EmitAssignment(Expression target, Expression value)
    {
        switch (target)
        {
            // x = value
            case IdentifierExpression id:
            {
                // Сначала локальные переменные
                if (Locals.TryResolve(id.Name, out var slot))
                {
                    value.Accept(this);
                    Emit(OpCode.DUP);
                    Emit(OpCode.STORE_LOCAL, _currentFunction!.FunctionId, slot);
                    return;
                }

                // Затем поля текущего класса (неявный this)
                if (_currentClass != null && _currentClass.Fields.TryGetValue(id.Name, out var field))
                {
                    if (!Locals.TryResolve("this", out var thisSlot))
                        throw new Exception("'this' not found in method scope");

                    Emit(OpCode.LOAD_LOCAL, _currentFunction!.FunctionId, thisSlot);
                    value.Accept(this);
                    Emit(OpCode.DUP);
                    Emit(OpCode.SET_FIELD, _currentClass.ClassId, field.FieldId);
                    return;
                }

                throw new Exception($"Local '{id.Name}' not found");
            }

            // obj.field = value
            case MemberAccessExpression ma:
            {
                ma.Object.Accept(this); // stack: object
                value.Accept(this); // stack: object, value
                Emit(OpCode.DUP); // stack: object, value, value

                var tempSlot = AllocateTempLocal();
                Emit(OpCode.STORE_LOCAL, _currentFunction!.FunctionId, tempSlot); // stack: object, value

                var (classId, fieldId) = ResolveField(ma);
                Emit(OpCode.SET_FIELD, classId, fieldId);
                Emit(OpCode.LOAD_LOCAL, _currentFunction!.FunctionId, tempSlot); // stack: value
                return;
            }

            // arr[index] = value
            case ArrayAccessExpression aa:
            {
                aa.Target.Accept(this); // stack: array
                aa.Index.Accept(this); // stack: array, index
                value.Accept(this); // stack: array, index, value
                Emit(OpCode.DUP); // stack: array, index, value, value

                var tempSlot = AllocateTempLocal();
                Emit(OpCode.STORE_LOCAL, _currentFunction!.FunctionId, tempSlot); // stack: array, index, value

                Emit(OpCode.SET_ELEMENT);
                Emit(OpCode.LOAD_LOCAL, _currentFunction!.FunctionId, tempSlot); // stack: value
                return;
            }

            default:
                throw new InvalidOperationException($"Expression '{target.NodeType}' cannot be assigned to");
        }
    }

    private void EmitCompoundAssignment(Expression target, Expression value, TokenType op)
    {
        switch (target)
        {
            case IdentifierExpression id:
            {
                if (!Locals.TryResolve(id.Name, out var slot))
                {
                    throw new Exception($"Local '{id.Name}' not found");
                }

                if (_currentFunction == null)
                {
                    throw new NullReferenceException("No function declared in scope");
                }

                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, slot);
                value.Accept(this);
                EmitBinaryArithmetic(op);
                Emit(OpCode.DUP);
                Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, slot);
                return;
            }

            case MemberAccessExpression ma:
            {
                ma.Object.Accept(this); // stack: object
                Emit(OpCode.DUP); // stack: object, object
                var (classId, fieldId) = ResolveField(ma);
                Emit(OpCode.GET_FIELD, classId, fieldId); // stack: object, field
                value.Accept(this); // stack: object, field, value
                EmitBinaryArithmetic(op); // stack: object, result
                Emit(OpCode.DUP); // stack: object, result, result
                Emit(OpCode.SET_FIELD, classId, fieldId); // stack: result
                return;
            }

            case ArrayAccessExpression aa:
            {
                aa.Target.Accept(this); // stack: array
                aa.Index.Accept(this); // stack: array, index

                var indexSlot = AllocateTempLocal();
                Emit(OpCode.STORE_LOCAL, _currentFunction!.FunctionId, indexSlot); // stack: array
                var arraySlot = AllocateTempLocal();
                Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, arraySlot); // stack: empty

                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, arraySlot); // stack: array
                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, indexSlot); // stack: array, index
                Emit(OpCode.GET_ELEMENT); // stack: element
                value.Accept(this); // stack: element, value
                EmitBinaryArithmetic(op); // stack: result

                var resultSlot = AllocateTempLocal();
                Emit(OpCode.DUP); // stack: result, result
                Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, resultSlot); // stack: result

                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, arraySlot); // stack: result, array
                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, indexSlot); // stack: result, array, index
                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, resultSlot); // stack: result, array, index, result
                Emit(OpCode.SET_ELEMENT); // stack: result
                return;
            }

            default:
                throw new InvalidOperationException($"Expression '{target.NodeType}' cannot be assigned to");
        }
    }

    private static bool IsCompoundAssignment(TokenType op) => op is
        TokenType.PLUS_ASSIGN or
        TokenType.MINUS_ASSIGN or
        TokenType.STAR_ASSIGN or
        TokenType.SLASH_ASSIGN or
        TokenType.MODULO_ASSIGN;

    private void EmitBinaryArithmetic(TokenType op)
    {
        switch (op)
        {
            case TokenType.PLUS:
            case TokenType.PLUS_ASSIGN:
                Emit(OpCode.ADD);
                return;
            case TokenType.MINUS:
            case TokenType.MINUS_ASSIGN:
                Emit(OpCode.SUB);
                return;
            case TokenType.STAR:
            case TokenType.STAR_ASSIGN:
                Emit(OpCode.MUL);
                return;
            case TokenType.SLASH:
            case TokenType.SLASH_ASSIGN:
                Emit(OpCode.DIV);
                return;
            case TokenType.MODULO:
            case TokenType.MODULO_ASSIGN:
                Emit(OpCode.MOD);
                return;
            default:
                throw new NotSupportedException($"Operator {op} not supported");
        }
    }

    // Унарное выражение, пример -a, !a
    public BytecodeGenerator VisitUnaryExpression(UnaryExpression node)
    {
        switch (node.Operator.Type)
        {
            case TokenType.NOT:
                node.Operand.Accept(this);
                Emit(OpCode.NOT);
                break;
            case TokenType.MINUS:
                node.Operand.Accept(this);
                Emit(OpCode.NEG);
                break;
            case TokenType.INCREMENT:
            case TokenType.DECREMENT:
                EmitIncrementDecrement(node.Operand, node.Operator.Type == TokenType.INCREMENT, node.IsPostfix);
                break;
            default:
                throw new NotSupportedException($"Operator {node.Operator.Type} not supported");
        }

        return this;
    }

    private void EmitIncrementDecrement(Expression target, bool isIncrement, bool isPostfix)
    {
        if (_currentFunction == null)
        {
            throw new NullReferenceException("No function declared in scope");
        }

        var deltaConstId = _program.ConstantPool.Count;
        _program.ConstantPool.Add(1);

        switch (target)
        {
            case IdentifierExpression id:
            {
                if (!Locals.TryResolve(id.Name, out var slot))
                {
                    throw new Exception($"Local '{id.Name}' not found");
                }

                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, slot);

                if (isPostfix)
                {
                    Emit(OpCode.DUP);
                    Emit(OpCode.PUSH, deltaConstId);
                    EmitBinaryArithmetic(isIncrement ? TokenType.PLUS : TokenType.MINUS);

                    var tempSlot = AllocateTempLocal();
                    Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, slot);
                }
                else
                {
                    Emit(OpCode.PUSH, deltaConstId);
                    EmitBinaryArithmetic(isIncrement ? TokenType.PLUS : TokenType.MINUS);
                    Emit(OpCode.DUP);
                    Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, slot);
                }

                return;
            }

            case MemberAccessExpression ma:
            {
                ma.Object.Accept(this);
                var objSlot = AllocateTempLocal();
                Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, objSlot);

                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, objSlot);
                var (classId, fieldId) = ResolveField(ma);
                Emit(OpCode.GET_FIELD, classId, fieldId);

                if (isPostfix)
                {
                    Emit(OpCode.DUP);
                    Emit(OpCode.PUSH, deltaConstId);
                    EmitBinaryArithmetic(isIncrement ? TokenType.PLUS : TokenType.MINUS);

                    var tempSlot = AllocateTempLocal();
                    Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, objSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.SET_FIELD, classId, fieldId);
                }
                else
                {
                    Emit(OpCode.PUSH, deltaConstId);
                    EmitBinaryArithmetic(isIncrement ? TokenType.PLUS : TokenType.MINUS);

                    var tempSlot = AllocateTempLocal();
                    Emit(OpCode.DUP);
                    Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, objSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.SET_FIELD, classId, fieldId);
                }

                return;
            }

            case ArrayAccessExpression aa:
            {
                aa.Target.Accept(this);
                aa.Index.Accept(this);

                var indexSlot = AllocateTempLocal();
                Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, indexSlot);
                var arraySlot = AllocateTempLocal();
                Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, arraySlot);

                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, arraySlot);
                Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, indexSlot);
                Emit(OpCode.GET_ELEMENT);

                if (isPostfix)
                {
                    Emit(OpCode.DUP);
                    Emit(OpCode.PUSH, deltaConstId);
                    EmitBinaryArithmetic(isIncrement ? TokenType.PLUS : TokenType.MINUS);

                    var tempSlot = AllocateTempLocal();
                    Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, arraySlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, indexSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.SET_ELEMENT);
                }
                else
                {
                    Emit(OpCode.PUSH, deltaConstId);
                    EmitBinaryArithmetic(isIncrement ? TokenType.PLUS : TokenType.MINUS);

                    var tempSlot = AllocateTempLocal();
                    Emit(OpCode.DUP);
                    Emit(OpCode.STORE_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, arraySlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, indexSlot);
                    Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, tempSlot);
                    Emit(OpCode.SET_ELEMENT);
                }

                return;
            }

            default:
                throw new InvalidOperationException($"Expression '{target.NodeType}' cannot be incremented");
        }
    }

    // Добавляет константу в пул и на стек
    public BytecodeGenerator VisitLiteralExpression(LiteralExpression node)
    {
        var constId = _program.ConstantPool.Count;
        _program.ConstantPool.Add(node.Value);

        Emit(OpCode.PUSH, constId);

        return this;
    }

    // Загружает нужную переменную в стек
    public BytecodeGenerator VisitIdentifierExpression(IdentifierExpression node)
    {
        // Локал / Параметр функции
        if (_currentFunction != null && Locals.TryResolve(node.Name, out var slot))
        {
            Emit(OpCode.LOAD_LOCAL, _currentFunction.FunctionId, slot);
            return this;
        }

        // Поле текущего класса (неявный this)
        if (_currentClass != null && _currentClass.Fields.TryGetValue(node.Name, out var field))
        {
            if (!Locals.TryResolve("this", out var thisSlot))
                throw new Exception("'this' not found in method scope");

            Emit(OpCode.LOAD_LOCAL, _currentFunction!.FunctionId, thisSlot);
            Emit(OpCode.GET_FIELD, _currentClass.ClassId, field.FieldId);
            return this;
        }

        // Глобал
        var global = _program.Globals.FirstOrDefault(g => g.Name == node.Name);
        if (global != null)
        {
            Emit(OpCode.LOAD_GLOBAL, global.VariableId);
            return this;
        }

        throw new InvalidOperationException($"Unknown identifier '{node.Name}'");
    }

    // Вызов функции
    public BytecodeGenerator VisitCallExpression(CallExpression node)
    {
        // Сценарий 1: Вызов по имени (функция или Native API)
        // Пример: print("Hello"), sum(1, 2)
        if (node.Callee is IdentifierExpression id)
        {
            // --- NATIVE API (Встроенные функции) ---

            // 1. print(arg) -> void
            if (id.Name == "print")
            {
                // Генерируем код для аргументов
                if (node.Arguments.Count == 0)
                {
                    var constId = _program.ConstantPool.Count;
                    _program.ConstantPool.Add(string.Empty);
                    Emit(OpCode.PUSH, constId);
                }
                else
                {
                    foreach (var arg in node.Arguments)
                    {
                        arg.Accept(this);
                    }
                }

                // Вызываем Native ID 0 (см. RuntimeContext)
                Emit(OpCode.CALL_NATIVE, 0);
                return this;
            }

            // 2. println(arg) -> void
            if (id.Name == "println")
            {
                if (node.Arguments.Count == 0)
                {
                    var constId = _program.ConstantPool.Count;
                    _program.ConstantPool.Add(string.Empty);
                    Emit(OpCode.PUSH, constId);
                }
                else
                {
                    foreach (var arg in node.Arguments)
                    {
                        arg.Accept(this);
                    }
                }

                // Вызываем Native ID 3 (см. RuntimeContext)
                Emit(OpCode.CALL_NATIVE, 3);
                return this;
            }

            // 3. time() -> int (ms)
            if (id.Name == "time")
            {
                // Аргументов нет, но на всякий случай обработаем, если они были переданы ошибочно
                // (хотя семантический анализ должен был это отловить)
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                // Вызываем Native ID 1
                Emit(OpCode.CALL_NATIVE, 1);
                return this;
            }

            // 4. random(max) -> int
            if (id.Name == "random")
            {
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                // Вызываем Native ID 2
                Emit(OpCode.CALL_NATIVE, 2);
                return this;
            }

            // --- ПОЛЬЗОВАТЕЛЬСКИЕ ФУНКЦИИ ---

            // 1. Генерируем аргументы (слева направо, чтобы они легли на стек в правильном порядке)
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }

            // 2. Находим ID функции в таблице символов
            var functionId = ResolveFunction(id.Name);

            // 3. Генерируем инструкцию CALL
            Emit(OpCode.CALL, functionId);
            return this;
        }

        // Сценарий 2: Вызов метода объекта
        // Пример: obj.method(1, 2)
        if (node.Callee is MemberAccessExpression ma)
        {
            // 1. Загружаем объект (this) на стек. 
            // Это критически важно: метод должен знать, над каким объектом он выполняется.
            ma.Object.Accept(this);

            // 2. Генерируем аргументы
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }

            // 3. Резолвим класс объекта и ID метода
            // Метод ResolveClass анализирует тип переменной слева от точки
            var cls = ResolveClass(ma);

            if (!cls.Methods.TryGetValue(ma.MemberName, out var methodId))
            {
                throw new InvalidOperationException($"Method '{ma.MemberName}' not found in class '{cls.Name}'");
            }

            // 4. Генерируем инструкцию CALL_METHOD
            Emit(OpCode.CALL_METHOD, cls.ClassId, methodId);
            return this;
        }

        // Если мы тут, значит AST содержит что-то странное (например, вызов (1+2)())
        throw new InvalidOperationException($"Invalid call target: {node.Callee.GetType().Name}");
    }

    // Обход тернарного оператора
    public BytecodeGenerator VisitTernaryExpression(TernaryExpression node)
    {
        node.Condition.Accept(this);
        var jumpFalse = EmitPlaceholder(OpCode.JUMP_IF_FALSE);

        node.ThenBranch.Accept(this);
        var jumpEnd = EmitPlaceholder(OpCode.JUMP);

        Patch(jumpFalse);
        node.ElseBranch.Accept(this);
        Patch(jumpEnd);

        return this;
    }

    // Доступ к элементу массива arr[i]
    public BytecodeGenerator VisitArrayAccessExpression(ArrayAccessExpression node)
    {
        node.Target.Accept(this);
        node.Index.Accept(this);
        Emit(OpCode.GET_ELEMENT);
        return this;
    }

    // Доступ к полю класса a.field
    public BytecodeGenerator VisitMemberAccessExpression(MemberAccessExpression node)
    {
        // Загружаем объект
        node.Object.Accept(this);

        var cls = ResolveClass(node);

        // Получаем поле
        if (!cls.Fields.TryGetValue(node.MemberName, out var field))
        {
            throw new InvalidOperationException($"Field '{node.MemberName}' not found in class '{cls.Name}'");
        }

        // Генерируем байткод
        Emit(OpCode.GET_FIELD, cls.ClassId, field.FieldId);

        return this;
    }

    // Создание нового массива на куче
    public BytecodeGenerator VisitNewArrayExpression(NewArrayExpression node)
    {
        node.SizeExpression.Accept(this);
        Emit(OpCode.NEW_ARRAY);
        return this;
    }

    // Создание нового объекта на куче (в нашем случае класса)
    public BytecodeGenerator VisitNewObjectExpression(NewObjectExpression node)
    {
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this);
        }

        var cls = _program.Classes.FirstOrDefault(c => c.Name == node.ClassName);
        if (cls == null)
        {
            throw new InvalidOperationException($"Unknown class '{node.ClassName}'");
        }

        Emit(OpCode.NEW_OBJECT, cls.ClassId);
        return this;
    }

    // Разрешение типа
    private BytecodeType ResolveType(string typeName)
    {
        if (_resolvedTypes.TryGetValue(typeName, out var cached))
        {
            return cached;
        }

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
                "int" => GetOrCreatePrimitive("int"),
                "long" => GetOrCreatePrimitive("long"),
                "double" => GetOrCreatePrimitive("double"),
                "bool" => GetOrCreatePrimitive("bool"),
                "char" => GetOrCreatePrimitive("char"),
                "string" => GetOrCreatePrimitive("string"),
                "void" => GetOrCreatePrimitive("void"),
                _ => ResolveClassType(typeName)
            };
        }

        // Регистрация типа в Program
        result.TypeId = _program.Types.Count;
        _program.Types.Add(result);
        _resolvedTypes[typeName] = result;

        return result;
    }

    // Создаёт примитив или выдаёт из кэша
    private PrimitiveType GetOrCreatePrimitive(string name)
    {
        if (_primitiveTypes.TryGetValue(name, out var t))
        {
            return t;
        }

        var type = new PrimitiveType(name);
        _primitiveTypes[name] = type;
        return type;
    }

    // Находит класс по имени
    private ClassType ResolveClassType(string name)
    {
        var cls = _program.Classes.FirstOrDefault(c => c.Name == name);
        return cls == null
            ? throw new InvalidOperationException($"Unknown class type '{name}'")
            : new ClassType(cls.ClassId, cls.Name);
    }

    // Возвращает Id функции
    private int ResolveFunction(string name)
    {
        var func = _program.Functions.FirstOrDefault(f => f.Name == name);
        return func?.FunctionId ?? throw new InvalidOperationException($"Function '{name}' not found");
    }

    // Получает информацию о поле класса
    private (int ClassId, int FieldId) ResolveField(MemberAccessExpression node)
    {
        var bytecodeClass = ResolveClass(node);

        // Получаем поле
        if (!bytecodeClass.Fields.TryGetValue(node.MemberName, out var field))
        {
            throw new InvalidOperationException($"Field '{node.MemberName}' not found in class '{bytecodeClass.Name}'");
        }

        return (bytecodeClass.ClassId, field.FieldId);
    }

    // Возвращает класс, поле которого, мы хотим получить
    private BytecodeClass ResolveClass(MemberAccessExpression node)
    {
        if (node.Object is not IdentifierExpression id)
        {
            throw new InvalidOperationException("MemberAccessExpression.Object must be IdentifierExpression");
        }

        // 1. Находим переменную
        BytecodeVariable? variable;
        if (_currentFunction != null && Locals.TryResolve(id.Name, out var slot))
        {
            variable = _currentFunction.Locals[slot];
        }
        else
        {
            variable = _program.Globals.FirstOrDefault(v => v.Name == id.Name);
        }

        if (variable == null) throw new InvalidOperationException($"Unknown variable '{id.Name}'");

        // 2. Проверяем, что это класс
        if (variable.Type is not ClassType classType)
        {
            throw new InvalidOperationException($"Member access on non-class variable '{id.Name}'");
        }

        // 3. Получаем класс
        var cls = _program.Classes.First(c => c.ClassId == classType.ClassId);

        return cls;
    }
}
