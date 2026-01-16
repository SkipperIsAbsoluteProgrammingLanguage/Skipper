using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime.Values;

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
                        else if (val1.Kind == ValueKind.Double || val2.Kind == ValueKind.Double)
                        {
                            var d1 = val1.Kind == ValueKind.Double ? val1.AsDouble() : val1.AsInt();
                            var d2 = val2.Kind == ValueKind.Double ? val2.AsDouble() : val2.AsInt();
                            ctx.PushStack(Value.FromDouble(d1 + d2));
                        }
                        else
                        {
                            ctx.PushStack(Value.FromInt(val1.AsInt() + val2.AsInt()));
                        }

                        ip++;
                    }
                    break;

                    case OpCode.SUB:
                    {
                        var b = ctx.PopStack().AsInt();
                        var a = ctx.PopStack().AsInt();
                        ctx.PushStack(Value.FromInt(a - b));
                        ip++;
                    }
                    break;

                    case OpCode.MUL:
                    {
                        var b = ctx.PopStack().AsInt();
                        var a = ctx.PopStack().AsInt();
                        ctx.PushStack(Value.FromInt(a * b));
                        ip++;
                    }
                    break;

                    case OpCode.DIV:
                    {
                        var b = ctx.PopStack().AsInt();
                        if (b == 0)
                        {
                            throw new DivideByZeroException();
                        }

                        var a = ctx.PopStack().AsInt();
                        ctx.PushStack(Value.FromInt(a / b));
                        ip++;
                    }
                    break;

                    case OpCode.MOD:
                    {
                        var b = ctx.PopStack().AsInt();
                        if (b == 0)
                        {
                            throw new DivideByZeroException();
                        }

                        var a = ctx.PopStack().AsInt();
                        ctx.PushStack(Value.FromInt(a % b));
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
                        else
                        {
                            ctx.PushStack(Value.FromInt(-val.AsInt()));
                        }

                        ip++;
                    }
                    break;

                    case OpCode.CMP_EQ:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        ctx.PushStack(Value.FromBool(a.Raw == b.Raw));
                        ip++;
                    }
                    break;

                    case OpCode.CMP_NE:
                    {
                        var b = ctx.PopStack();
                        var a = ctx.PopStack();
                        ctx.PushStack(Value.FromBool(a.Raw != b.Raw));
                        ip++;
                    }
                    break;

                    case OpCode.CMP_LT:
                    {
                        var b = ctx.PopStack().AsInt();
                        var a = ctx.PopStack().AsInt();
                        ctx.PushStack(Value.FromBool(a < b));
                        ip++;
                    }
                    break;

                    case OpCode.CMP_GT:
                    {
                        var b = ctx.PopStack().AsInt();
                        var a = ctx.PopStack().AsInt();
                        ctx.PushStack(Value.FromBool(a > b));
                        ip++;
                    }
                    break;

                    case OpCode.CMP_LE:
                    {
                        var b = ctx.PopStack().AsInt();
                        var a = ctx.PopStack().AsInt();
                        ctx.PushStack(Value.FromBool(a <= b));
                        ip++;
                    }
                    break;

                    case OpCode.CMP_GE:
                    {
                        var b = ctx.PopStack().AsInt();
                        var a = ctx.PopStack().AsInt();
                        ctx.PushStack(Value.FromBool(a >= b));
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
                        var fieldId = Convert.ToInt32(instr.Operands[1]);
                        var objRef = ctx.PopStack();
                        VmChecks.CheckNull(objRef);

                        var val = ctx.Runtime.ReadField(objRef.AsObject(), fieldId);
                        ctx.PushStack(val);
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
                Console.WriteLine($"[VM Runtime Error] Func: {func.Name}, IP: {ip}, Op: {instr.OpCode}. Error: {ex.Message}");
                throw;
            }
        }
    }
}
