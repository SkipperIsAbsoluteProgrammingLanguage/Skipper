namespace Skipper.BaitCode.IdManager;

/// <summary>
/// Управление слотами для локальных переменных и параметров
/// </summary>

public sealed class LocalSlotManager
{
    private readonly Stack<Dictionary<string, int>> _scopes = new();
    private int _nextSlot = 0;

    public void EnterScope()
    {
        _scopes.Push(new Dictionary<string, int>());
    }

    public void ExitScope()
    {
        _scopes.Pop();
    }

    public int Declare(string name)
    {
        var scope = _scopes.Peek();

        if (scope.ContainsKey(name))
            throw new InvalidOperationException($"Variable '{name}' already declared in this scope");

        var slot = _nextSlot++;
        scope[name] = slot;
        return slot;
    }

    public int Resolve(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out var slot))
                return slot;
        }

        throw new InvalidOperationException($"Variable '{name}' not found");
    }

    public void Reset()
    {
        _scopes.Clear();
        _nextSlot = 0;
    }
}