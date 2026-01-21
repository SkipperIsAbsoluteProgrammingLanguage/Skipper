using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.BaitCode.Types;
using Skipper.Runtime.Values;
using Skipper.VM.Execution;

namespace Skipper.VM.Interpreter;

public static class BytecodeInterpreter
{
    public static void Execute(IInterpreterContext ctx, BytecodeFunction func)
    {
        var code = func.Code;
        var ip = 0;

        while (ip < code.Count)
        {
            var instr = code[ip];
            if (ctx.Trace)
            {
                Console.WriteLine($"[STEP] Func: {func.Name}, IP: {ip} (Total: {code.Count}), Op: {instr.OpCode}");
            }

            try
            {
                switch (instr.OpCode)
                {
                    case OpCode.PUSH:
                    {
                        var constId = Convert.ToInt32(instr.Operands[0]);
                        ctx.PushStack(ctx.LoadConst(constId));
                        ip++;
                    }
                    break;

                    case OpCode.POP:
                        if (ctx.HasStack())
                        {
                            _ = ctx.PopStack();
                        }

                        ip++;
                        break;

                    case OpCode.DUP:
                        ctx.PushStack(ctx.PeekStack());
                        ip++;
                        break;

                    case OpCode.SWAP:
                    {
                        var top = ctx.PopStack();
                        var below = ctx.PopStack();
                        ctx.PushStack(top);
                        ctx.PushStack(below);
                        ip++;
                    }
                    break;

                    case OpCode.LOAD_LOCAL:
                    {
                        var slot = Convert.ToInt32(instr.Operands[1]);
                        ctx.PushStack(ctx.LoadLocal(slot));
                        ip++;
                    }
                    break;

                    case OpCode.STORE_LOCAL:
                    {
                        var slot = Convert.ToInt32(instr.Operands[1]);
                        ctx.StoreLocal(slot, ctx.PopStack());
                        ip++;
                    }
                    break;

                    case OpCode.LOAD_GLOBAL:
                    {
                        var slot = Convert.ToInt32(instr.Operands[0]);
                        ctx.PushStack(ctx.LoadGlobal(slot));
                        ip++;
                    }
                    break;

                    case OpCode.STORE_GLOBAL:
                    {
                        var slot = Convert.ToInt32(instr.Operands[0]);
                        ctx.StoreGlobal(slot, ctx.PopStack());
                        ip++;
                    }
                    break;

                    case OpCode.ADD:
                    {
                        var val2 = ctx.PopStack();
                        var val1 = ctx.PopStack();

                        if (val1.Kind == ValueKind.ObjectRef && val2.Kind == ValueKind.ObjectRef)
                        {
                            var newPtr = ctx.Runtime.ConcatStrings(val1.AsObject(), val2.AsObject());
                            ctx.PushStack(Value.FromObject(newPtr));
                        }
                        else if (val1.Kind == ValueKind.ObjectRef && IsScalarForStringConcat(val2))
                        {
                            var rightPtr = ctx.Runtime.AllocateString(FormatScalar(val2));
                            var newPtr = ctx.Runtime.ConcatStrings(val1.AsObject(), rightPtr);
                            ctx.PushStack(Value.FromObject(newPtr));
                        }
                        else if (IsScalarForStringConcat(val1) && val2.Kind == ValueKind.ObjectRef)
                        {
                            var leftPtr = ctx.Runtime.AllocateString(FormatScalar(val1));
                            var newPtr = ctx.Runtime.ConcatStrings(leftPtr, val2.AsObject());
                            ctx.PushStack(Value.FromObject(newPtr));
                        }
                        else if (val1.Kind == ValueKind.Double || val2.Kind == ValueKind.Double)
                        {
                            var d1 = ToDouble(val1);
                            var d2 = ToDouble(val2);
                            ctx.PushStack(Value.FromDouble(d1 + d2));
                        }
                        else if (val1.Kind == ValueKind.Long || val2.Kind == ValueKind.Long)
                        {
                            var l1 = ToLong(val1);
                            var l2 = ToLong(val2);
                            ctx.PushStack(Value.FromLong(unchecked(l1 + l2)));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromInt(unchecked(val1.AsInt() + val2.AsInt())));
                        }

                        ip++;
                    }
                    break;

                    case OpCode.SUB:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
                        {
                            ctx.PushStack(Value.FromDouble(ToDouble(a) - ToDouble(b)));
                        }
                        else if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
                        {
                            ctx.PushStack(Value.FromLong(unchecked(ToLong(a) - ToLong(b))));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromInt(unchecked(a.AsInt() - b.AsInt())));
                        }
                        ip++;
                    }
                    break;

                    case OpCode.MUL:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
                        {
                            ctx.PushStack(Value.FromDouble(ToDouble(a) * ToDouble(b)));
                        }
                        else if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
                        {
                            ctx.PushStack(Value.FromLong(unchecked(ToLong(a) * ToLong(b))));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromInt(unchecked(a.AsInt() * b.AsInt())));
                        }
                        ip++;
                    }
                    break;

                    case OpCode.DIV:
                    {
                        var b = ctx.PopStack();
                        if (b.Kind is ValueKind.Long or ValueKind.Int && ToLong(b) == 0)
                        {
                            throw new DivideByZeroException();
                        }

                        var a = ctx.PopStack();
                        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
                        {
                            ctx.PushStack(Value.FromDouble(ToDouble(a) / ToDouble(b)));
                        }
                        else if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
                        {
                            ctx.PushStack(Value.FromLong(ToLong(a) / ToLong(b)));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromInt(a.AsInt() / b.AsInt()));
                        }
                        ip++;
                    }
                    break;

                    case OpCode.MOD:
                    {
                        var b = ctx.PopStack();
                        if (b.Kind is ValueKind.Long or ValueKind.Int && ToLong(b) == 0)
                        {
                            throw new DivideByZeroException();
                        }

                        var a = ctx.PopStack();
                        if (a.Kind == ValueKind.Double || b.Kind == ValueKind.Double)
                        {
                            ctx.PushStack(Value.FromDouble(ToDouble(a) % ToDouble(b)));
                        }
                        else if (a.Kind == ValueKind.Long || b.Kind == ValueKind.Long)
                        {
                            ctx.PushStack(Value.FromLong(ToLong(a) % ToLong(b)));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromInt(a.AsInt() % b.AsInt()));
                        }
                        ip++;
                    }
                    break;

                    case OpCode.NEG:
                    {
                        var val = ctx.PopStack();
                        if (val.Kind == ValueKind.Double)
                        {
                            ctx.PushStack(Value.FromDouble(-val.AsDouble()));
                        }
                        else if (val.Kind == ValueKind.Long)
                        {
                            ctx.PushStack(Value.FromLong(unchecked(-val.AsLong())));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromInt(unchecked(-val.AsInt())));
                        }

                        ip++;
                    }
                    break;

                    case OpCode.CMP_EQ:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        if (IsNumeric(a) && IsNumeric(b))
                        {
                            ctx.PushStack(Value.FromBool(CompareNumeric(a, b) == 0));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromBool(a.Raw == b.Raw));
                        }
                        ip++;
                    }
                    break;

                    case OpCode.CMP_NE:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        if (IsNumeric(a) && IsNumeric(b))
                        {
                            ctx.PushStack(Value.FromBool(CompareNumeric(a, b) != 0));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromBool(a.Raw != b.Raw));
                        }
                        ip++;
                    }
                    break;

                    case OpCode.CMP_LT:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        ctx.PushStack(Value.FromBool(CompareNumeric(a, b) < 0));
                        ip++;
                    }
                    break;

                    case OpCode.CMP_GT:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        ctx.PushStack(Value.FromBool(CompareNumeric(a, b) > 0));
                        ip++;
                    }
                    break;

                    case OpCode.CMP_LE:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        ctx.PushStack(Value.FromBool(CompareNumeric(a, b) <= 0));
                        ip++;
                    }
                    break;

                    case OpCode.CMP_GE:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        ctx.PushStack(Value.FromBool(CompareNumeric(a, b) >= 0));
                        ip++;
                    }
                    break;

                    case OpCode.AND:
                    {
                        var b = ctx.PopStack().AsBool();
                        var a = ctx.PopStack().AsBool();
                        ctx.PushStack(Value.FromBool(a && b));
                        ip++;
                    }
                    break;

                    case OpCode.OR:
                    {
                        var b = ctx.PopStack().AsBool();
                        var a = ctx.PopStack().AsBool();
                        ctx.PushStack(Value.FromBool(a || b));
                        ip++;
                    }
                    break;

                    case OpCode.NOT:
                    {
                        var a = ctx.PopStack().AsBool();
                        ctx.PushStack(Value.FromBool(!a));
                        ip++;
                    }
                    break;

                    case OpCode.JUMP:
                        ip = Convert.ToInt32(instr.Operands[0]);
                        break;

                    case OpCode.JUMP_IF_TRUE:
                    {
                        var cond = ctx.PopStack().AsBool();
                        if (cond)
                        {
                            ip = Convert.ToInt32(instr.Operands[0]);
                        }
                        else
                        {
                            ip++;
                        }
                    }
                    break;

                    case OpCode.JUMP_IF_FALSE:
                    {
                        var cond = ctx.PopStack().AsBool();
                        if (!cond)
                        {
                            ip = Convert.ToInt32(instr.Operands[0]);
                        }
                        else
                        {
                            ip++;
                        }
                    }
                    break;

                    case OpCode.CALL:
                    {
                        var funcId = Convert.ToInt32(instr.Operands[0]);
                        ctx.CallFunction(funcId);
                        ip++;
                    }
                    break;

                    case OpCode.CALL_METHOD:
                    {
                        var classId = Convert.ToInt32(instr.Operands[0]);
                        var methodId = Convert.ToInt32(instr.Operands[1]);
                        ctx.CallMethod(classId, methodId);
                        ip++;
                    }
                    break;

                    case OpCode.RETURN:
                        return;

                    case OpCode.NEW_OBJECT:
                    {
                        var classId = Convert.ToInt32(instr.Operands[0]);
                        var cls = ctx.GetClassById(classId);
                        var payloadSize = cls.Fields.Count * 8;

                        if (!ctx.Runtime.CanAllocate(payloadSize))
                        {
                            ctx.Runtime.Collect(ctx);
                            if (!ctx.Runtime.CanAllocate(payloadSize))
                            {
                                throw new OutOfMemoryException("Heap full after GC");
                            }
                        }

                        var ptr = ctx.Runtime.AllocateObject(payloadSize, classId);
                        ctx.PushStack(Value.FromObject(ptr));
                        ip++;
                    }
                    break;

                    case OpCode.GET_FIELD:
                    {
                        var classId = Convert.ToInt32(instr.Operands[0]);
                        var fieldId = Convert.ToInt32(instr.Operands[1]);
                        var objRef = ctx.PopStack();
                        VmChecks.CheckNull(objRef);

                        var raw = ctx.Runtime.ReadFieldRaw(objRef.AsObject(), fieldId);

                        var cls = ctx.GetClassById(classId);
                        var field = cls.Fields.Values.FirstOrDefault(f => f.FieldId == fieldId);
                        if (field == null)
                        {
                            ctx.PushStack(new Value(raw));
                        }
                        else
                        {
                            var fieldType = field.Type;
                            Value val;
                            if (fieldType is PrimitiveType p && p.Name == "double")
                                val = new Value(ValueKind.Double, raw);
                            else if (fieldType is PrimitiveType p2 && p2.Name == "long")
                                val = new Value(ValueKind.Long, raw);
                            else if (fieldType is PrimitiveType p3 && p3.Name == "bool")
                                val = new Value(ValueKind.Bool, raw);
                            else if (fieldType is PrimitiveType p4 && p4.Name == "char")
                                val = new Value(ValueKind.Char, raw);
                            else
                                val = new Value(raw);

                            ctx.PushStack(val);
                        }

                        ip++;
                    }
                    break;

                    case OpCode.SET_FIELD:
                    {
                        var fieldId = Convert.ToInt32(instr.Operands[1]);
                        var val = ctx.PopStack();
                        var objRef = ctx.PopStack();
                        VmChecks.CheckNull(objRef);

                        ctx.Runtime.WriteField(objRef.AsObject(), fieldId, val);
                        ip++;
                    }
                    break;

                    case OpCode.NEW_ARRAY:
                    {
                        var length = ctx.PopStack().AsInt();
                        if (length < 0)
                        {
                            throw new InvalidOperationException("Array size cannot be negative");
                        }

                        var payloadSize = length * 8;
                        if (!ctx.Runtime.CanAllocate(payloadSize))
                        {
                            ctx.Runtime.Collect(ctx);
                            if (!ctx.Runtime.CanAllocate(payloadSize))
                            {
                                throw new OutOfMemoryException("Heap full after GC (Array)");
                            }
                        }

                        var ptr = ctx.Runtime.AllocateArray(length);
                        ctx.PushStack(Value.FromObject(ptr));
                        ip++;
                    }
                    break;

                    case OpCode.GET_ELEMENT:
                    {
                        var index = ctx.PopStack().AsInt();
                        var arrRef = ctx.PopStack();
                        VmChecks.CheckNull(arrRef);

                        var val = ctx.Runtime.ReadArrayElement(arrRef.AsObject(), index);
                        ctx.PushStack(val);
                        ip++;
                    }
                    break;

                    case OpCode.SET_ELEMENT:
                    {
                        var val = ctx.PopStack();
                        var index = ctx.PopStack().AsInt();
                        var arrRef = ctx.PopStack();
                        VmChecks.CheckNull(arrRef);

                        ctx.Runtime.WriteArrayElement(arrRef.AsObject(), index, val);
                        ip++;
                    }
                    break;

                    case OpCode.CALL_NATIVE:
                    {
                        var nativeId = Convert.ToInt32(instr.Operands[0]);
                        ctx.CallNative(nativeId);
                        ip++;
                    }
                    break;

                    default:
                        throw new NotSupportedException($"Unsupported opcode {instr.OpCode}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[VM Runtime Error] Func: {func.Name}, IP: {ip}, Op: {instr.OpCode}. Error: {ex.Message}");
                throw;
            }
        }
    }

    private static bool IsNumeric(Value value)
    {
        return value.Kind is ValueKind.Int or ValueKind.Long or ValueKind.Double;
    }

    private static long ToLong(Value value)
    {
        return value.Kind == ValueKind.Long ? value.AsLong() : value.AsInt();
    }

    private static double ToDouble(Value value)
    {
        return value.Kind switch
        {
            ValueKind.Double => value.AsDouble(),
            ValueKind.Long => value.AsLong(),
            _ => value.AsInt()
        };
    }

    private static int CompareNumeric(Value left, Value right)
    {
        if (left.Kind == ValueKind.Double || right.Kind == ValueKind.Double)
        {
            return ToDouble(left).CompareTo(ToDouble(right));
        }

        if (left.Kind == ValueKind.Long || right.Kind == ValueKind.Long)
        {
            return ToLong(left).CompareTo(ToLong(right));
        }

        return left.AsInt().CompareTo(right.AsInt());
    }

    private static bool IsScalarForStringConcat(Value value)
    {
        return value.Kind is ValueKind.Int or ValueKind.Long or ValueKind.Double or ValueKind.Bool or ValueKind.Char;
    }

    private static string FormatScalar(Value value)
    {
        return value.Kind switch
        {
            ValueKind.Int => value.AsInt().ToString(),
            ValueKind.Long => value.AsLong().ToString(),
            ValueKind.Double => value.AsDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ValueKind.Bool => value.AsBool() ? "true" : "false",
            ValueKind.Char => value.AsChar().ToString(),
            _ => value.ToString()
        };
    }
}
