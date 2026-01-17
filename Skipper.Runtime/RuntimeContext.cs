using Skipper.Runtime.Abstractions;
using Skipper.Runtime.GC;
using Skipper.Runtime.Memory;
using Skipper.Runtime.Objects;
using Skipper.Runtime.Values;
using System.Diagnostics;

namespace Skipper.Runtime;

public sealed class RuntimeContext
{
    private readonly Heap _heap;
    private readonly IGarbageCollector _gc;

    // Размер заголовка 
    // Размер заголовка объекта/массива в байтах (метаданные или длина)
    private const int HeaderSize = sizeof(long);

    // Размер одного слота значения (8 байт)
    private const int SlotSize = sizeof(long);

    private readonly Dictionary<int, Action<IVirtualMachine>> _nativeFunctions = new();
    private readonly long _startTime;

    public RuntimeContext(long heapBytes = 1024 * 1024)
    {
        _heap = new Heap(Math.Max(heapBytes, 1024 * 1024));
        _gc = new MarkSweepGc(_heap);
        _startTime = Stopwatch.GetTimestamp();

        RegisterNatives();
    }

    private void RegisterNatives()
    {
        // ID 0: print(any) -> void
        _nativeFunctions[0] = (vm) => {
            var val = vm.PopStack();

            if (val.Kind == ValueKind.ObjectRef && val.Raw != 0)
            {
                try
                {
                    var str = ReadStringFromMemory(val.AsObject());
                    Console.WriteLine(str);
                } catch
                {
                    Console.WriteLine(val.ToString());
                }
            } else
            {
                Console.WriteLine(val.ToString());
            }

            vm.PushStack(Value.Null());
        };

        // ID 1: time() -> int (milliseconds)
        _nativeFunctions[1] = (vm) => {
            // Возвращаем время в миллисекундах от старта VM
            var elapsed = (Stopwatch.GetTimestamp() - _startTime) / (double)Stopwatch.Frequency * 1000.0;
            vm.PushStack(Value.FromInt((int)elapsed));
        };

        // ID 2: random(max) -> int
        _nativeFunctions[2] = (vm) => {
            var max = vm.PopStack().AsInt();
            var rnd = Random.Shared.Next(max);
            vm.PushStack(Value.FromInt(rnd));
        };
    }

    public void InvokeNative(int id, IVirtualMachine vm)
    {
        if (_nativeFunctions.TryGetValue(id, out var action))
        {
            action(vm);
        } else
        {
            throw new InvalidOperationException($"Native function ID {id} not found");
        }
    }

    public string ReadStringFromMemory(nint ptr)
    {
        var len = GetArrayLength(ptr);
        var chars = new char[len];
        for (var i = 0; i < len; i++)
        {
            var charVal = ReadArrayElement(ptr, i);
            chars[i] = (char)charVal.Raw;
        }
        return new string(chars);
    }

    public nint AllocateString(string s)
    {
        var ptr = AllocateArray(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            WriteArrayElement(ptr, i, Value.FromChar(s[i]));
        }
        return ptr;
    }

    public nint ConcatStrings(nint ptr1, nint ptr2)
    {
        var s1 = ReadStringFromMemory(ptr1);
        var s2 = ReadStringFromMemory(ptr2);

        return AllocateString(s1 + s2);
    }

    // --- Управление памятью ---

    public bool CanAllocate(int bytes)
    {
        return _heap.HasSpace(bytes + HeaderSize);
    }

    public void Collect(IRootProvider roots)
    {
        _gc.Collect(roots);
    }

    // --- Аллокация ---

    public nint AllocateObject(int payloadSize, int classId)
    {
        ObjectDescriptor desc = new(ObjectKind.Class, []);

        // Выделяем память: Заголовок + Поля
        var ptr = _heap.Allocate(desc, HeaderSize + payloadSize);

        // В заголовок записываем ClassId
        _heap.WriteInt64(ptr, 0, classId);

        return ptr;
    }

    public nint AllocateArray(int length)
    {
        var totalSize = HeaderSize + length * SlotSize;
        var desc = new ObjectDescriptor(ObjectKind.Array, []);

        var ptr = _heap.Allocate(desc, totalSize);

        // В заголовок записываем длину массива
        _heap.WriteInt64(ptr, 0, length);

        return ptr;
    }

    // --- Доступ к полям объектов ---

    public Value ReadField(nint objPtr, int fieldIndex)
    {
        var offset = HeaderSize + fieldIndex * SlotSize;
        var raw = _heap.ReadInt64(objPtr, offset);
        return new Value(raw);
    }

    public void WriteField(nint objPtr, int fieldIndex, Value val)
    {
        var offset = HeaderSize + fieldIndex * SlotSize;
        _heap.WriteInt64(objPtr, offset, val.Raw);
    }

    // --- Доступ к массивам ---

    public int GetArrayLength(nint arrPtr)
    {
        return (int)_heap.ReadInt64(arrPtr, 0);
    }

    public Value ReadArrayElement(nint arrPtr, int index)
    {
        var length = GetArrayLength(arrPtr);

        if (index < 0 || index >= length)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of bounds (Length: {length})");
        }

        var offset = HeaderSize + index * SlotSize;
        var raw = _heap.ReadInt64(arrPtr, offset);
        return new Value(raw);
    }

    public void WriteArrayElement(nint arrPtr, int index, Value val)
    {
        var length = GetArrayLength(arrPtr);

        if (index < 0 || index >= length)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of bounds (Length: {length})");
        }

        var offset = HeaderSize + index * SlotSize;
        _heap.WriteInt64(arrPtr, offset, val.Raw);
    }

    public int GetAliveObjectCount() => _heap.Objects.Count;
}
