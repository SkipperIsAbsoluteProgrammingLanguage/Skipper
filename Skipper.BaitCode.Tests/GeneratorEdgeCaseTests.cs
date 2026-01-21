using System.Reflection;
using Skipper.BaitCode.Generator;
using Skipper.BaitCode.IdManager;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Skipper.Lexer.Tokens;
using Skipper.Parser.AST;
using Skipper.Parser.AST.Declarations;
using Skipper.Parser.AST.Expressions;
using Skipper.Parser.AST.Statements;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class GeneratorEdgeCaseTests
{
    private sealed class InjectFunctionExpression(BytecodeFunction func) : Expression(new Token(TokenType.NUMBER, "0"))
    {
        public override AstNodeType NodeType => AstNodeType.LiteralExpression;

        public override T Accept<T>(Skipper.Parser.Visitor.IAstVisitor<T> visitor)
        {
            if (visitor is BytecodeGenerator gen)
            {
                var funcField = typeof(BytecodeGenerator).GetField("_currentFunction", BindingFlags.NonPublic | BindingFlags.Instance);
                funcField!.SetValue(gen, func);
            }

            return (T)(object)visitor;
        }
    }

    private static BytecodeGenerator CreateGeneratorWithFunction(out BytecodeFunction func, out LocalSlotManager locals)
    {
        var generator = new BytecodeGenerator();
        func = new BytecodeFunction(0, "main", new PrimitiveType("void"), []);

        locals = new LocalSlotManager(func);
        locals.EnterScope();

        var funcField = typeof(BytecodeGenerator).GetField("_currentFunction", BindingFlags.NonPublic | BindingFlags.Instance);
        var localsField = typeof(BytecodeGenerator).GetField("_locals", BindingFlags.NonPublic | BindingFlags.Instance);
        funcField!.SetValue(generator, func);
        var stack = (Stack<LocalSlotManager>)localsField!.GetValue(generator)!;
        stack.Push(locals);

        return generator;
    }

    private static BytecodeProgram GetProgram(BytecodeGenerator generator)
    {
        var programField = typeof(BytecodeGenerator).GetField("_program", BindingFlags.NonPublic | BindingFlags.Instance);
        return (BytecodeProgram)programField!.GetValue(generator)!;
    }

    private static TException AssertInnerException<TException>(Action action) where TException : Exception
    {
        var ex = Assert.Throws<TargetInvocationException>(action);
        var inner = Assert.IsType<TException>(ex.InnerException);
        return inner;
    }

    private static LiteralExpression Lit(int value)
    {
        return new LiteralExpression(value, new Token(TokenType.NUMBER, value.ToString()));
    }

    [Fact]
    public void Emit_WithoutFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitLiteralExpression(Lit(1)));
    }

    [Fact]
    public void AllocateTempLocal_WithoutFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var method = typeof(BytecodeGenerator).GetMethod("AllocateTempLocal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        _ = AssertInnerException<InvalidOperationException>(() => method!.Invoke(generator, null));
    }

    [Fact]
    public void ParameterOutsideFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var param = new ParameterDeclaration("int", "x");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitParameterDeclaration(param));
    }

    [Fact]
    public void GlobalVariableDeclaration_RegistersGlobal()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var decl = new VariableDeclaration("int", "g", null);

        // Act
        generator.VisitVariableDeclaration(decl);

        // Assert
        var program = GetProgram(generator);
        Assert.Single(program.Globals);
    }

    [Fact]
    public void GlobalVariableDeclaration_WithInitializer_EmitsStoreGlobal()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var dummy = new BytecodeFunction(0, "init", new PrimitiveType("void"), []);
        var decl = new VariableDeclaration("int", "g", new InjectFunctionExpression(dummy));

        // Act
        generator.VisitVariableDeclaration(decl);

        // Assert
        Assert.Contains(dummy.Code, i => i.OpCode == OpCode.STORE_GLOBAL);
    }

    [Fact]
    public void VariableDeclaration_InvalidContext_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var funcField = typeof(BytecodeGenerator).GetField("_currentFunction", BindingFlags.NonPublic | BindingFlags.Instance);
        var classField = typeof(BytecodeGenerator).GetField("_currentClass", BindingFlags.NonPublic | BindingFlags.Instance);
        funcField!.SetValue(generator, new BytecodeFunction(0, "f", new PrimitiveType("void"), []));
        classField!.SetValue(generator, new BytecodeClass(0, "C"));

        var decl = new VariableDeclaration("int", "x", null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitVariableDeclaration(decl));
    }

    [Fact]
    public void EmitPlaceholder_WithoutFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var method = typeof(BytecodeGenerator).GetMethod("EmitPlaceholder", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        _ = AssertInnerException<NullReferenceException>(() => method!.Invoke(generator, [OpCode.JUMP]));
    }

    [Fact]
    public void Patch_WithoutFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var method = typeof(BytecodeGenerator).GetMethod("Patch", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        _ = AssertInnerException<NullReferenceException>(() => method!.Invoke(generator, [0]));
    }

    [Fact]
    public void WhileStatement_WithoutFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var node = new WhileStatement(new LiteralExpression(true, new Token(TokenType.BOOL_LITERAL, "true")),
            new BlockStatement([]));

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => generator.VisitWhileStatement(node));
    }

    [Fact]
    public void ForStatement_WithoutFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var localsField = typeof(BytecodeGenerator).GetField("_locals", BindingFlags.NonPublic | BindingFlags.Instance);
        var stack = (Stack<LocalSlotManager>)localsField!.GetValue(generator)!;
        var dummyFunc = new BytecodeFunction(0, "dummy", new PrimitiveType("void"), []);
        var locals = new LocalSlotManager(dummyFunc);
        locals.EnterScope();
        stack.Push(locals);
        var node = new ForStatement(null, null, null, new BlockStatement([]));

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => generator.VisitForStatement(node));
    }

    [Fact]
    public void BinaryExpression_OrOperator_GeneratesOr()
    {
        // Arrange
        const string code = "fn main() -> bool { return true || false; }";

        // Act
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        // Assert
        Assert.Contains(inst, i => i.OpCode == OpCode.OR);
    }

    [Fact]
    public void BinaryExpression_UnsupportedOperator_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new BinaryExpression(Lit(1), new Token(TokenType.COMMA, ","), Lit(2));

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => generator.VisitBinaryExpression(expr));
    }

    [Fact]
    public void Assignment_LocalNotFound_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new BinaryExpression(new IdentifierExpression(new Token(TokenType.IDENTIFIER, "x")),
            new Token(TokenType.ASSIGN, "="),
            Lit(1));

        // Act & Assert
        Assert.Throws<Exception>(() => generator.VisitBinaryExpression(expr));
    }

    [Fact]
    public void Assignment_WithoutFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var expr = new BinaryExpression(
        new IdentifierExpression(new Token(TokenType.IDENTIFIER, "x")),
        new Token(TokenType.ASSIGN, "="),
        Lit(1));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitBinaryExpression(expr));
    }

    [Fact]
    public void Assignment_InvalidTarget_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new BinaryExpression(Lit(1), new Token(TokenType.ASSIGN, "="), Lit(2));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitBinaryExpression(expr));
    }

    [Fact]
    public void CompoundAssignment_LocalNotFound_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new BinaryExpression(new IdentifierExpression(new Token(TokenType.IDENTIFIER, "x")),
            new Token(TokenType.PLUS_ASSIGN, "+="),
            Lit(1));

        // Act & Assert
        Assert.Throws<Exception>(() => generator.VisitBinaryExpression(expr));
    }

    [Fact]
    public void CompoundAssignment_NoFunction_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out var locals);
        _ = locals.Declare("x", new PrimitiveType("int"));

        var funcField = typeof(BytecodeGenerator).GetField("_currentFunction", BindingFlags.NonPublic | BindingFlags.Instance);
        funcField!.SetValue(generator, null);

        var expr = new BinaryExpression(new IdentifierExpression(new Token(TokenType.IDENTIFIER, "x")),
            new Token(TokenType.PLUS_ASSIGN, "+="),
            Lit(1));

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => generator.VisitBinaryExpression(expr));
    }

    [Fact]
    public void CompoundAssignment_MemberAccess_GeneratesGetSet()
    {
        // Arrange
        const string code = """
                            class Box { int val; }
                            fn main() {
                                Box b = new Box();
                                b.val += 2;
                            }
                            """;

        // Act
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        // Assert
        Assert.Contains(inst, i => i.OpCode == OpCode.GET_FIELD);
        Assert.Contains(inst, i => i.OpCode == OpCode.SET_FIELD);
    }

    [Fact]
    public void CompoundAssignment_ArrayAccess_GeneratesGetSet()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int[] a = new int[2];
                                a[1] += 3;
                            }
                            """;

        // Act
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        // Assert
        Assert.Contains(inst, i => i.OpCode == OpCode.GET_ELEMENT);
        Assert.Contains(inst, i => i.OpCode == OpCode.SET_ELEMENT);
    }

    [Fact]
    public void CompoundAssignment_InvalidTarget_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new BinaryExpression(Lit(1), new Token(TokenType.PLUS_ASSIGN, "+="), Lit(2));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitBinaryExpression(expr));
    }

    [Fact]
    public void EmitBinaryArithmetic_UnsupportedOperator_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var method = typeof(BytecodeGenerator).GetMethod("EmitBinaryArithmetic", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        _ = AssertInnerException<NotSupportedException>(() => method!.Invoke(generator, [TokenType.ASSIGN]));
    }

    [Fact]
    public void UnaryExpression_UnsupportedOperator_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new UnaryExpression(new Token(TokenType.PLUS, "+"), Lit(1));

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => generator.VisitUnaryExpression(expr));
    }

    [Fact]
    public void IncrementDecrement_NoFunction_Throws()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var expr = new UnaryExpression(new Token(TokenType.INCREMENT, "++"), new IdentifierExpression(new Token(TokenType.IDENTIFIER, "x")));

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => generator.VisitUnaryExpression(expr));
    }

    [Fact]
    public void IncrementDecrement_LocalNotFound_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new UnaryExpression(new Token(TokenType.INCREMENT, "++"), new IdentifierExpression(new Token(TokenType.IDENTIFIER, "x")));

        // Act & Assert
        Assert.Throws<Exception>(() => generator.VisitUnaryExpression(expr));
    }

    [Fact]
    public void IncrementDecrement_MemberAccess_Postfix_Works()
    {
        // Arrange
        const string code = """
                            class Box { int val; }
                            fn main() {
                                Box b = new Box();
                                b.val++;
                            }
                            """;

        // Act
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        // Assert
        Assert.Contains(inst, i => i.OpCode == OpCode.SET_FIELD);
    }

    [Fact]
    public void IncrementDecrement_MemberAccess_Prefix_Works()
    {
        // Arrange
        const string code = """
                            class Box { int val; }
                            fn main() {
                                Box b = new Box();
                                ++b.val;
                            }
                            """;

        // Act
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        // Assert
        Assert.Contains(inst, i => i.OpCode == OpCode.SET_FIELD);
    }

    [Fact]
    public void IncrementDecrement_ArrayAccess_Postfix_Works()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int[] a = new int[2];
                                a[0]++;
                            }
                            """;

        // Act
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        // Assert
        Assert.Contains(inst, i => i.OpCode == OpCode.SET_ELEMENT);
    }

    [Fact]
    public void IncrementDecrement_ArrayAccess_Prefix_Works()
    {
        // Arrange
        const string code = """
                            fn main() {
                                int[] a = new int[2];
                                ++a[0];
                            }
                            """;

        // Act
        var program = TestHelpers.Generate(code);
        var inst = TestHelpers.GetInstructions(program, "main");

        // Assert
        Assert.Contains(inst, i => i.OpCode == OpCode.SET_ELEMENT);
    }

    [Fact]
    public void IncrementDecrement_InvalidTarget_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new UnaryExpression(new Token(TokenType.INCREMENT, "++"), Lit(1));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitUnaryExpression(expr));
    }

    [Fact]
    public void IdentifierExpression_Global_Loads()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out var func, out _);
        var program = GetProgram(generator);
        program.Globals.Add(new BytecodeVariable(0, "g", new PrimitiveType("int")));

        // Act
        generator.VisitIdentifierExpression(new IdentifierExpression(new Token(TokenType.IDENTIFIER, "g")));

        // Assert
        Assert.Contains(func.Code, i => i.OpCode == OpCode.LOAD_GLOBAL);
    }

    [Fact]
    public void IdentifierExpression_Unknown_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new IdentifierExpression(new Token(TokenType.IDENTIFIER, "missing"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitIdentifierExpression(expr));
    }

    [Fact]
    public void CallExpression_TimeWithArgs_EvaluatesArgs()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out var func, out _);
        var call = new CallExpression(
            new IdentifierExpression(new Token(TokenType.IDENTIFIER, "time")),
            [Lit(1)]);

        // Act
        generator.VisitCallExpression(call);

        // Assert
        Assert.Contains(func.Code, i => i.OpCode == OpCode.CALL_NATIVE && (int)i.Operands[0] == 1);
    }

    [Fact]
    public void CallExpression_MethodNotFound_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var program = GetProgram(generator);
        var cls = new BytecodeClass(0, "Box");
        program.Classes.Add(cls);
        program.Globals.Add(new BytecodeVariable(0, "b", new ClassType(cls.ClassId, cls.Name)));

        var call = new CallExpression(
            new MemberAccessExpression(new IdentifierExpression(new Token(TokenType.IDENTIFIER, "b")), "missing"),
            []);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitCallExpression(call));
    }

    [Fact]
    public void CallExpression_InvalidTarget_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var call = new CallExpression(Lit(1), []);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitCallExpression(call));
    }

    [Fact]
    public void MemberAccess_FieldNotFound_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var program = GetProgram(generator);
        var cls = new BytecodeClass(0, "Box");
        program.Classes.Add(cls);
        program.Globals.Add(new BytecodeVariable(0, "b", new ClassType(cls.ClassId, cls.Name)));

        var expr = new MemberAccessExpression(new IdentifierExpression(new Token(TokenType.IDENTIFIER, "b")), "missing");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitMemberAccessExpression(expr));
    }

    [Fact]
    public void NewObjectExpression_WithArgs_EmitsNewObject()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out var func, out _);
        var program = GetProgram(generator);
        var cls = new BytecodeClass(0, "Point");
        program.Classes.Add(cls);

        var expr = new NewObjectExpression("Point", [Lit(1)]);

        // Act
        generator.VisitNewObjectExpression(expr);

        // Assert
        Assert.Contains(func.Code, i => i.OpCode == OpCode.NEW_OBJECT);
    }

    [Fact]
    public void NewObjectExpression_UnknownClass_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new NewObjectExpression("Missing", []);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitNewObjectExpression(expr));
    }

    [Fact]
    public void ResolveType_CharAndString_AreRegistered()
    {
        // Arrange
        const string code = """
                            fn main() {
                                char c = 'a';
                                string s = "x";
                            }
                            """;

        // Act
        var program = TestHelpers.Generate(code);

        // Assert
        Assert.Contains(program.Types, t => t is PrimitiveType p && p.Name == "char");
        Assert.Contains(program.Types, t => t is PrimitiveType p && p.Name == "string");
    }

    [Fact]
    public void GetOrCreatePrimitive_ReturnsCachedInstance()
    {
        // Arrange
        var generator = new BytecodeGenerator();
        var method = typeof(BytecodeGenerator).GetMethod("GetOrCreatePrimitive", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var first = (PrimitiveType)method!.Invoke(generator, ["int"])!;
        var second = (PrimitiveType)method.Invoke(generator, ["int"])!;

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void ResolveField_FieldMissing_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var program = GetProgram(generator);
        var cls = new BytecodeClass(0, "Box");
        program.Classes.Add(cls);
        program.Globals.Add(new BytecodeVariable(0, "b", new ClassType(cls.ClassId, cls.Name)));

        var expr = new BinaryExpression(
            new MemberAccessExpression(new IdentifierExpression(new Token(TokenType.IDENTIFIER, "b")), "missing"),
            new Token(TokenType.ASSIGN, "="),
            Lit(1));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitBinaryExpression(expr));
    }

    [Fact]
    public void ResolveClass_ObjectNotIdentifier_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var expr = new MemberAccessExpression(Lit(1), "x");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitMemberAccessExpression(expr));
    }

    [Fact]
    public void ResolveClass_NonClassVariable_Throws()
    {
        // Arrange
        var generator = CreateGeneratorWithFunction(out _, out _);
        var program = GetProgram(generator);
        program.Globals.Add(new BytecodeVariable(0, "i", new PrimitiveType("int")));
        var expr = new MemberAccessExpression(new IdentifierExpression(new Token(TokenType.IDENTIFIER, "i")), "x");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.VisitMemberAccessExpression(expr));
    }
}
