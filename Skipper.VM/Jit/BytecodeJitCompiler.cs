using System.Reflection;
using System.Reflection.Emit;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime.Values;
using Skipper.VM.Execution;
using BytecodeOpCode = Skipper.BaitCode.Objects.Instructions.OpCode;

namespace Skipper.VM.Jit;

// JIT-компилятор: превращает байткод функции в IL DynamicMethod.
public sealed class BytecodeJitCompiler
{
    // Кэш скомпилированных методов по ID функции.
    private readonly Dictionary<int, JitMethod> _cache = new();

    // MethodInfo для доступа к операциям стека и контекста.
    private static readonly MethodInfo PushStackMethod = typeof(JitExecutionContext)
        .GetMethod(nameof(JitExecutionContext.PushStack))!;
    private static readonly MethodInfo PopStackMethod = typeof(JitExecutionContext)
        .GetMethod(nameof(JitExecutionContext.PopStack))!;
    private static readonly MethodInfo PeekStackMethod = typeof(JitExecutionContext)
        .GetMethod(nameof(JitExecutionContext.PeekStack))!;
    private static readonly MethodInfo HasStackMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.HasStack), BindingFlags.Instance | BindingFlags.Public)!;
    private static readonly MethodInfo LoadConstMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.LoadConst), BindingFlags.Instance | BindingFlags.Public)!;
    private static readonly MethodInfo LoadLocalMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.LoadLocal), BindingFlags.Instance | BindingFlags.Public)!;
    private static readonly MethodInfo StoreLocalMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.StoreLocal), BindingFlags.Instance | BindingFlags.Public)!;
    private static readonly MethodInfo LoadGlobalMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.LoadGlobal), BindingFlags.Instance | BindingFlags.Public)!;
    private static readonly MethodInfo StoreGlobalMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.StoreGlobal), BindingFlags.Instance | BindingFlags.Public)!;
    private static readonly MethodInfo CallFunctionMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.CallFunction), BindingFlags.Instance | BindingFlags.Public)!;
    private static readonly MethodInfo CallMethodMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.CallMethod), BindingFlags.Instance | BindingFlags.Public)!;
    private static readonly MethodInfo CallNativeMethod = typeof(ExecutionContextBase)
        .GetMethod(nameof(ExecutionContextBase.CallNative), BindingFlags.Instance | BindingFlags.Public)!;

    // MethodInfo для арифметики/логики (вынесено в JitOps).
    private static readonly MethodInfo AddMethod = typeof(JitOps).GetMethod(nameof(JitOps.Add), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo SubMethod = typeof(JitOps).GetMethod(nameof(JitOps.Sub), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo MulMethod = typeof(JitOps).GetMethod(nameof(JitOps.Mul), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo DivMethod = typeof(JitOps).GetMethod(nameof(JitOps.Div), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo ModMethod = typeof(JitOps).GetMethod(nameof(JitOps.Mod), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo NegMethod = typeof(JitOps).GetMethod(nameof(JitOps.Neg), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CmpEqMethod = typeof(JitOps).GetMethod(nameof(JitOps.CmpEq), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CmpNeMethod = typeof(JitOps).GetMethod(nameof(JitOps.CmpNe), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CmpLtMethod = typeof(JitOps).GetMethod(nameof(JitOps.CmpLt), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CmpGtMethod = typeof(JitOps).GetMethod(nameof(JitOps.CmpGt), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CmpLeMethod = typeof(JitOps).GetMethod(nameof(JitOps.CmpLe), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CmpGeMethod = typeof(JitOps).GetMethod(nameof(JitOps.CmpGe), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo AndMethod = typeof(JitOps).GetMethod(nameof(JitOps.And), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo OrMethod = typeof(JitOps).GetMethod(nameof(JitOps.Or), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo NotMethod = typeof(JitOps).GetMethod(nameof(JitOps.Not), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo IsTrueMethod = typeof(JitOps).GetMethod(nameof(JitOps.IsTrue), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo NewObjectMethod = typeof(JitOps).GetMethod(nameof(JitOps.NewObject), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo NewArrayMethod = typeof(JitOps).GetMethod(nameof(JitOps.NewArray), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo GetFieldMethod = typeof(JitOps).GetMethod(nameof(JitOps.GetField), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo SetFieldMethod = typeof(JitOps).GetMethod(nameof(JitOps.SetField), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo GetElementMethod = typeof(JitOps).GetMethod(nameof(JitOps.GetElement), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo SetElementMethod = typeof(JitOps).GetMethod(nameof(JitOps.SetElement), BindingFlags.Static | BindingFlags.NonPublic)!;

    internal JitMethod GetOrCompile(BytecodeFunction func, BytecodeProgram program)
    {
        // Кэшируем результат компиляции по ID функции.
        if (_cache.TryGetValue(func.FunctionId, out var method))
        {
            return method;
        }

        method = Compile(func, program);
        _cache[func.FunctionId] = method;
        return method;
    }

    private static JitMethod Compile(BytecodeFunction func, BytecodeProgram program)
    {
        // Локальная оптимизация байткода перед компиляцией.
        var code = SimplifyBranches(func, program);

        // Генерируем IL-метод с сигнатурой: void(JitExecutionContext).
        var dm = new DynamicMethod(
            $"jit_{func.Name}_{func.FunctionId}",
            typeof(void),
            [typeof(JitExecutionContext)],
            typeof(BytecodeJitCompiler).Module,
            true);

        var il = dm.GetILGenerator();

        // Временные локалы IL для промежуточных значений.
        var tmp1 = il.DeclareLocal(typeof(Value));
        var tmp2 = il.DeclareLocal(typeof(Value));
        var tmp3 = il.DeclareLocal(typeof(Value));

        // Метки для переходов по байткоду.
        var labels = new Label[code.Count];
        for (var i = 0; i < labels.Length; i++)
        {
            labels[i] = il.DefineLabel();
        }
        var endLabel = il.DefineLabel();

        // Транслируем каждую инструкцию байткода в IL.
        for (var ip = 0; ip < code.Count; ip++)
        {
            il.MarkLabel(labels[ip]);
            var instr = code[ip];
            switch (instr.OpCode)
            {
                case BytecodeOpCode.PUSH:
                {
                    var constId = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, constId);
                    il.EmitCall(OpCodes.Callvirt, LoadConstMethod, null);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.POP:
                {
                    var skip = il.DefineLabel();
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, HasStackMethod, null);
                    il.Emit(OpCodes.Brfalse, skip);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Pop);
                    il.MarkLabel(skip);
                    break;
                }

                case BytecodeOpCode.DUP:
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PeekStackMethod, null);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.SWAP:
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp2);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc, tmp2);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.LOAD_LOCAL:
                {
                    var slot = Convert.ToInt32(instr.Operands[1]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, slot);
                    il.EmitCall(OpCodes.Callvirt, LoadLocalMethod, null);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.STORE_LOCAL:
                {
                    var slot = Convert.ToInt32(instr.Operands[1]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, slot);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Callvirt, StoreLocalMethod, null);
                    break;
                }

                case BytecodeOpCode.LOAD_GLOBAL:
                {
                    var slot = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, slot);
                    il.EmitCall(OpCodes.Callvirt, LoadGlobalMethod, null);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.STORE_GLOBAL:
                {
                    var slot = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, slot);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Callvirt, StoreGlobalMethod, null);
                    break;
                }

                case BytecodeOpCode.ADD:
                {
                    EmitBinaryWithContext(il, tmp1, tmp2, AddMethod);
                    break;
                }

                case BytecodeOpCode.SUB:
                {
                    EmitBinary(il, tmp1, tmp2, SubMethod);
                    break;
                }

                case BytecodeOpCode.MUL:
                {
                    EmitBinary(il, tmp1, tmp2, MulMethod);
                    break;
                }

                case BytecodeOpCode.DIV:
                {
                    EmitBinary(il, tmp1, tmp2, DivMethod);
                    break;
                }

                case BytecodeOpCode.MOD:
                {
                    EmitBinary(il, tmp1, tmp2, ModMethod);
                    break;
                }

                case BytecodeOpCode.NEG:
                {
                    EmitUnary(il, tmp1, NegMethod);
                    break;
                }

                case BytecodeOpCode.CMP_EQ:
                {
                    EmitBinary(il, tmp1, tmp2, CmpEqMethod);
                    break;
                }

                case BytecodeOpCode.CMP_NE:
                {
                    EmitBinary(il, tmp1, tmp2, CmpNeMethod);
                    break;
                }

                case BytecodeOpCode.CMP_LT:
                {
                    EmitBinary(il, tmp1, tmp2, CmpLtMethod);
                    break;
                }

                case BytecodeOpCode.CMP_GT:
                {
                    EmitBinary(il, tmp1, tmp2, CmpGtMethod);
                    break;
                }

                case BytecodeOpCode.CMP_LE:
                {
                    EmitBinary(il, tmp1, tmp2, CmpLeMethod);
                    break;
                }

                case BytecodeOpCode.CMP_GE:
                {
                    EmitBinary(il, tmp1, tmp2, CmpGeMethod);
                    break;
                }

                case BytecodeOpCode.AND:
                {
                    EmitBinary(il, tmp1, tmp2, AndMethod);
                    break;
                }

                case BytecodeOpCode.OR:
                {
                    EmitBinary(il, tmp1, tmp2, OrMethod);
                    break;
                }

                case BytecodeOpCode.NOT:
                {
                    EmitUnary(il, tmp1, NotMethod);
                    break;
                }

                case BytecodeOpCode.JUMP:
                {
                    var target = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Br, target == code.Count ? endLabel : labels[target]);
                    break;
                }

                case BytecodeOpCode.JUMP_IF_TRUE:
                {
                    var target = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Call, IsTrueMethod, null);
                    il.Emit(OpCodes.Brtrue, target == code.Count ? endLabel : labels[target]);
                    break;
                }

                case BytecodeOpCode.JUMP_IF_FALSE:
                {
                    var target = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Call, IsTrueMethod, null);
                    il.Emit(OpCodes.Brfalse, target == code.Count ? endLabel : labels[target]);
                    break;
                }

                case BytecodeOpCode.CALL:
                {
                    var funcId = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, funcId);
                    il.EmitCall(OpCodes.Callvirt, CallFunctionMethod, null);
                    break;
                }

                case BytecodeOpCode.CALL_METHOD:
                {
                    var classId = Convert.ToInt32(instr.Operands[0]);
                    var methodId = Convert.ToInt32(instr.Operands[1]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, classId);
                    il.Emit(OpCodes.Ldc_I4, methodId);
                    il.EmitCall(OpCodes.Callvirt, CallMethodMethod, null);
                    break;
                }

                case BytecodeOpCode.RETURN:
                {
                    il.Emit(OpCodes.Ret);
                    break;
                }

                case BytecodeOpCode.NEW_OBJECT:
                {
                    var classId = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, classId);
                    il.EmitCall(OpCodes.Call, NewObjectMethod, null);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.NEW_ARRAY:
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Call, NewArrayMethod, null);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.GET_FIELD:
                {
                    var fieldId = Convert.ToInt32(instr.Operands[1]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.Emit(OpCodes.Ldc_I4, fieldId);
                    il.EmitCall(OpCodes.Call, GetFieldMethod, null);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.SET_FIELD:
                {
                    var fieldId = Convert.ToInt32(instr.Operands[1]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp2);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc, tmp2);
                    il.Emit(OpCodes.Ldc_I4, fieldId);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Call, SetFieldMethod, null);
                    break;
                }

                case BytecodeOpCode.GET_ELEMENT:
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp2);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc, tmp2);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Call, GetElementMethod, null);
                    il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
                    break;
                }

                case BytecodeOpCode.SET_ELEMENT:
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp1);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp2);
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
                    il.Emit(OpCodes.Stloc, tmp3);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc, tmp3);
                    il.Emit(OpCodes.Ldloc, tmp2);
                    il.Emit(OpCodes.Ldloc, tmp1);
                    il.EmitCall(OpCodes.Call, SetElementMethod, null);
                    break;
                }

                case BytecodeOpCode.CALL_NATIVE:
                {
                    var nativeId = Convert.ToInt32(instr.Operands[0]);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, nativeId);
                    il.EmitCall(OpCodes.Callvirt, CallNativeMethod, null);
                    break;
                }

                default:
                    throw new NotSupportedException($"Unsupported opcode {instr.OpCode}");
            }
        }

        il.MarkLabel(endLabel);
        il.Emit(OpCodes.Ret);

        return (JitMethod)dm.CreateDelegate(typeof(JitMethod));
    }

    private static List<Instruction> SimplifyBranches(BytecodeFunction func, BytecodeProgram program)
    {
        // Локальная оптимизация: упрощаем ветвления на константных условиях.
        var oldCode = func.Code;
        if (oldCode.Count < 2)
        {
            return oldCode;
        }

        // Новый список инструкций и таблица соответствий старых/новых индексов.
        var newCode = new List<Instruction>(oldCode.Count);
        var map = new int[oldCode.Count + 1];
        Array.Fill(map, -1);
        var jumpFixups = new List<int>();

        for (var i = 0; i < oldCode.Count; i++)
        {
            if (i + 1 < oldCode.Count &&
                oldCode[i].OpCode == BytecodeOpCode.PUSH &&
                (oldCode[i + 1].OpCode == BytecodeOpCode.JUMP_IF_FALSE ||
                 oldCode[i + 1].OpCode == BytecodeOpCode.JUMP_IF_TRUE))
            {
                // Схема: PUSH constBool; JUMP_IF_* => можно решить на месте.
                var constId = Convert.ToInt32(oldCode[i].Operands[0]);
                if (TryGetConstBool(program, constId, out var cond))
                {
                    var next = oldCode[i + 1];
                    var target = Convert.ToInt32(next.Operands[0]);
                    var take = (next.OpCode == BytecodeOpCode.JUMP_IF_TRUE && cond) ||
                               (next.OpCode == BytecodeOpCode.JUMP_IF_FALSE && !cond);

                    if (take)
                    {
                        // Заменяем на безусловный переход.
                        newCode.Add(new Instruction(BytecodeOpCode.JUMP, target));
                        jumpFixups.Add(newCode.Count - 1);
                        map[i] = newCode.Count - 1;
                        map[i + 1] = newCode.Count - 1;
                    }
                    else
                    {
                        // Переход не нужен: удаляем обе инструкции.
                        map[i] = newCode.Count;
                        map[i + 1] = newCode.Count;
                    }

                    i++;
                    continue;
                }
            }

            var instr = oldCode[i];
            newCode.Add(instr);
            map[i] = newCode.Count - 1;
            if (instr.OpCode is BytecodeOpCode.JUMP or BytecodeOpCode.JUMP_IF_FALSE or BytecodeOpCode.JUMP_IF_TRUE)
            {
                jumpFixups.Add(newCode.Count - 1);
            }
        }

        // Заполняем пробелы в таблице соответствий.
        map[oldCode.Count] = newCode.Count;

        var nextNew = newCode.Count;
        for (var i = oldCode.Count; i >= 0; i--)
        {
            if (map[i] >= 0)
            {
                nextNew = map[i];
            }
            else
            {
                map[i] = nextNew;
            }
        }

        foreach (var idx in jumpFixups)
        {
            // Пересчитываем цели переходов на новые индексы.
            var instr = newCode[idx];
            var oldTarget = Convert.ToInt32(instr.Operands[0]);
            var newTarget = map[oldTarget];
            newCode[idx] = new Instruction(instr.OpCode, newTarget);
        }

        return newCode;
    }

    private static bool TryGetConstBool(BytecodeProgram program, int constId, out bool value)
    {
        // Преобразование константы к bool для упрощения ветвлений.
        value = false;
        if (constId < 0 || constId >= program.ConstantPool.Count)
        {
            return false;
        }

        var c = program.ConstantPool[constId];
        switch (c)
        {
            case null:
                value = false;
                return true;
            case bool b:
                value = b;
                return true;
            case int i:
                value = i != 0;
                return true;
            case long l:
                value = l != 0;
                return true;
            case double d:
                value = Math.Abs(d) > double.Epsilon;
                return true;
            case char ch:
                value = ch != '\0';
                return true;
            case string:
                value = true;
                return true;
            default:
                return false;
        }
    }

    private static void EmitUnary(ILGenerator il, LocalBuilder tmp, MethodInfo opMethod)
    {
        // Генерация IL для унарной операции: pop -> op -> push.
        il.Emit(OpCodes.Ldarg_0);
        il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
        il.Emit(OpCodes.Stloc, tmp);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc, tmp);
        il.EmitCall(OpCodes.Call, opMethod, null);
        il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
    }

    private static void EmitBinary(ILGenerator il, LocalBuilder tmp1, LocalBuilder tmp2, MethodInfo opMethod)
    {
        // Генерация IL для бинарной операции без контекста.
        il.Emit(OpCodes.Ldarg_0);
        il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
        il.Emit(OpCodes.Stloc, tmp1);
        il.Emit(OpCodes.Ldarg_0);
        il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
        il.Emit(OpCodes.Stloc, tmp2);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc, tmp2);
        il.Emit(OpCodes.Ldloc, tmp1);
        il.EmitCall(OpCodes.Call, opMethod, null);
        il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
    }

    private static void EmitBinaryWithContext(ILGenerator il, LocalBuilder tmp1, LocalBuilder tmp2, MethodInfo opMethod)
    {
        // Генерация IL для бинарной операции с контекстом (например, строки).
        il.Emit(OpCodes.Ldarg_0);
        il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
        il.Emit(OpCodes.Stloc, tmp1);
        il.Emit(OpCodes.Ldarg_0);
        il.EmitCall(OpCodes.Callvirt, PopStackMethod, null);
        il.Emit(OpCodes.Stloc, tmp2);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc, tmp2);
        il.Emit(OpCodes.Ldloc, tmp1);
        il.EmitCall(OpCodes.Call, opMethod, null);
        il.EmitCall(OpCodes.Callvirt, PushStackMethod, null);
    }
}
