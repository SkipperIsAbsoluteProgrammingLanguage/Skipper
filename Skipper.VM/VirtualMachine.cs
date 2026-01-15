using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Objects.Instructions;
using Skipper.Runtime;
using Skipper.Runtime.Abstractions;
using Skipper.Runtime.Values;

namespace Skipper.VM;

public sealed class VirtualMachine : IRootProvider, IVirtualMachine
{
    private readonly RuntimeContext _runtime;
    private readonly BytecodeProgram _program;
    private readonly Value[] _globals;

    // Стек вызовов
    private readonly Stack<StackFrame> _callStack = new();

    // Глобальный стек операндов
    private readonly Stack<Value> _evalStack = new();

    // Регистры VM
    private int _ip;
    private BytecodeFunction? _currentFunc;
    private Value[]? _currentLocals;

    public Value PopStack() => _evalStack.Pop();
    public void PushStack(Value v) => _evalStack.Push(v);
    public Value PeekStack() => _evalStack.Peek();

    public VirtualMachine(BytecodeProgram program, RuntimeContext runtime)
    {
        _program = program;
        _runtime = runtime;
        _globals = new Value[program.Globals.Count];
    }

    public Value Run(string entryPointName)
    {
        var mainFunc = _program.Functions.FirstOrDefault(f => f.Name == entryPointName);
        if (mainFunc == null)
        {
            throw new InvalidOperationException($"Function '{entryPointName}' not found");
        }

        PushFrame(mainFunc, -1);

        try
        {
            while (_currentFunc != null && _ip < _currentFunc.Code.Count)
            {
                Console.WriteLine(
                    $"[STEP] Func: {_currentFunc.Name}, IP: {_ip} (Total: {_currentFunc.Code.Count}), Op: {_currentFunc.Code[_ip].OpCode}");

                var instr = _currentFunc.Code[_ip];
                Execute(instr);

                if (_callStack.Count == 0)
                {
                    break;
                }
            }
        } catch (Exception ex)
        {
            Console.WriteLine(
                $"[VM Runtime Error] Func: {_currentFunc?.Name}, IP: {_ip}, Op: {_currentFunc?.Code[_ip].OpCode}. Error: {ex.Message}");
            throw;
        }

        return _evalStack.Count > 0 ? _evalStack.Pop() : Value.Null();
    }

    private void Execute(Instruction instr)
    {
        switch (instr.OpCode)
        {
            // ===========================
            // Стек и Константы
            // ===========================
            case OpCode.PUSH:
            {
                var constIdx = Convert.ToInt32(instr.Operands[0]);
                var val = _program.ConstantPool[constIdx];
                _evalStack.Push(ValueFromConst(val));
                _ip++;
                break;
            }

            case OpCode.POP:
                if (_evalStack.Count > 0)
                {
                    _ = _evalStack.Pop();
                }

                _ip++;
                break;

            case OpCode.DUP:
                _evalStack.Push(_evalStack.Peek());
                _ip++;
                break;

            case OpCode.SWAP:
                var top = _evalStack.Pop();
                var below = _evalStack.Pop();
                _evalStack.Push(top);
                _evalStack.Push(below);
                _ip++;
                break;

            // ===========================
            // Локальные переменные
            // ===========================
            case OpCode.LOAD_LOCAL:
            {
                // [funcId, slot] -> нужен только slot
                var slot = Convert.ToInt32(instr.Operands[1]);
                _evalStack.Push(_currentLocals![slot]);
                _ip++;
            }
            break;

            case OpCode.STORE_LOCAL:
            {
                var slot = Convert.ToInt32(instr.Operands[1]);
                _currentLocals![slot] = _evalStack.Pop();
                _ip++;
            }
            break;

            // ===========================
            // Глобальные переменные
            // ===========================
            case OpCode.LOAD_GLOBAL:
            {
                var slot = Convert.ToInt32(instr.Operands[0]);
                _evalStack.Push(_globals[slot]);
                _ip++;
            }
            break;

            case OpCode.STORE_GLOBAL:
            {
                var slot = Convert.ToInt32(instr.Operands[0]);
                _globals[slot] = _evalStack.Pop();
                _ip++;
            }
            break;

            // ===========================
            // Арифметика
            // ===========================
            case OpCode.ADD:
            {
                var val2 = _evalStack.Pop();
                var val1 = _evalStack.Pop();

                if (val1.Kind == ValueKind.ObjectRef && val2.Kind == ValueKind.ObjectRef)
                {
                    var newPtr = _runtime.ConcatStrings(val1.AsObject(), val2.AsObject());
                    _evalStack.Push(Value.FromObject(newPtr));
                }
                else if (val1.Kind == ValueKind.Double || val2.Kind == ValueKind.Double)
                {
                    double d1 = val1.Kind == ValueKind.Double ? val1.AsDouble() : val1.AsInt();
                    double d2 = val2.Kind == ValueKind.Double ? val2.AsDouble() : val2.AsInt();
                    _evalStack.Push(Value.FromDouble(d1 + d2));
                }
                else
                {
                    _evalStack.Push(Value.FromInt(val1.AsInt() + val2.AsInt()));
                }

                _ip++;
            }
            break;

            case OpCode.SUB:
            {
                var b = _evalStack.Pop().AsInt();
                var a = _evalStack.Pop().AsInt();
                _evalStack.Push(Value.FromInt(a - b));
                _ip++;
            }
            break;

            case OpCode.MUL:
            {
                var b = _evalStack.Pop().AsInt();
                var a = _evalStack.Pop().AsInt();
                _evalStack.Push(Value.FromInt(a * b));
                _ip++;
            }
            break;

            case OpCode.DIV:
            {
                var b = _evalStack.Pop().AsInt();
                if (b == 0)
                {
                    throw new DivideByZeroException();
                }

                var a = _evalStack.Pop().AsInt();
                _evalStack.Push(Value.FromInt(a / b));
                _ip++;
            }
            break;

            case OpCode.MOD:
            {
                var b = _evalStack.Pop().AsInt();
                if (b == 0)
                {
                    throw new DivideByZeroException();
                }

                var a = _evalStack.Pop().AsInt();
                _evalStack.Push(Value.FromInt(a % b));
                _ip++;
            }
            break;

            case OpCode.NEG:
            {
                var val = _evalStack.Pop();

                if (val.Kind == ValueKind.Double)
                {
                    _evalStack.Push(Value.FromDouble(-val.AsDouble()));
                } else
                {
                    _evalStack.Push(Value.FromInt(-val.AsInt()));
                }

                _ip++;
            }
            break;

            // ===========================
            // Сравнения
            // ===========================
            case OpCode.CMP_EQ:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromBool(a == b));
                _ip++;
            }
            break;
            case OpCode.CMP_NE:
            {
                var b = _evalStack.Pop().Raw;
                var a = _evalStack.Pop().Raw;
                _evalStack.Push(Value.FromBool(a != b));
                _ip++;
            }
            break;
            case OpCode.CMP_LT:
            {
                var b = _evalStack.Pop().AsInt();
                var a = _evalStack.Pop().AsInt();
                _evalStack.Push(Value.FromBool(a < b));
                _ip++;
            }
            break;
            case OpCode.CMP_GT:
            {
                var b = _evalStack.Pop().AsInt();
                var a = _evalStack.Pop().AsInt();
                _evalStack.Push(Value.FromBool(a > b));
                _ip++;
            }
            break;
            case OpCode.CMP_LE:
            {
                var b = _evalStack.Pop().AsInt();
                var a = _evalStack.Pop().AsInt();
                _evalStack.Push(Value.FromBool(a <= b));
                _ip++;
            }
            break;
            case OpCode.CMP_GE:
            {
                var b = _evalStack.Pop().AsInt();
                var a = _evalStack.Pop().AsInt();
                _evalStack.Push(Value.FromBool(a >= b));
                _ip++;
            }
            break;

            // ===========================
            // Логика
            // ===========================
            case OpCode.AND:
            {
                var b = _evalStack.Pop().AsBool();
                var a = _evalStack.Pop().AsBool();
                _evalStack.Push(Value.FromBool(a && b));
                _ip++;
            }
            break;
            case OpCode.OR:
            {
                var b = _evalStack.Pop().AsBool();
                var a = _evalStack.Pop().AsBool();
                _evalStack.Push(Value.FromBool(a || b));
                _ip++;
            }
            break;
            case OpCode.NOT:
            {
                var a = _evalStack.Pop().AsBool();
                _evalStack.Push(Value.FromBool(!a));
                _ip++;
            }
            break;

            // ===========================
            // Поток управления
            // ===========================
            case OpCode.JUMP:
                _ip = Convert.ToInt32(instr.Operands[0]);
                break;

            case OpCode.JUMP_IF_TRUE:
            {
                var cond = _evalStack.Pop();
                if (cond.AsBool())
                {
                    _ip = Convert.ToInt32(instr.Operands[0]);
                } else
                {
                    _ip++;
                }
            }
            break;

            case OpCode.JUMP_IF_FALSE:
            {
                var cond = _evalStack.Pop();
                if (!cond.AsBool())
                {
                    _ip = Convert.ToInt32(instr.Operands[0]);
                } else
                {
                    _ip++;
                }
            }
            break;

            case OpCode.CALL:
            {
                var funcId = Convert.ToInt32(instr.Operands[0]);
                var target = _program.Functions.FirstOrDefault(f => f.FunctionId == funcId);
                if (target == null)
                {
                    throw new InvalidOperationException($"Func ID {funcId} not found");
                }

                PushFrame(target, _ip + 1);
            }
            break;

            case OpCode.CALL_METHOD:
            {
                var methodId = Convert.ToInt32(instr.Operands[1]);
                var target = _program.Functions.FirstOrDefault(f => f.FunctionId == methodId);
                if (target == null)
                {
                    throw new InvalidOperationException($"Method ID {methodId} not found");
                }

                var argCount = target.ParameterTypes.Count;
                var frame = new StackFrame(target, _ip + 1);
                for (var i = argCount - 1; i >= 0; i--)
                {
                    frame.Locals[i] = _evalStack.Pop();
                }

                var receiver = _evalStack.Pop();
                CheckNull(receiver);

                _callStack.Push(frame);
                _currentFunc = target;
                _currentLocals = frame.Locals;
                _ip = 0;
            }
            break;

            case OpCode.RETURN:
                PopFrame();
                break;

            // ===========================
            // Объекты (Память)
            // ===========================
            case OpCode.NEW_OBJECT:
            {
                var classId = Convert.ToInt32(instr.Operands[0]);
                var cls = _program.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (cls == null)
                {
                    throw new InvalidOperationException($"Class ID {classId} not found");
                }

                // Размер данных: кол-во полей * 8 байт (Header обрабатывает RuntimeContext)
                var payloadSize = cls.Fields.Count * 8;

                // 1. Пробуем выделить. Если нет места -> GC.
                if (!_runtime.CanAllocate(payloadSize))
                {
                    _runtime.Collect(this);
                    if (!_runtime.CanAllocate(payloadSize))
                    {
                        throw new OutOfMemoryException("Heap full after GC");
                    }
                }

                var ptr = _runtime.AllocateObject(payloadSize, classId);
                _evalStack.Push(Value.FromObject(ptr));
                _ip++;
            }
            break;

            case OpCode.GET_FIELD:
            {
                var fieldIdx = Convert.ToInt32(instr.Operands[1]);
                var objRef = _evalStack.Pop();
                CheckNull(objRef);

                // Используем RuntimeContext для безопасного чтения (с учетом Header)
                var val = _runtime.ReadField(objRef.AsObject(), fieldIdx);
                _evalStack.Push(val);
                _ip++;
            }
            break;

            case OpCode.SET_FIELD:
            {
                var fieldIdx = Convert.ToInt32(instr.Operands[1]);
                var val = _evalStack.Pop();
                var objRef = _evalStack.Pop(); // Object is below value on stack
                CheckNull(objRef);

                _runtime.WriteField(objRef.AsObject(), fieldIdx, val);
                _ip++;
            }
            break;

            // ===========================
            // Массивы
            // ===========================
            case OpCode.NEW_ARRAY:
            {
                // [array_id] (тип массива) игнорируем для MVP аллокации, нам нужен размер со стека
                var length = _evalStack.Pop().AsInt();
                if (length < 0)
                {
                    throw new InvalidOperationException("Array size cannot be negative");
                }

                // Размер данных: длина * 8 байт
                var payloadSize = length * 8;

                if (!_runtime.CanAllocate(payloadSize))
                {
                    _runtime.Collect(this);
                    if (!_runtime.CanAllocate(payloadSize))
                    {
                        throw new OutOfMemoryException("Heap full after GC (Array)");
                    }
                }

                var ptr = _runtime.AllocateArray(length);
                _evalStack.Push(Value.FromObject(ptr));
                _ip++;
            }
            break;

            case OpCode.GET_ELEMENT:
            {
                var index = _evalStack.Pop().AsInt();
                var arrRef = _evalStack.Pop();
                CheckNull(arrRef);

                // Runtime сам проверит границы массива
                var val = _runtime.ReadArrayElement(arrRef.AsObject(), index);
                _evalStack.Push(val);
                _ip++;
            }
            break;

            case OpCode.SET_ELEMENT:
            {
                var val = _evalStack.Pop();
                var index = _evalStack.Pop().AsInt();
                var arrRef = _evalStack.Pop();
                CheckNull(arrRef);

                _runtime.WriteArrayElement(arrRef.AsObject(), index, val);
                _ip++;
            }
            break;

            case OpCode.CALL_NATIVE:
            {
                var nativeId = Convert.ToInt32(instr.Operands[0]);
                _runtime.InvokeNative(nativeId, this);
                _ip++;
            }
            break;
        }
    }

    private void PushFrame(BytecodeFunction func, int returnAddress)
    {
        var frame = new StackFrame(func, returnAddress);
        var argCount = func.ParameterTypes.Count;
        for (var i = argCount - 1; i >= 0; i--)
        {
            if (_evalStack.Count == 0)
            {
                throw new InvalidOperationException("Stack underflow on args");
            }

            frame.Locals[i] = _evalStack.Pop();
        }

        _callStack.Push(frame);
        _currentFunc = func;
        _currentLocals = frame.Locals;
        _ip = 0;
    }

    private void PopFrame()
    {
        var poppedFrame = _callStack.Pop();

        if (_callStack.Count > 0)
        {
            var parentFrame = _callStack.Peek();
            _currentFunc = parentFrame.Function;
            _currentLocals = parentFrame.Locals;
            _ip = poppedFrame.ReturnAddress;
        } else
        {
            _currentFunc = null;
            _currentLocals = null;
            _ip = 0;
        }
    }

    private static void CheckNull(Value refVal)
    {
        if (refVal.Kind == ValueKind.Null || (refVal.Kind == ValueKind.ObjectRef && refVal.Raw == 0))
        {
            throw new NullReferenceException("Null pointer exception");
        }
    }

    private Value ValueFromConst(object c)
    {
        return c switch
        {
            int i => Value.FromInt(i),
            long l => Value.FromInt((int)l),
            double d => Value.FromDouble(d),
            bool b => Value.FromBool(b),
            char ch => Value.FromChar(ch),
            string s => AllocateString(s),
            _ => throw new NotImplementedException($"Const type {c.GetType()} not supported")
        };
    }

    private Value AllocateString(string s)
    {
        var ptr = _runtime.AllocateArray(s.Length);

        for (int i = 0; i < s.Length; i++)
        {
            _runtime.WriteArrayElement(ptr, i, Value.FromChar(s[i]));
        }

        return Value.FromObject(ptr);
    }

    public IEnumerable<nint> EnumerateRoots()
    {
        foreach (var val in _evalStack)
        {
            if (val.Kind == ValueKind.ObjectRef && val.Raw != 0)
            {
                yield return (nint)val.Raw;
            }
        }

        foreach (var frame in _callStack)
        {
            foreach (var local in frame.Locals)
            {
                if (local.Kind == ValueKind.ObjectRef && local.Raw != 0)
                {
                    yield return (nint)local.Raw;
                }
            }
        }

        foreach (var global in _globals)
        {
            if (global.Kind == ValueKind.ObjectRef && global.Raw != 0)
            {
                yield return (nint)global.Raw;
            }
        }
    }
}
