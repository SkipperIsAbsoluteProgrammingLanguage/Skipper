using Skipper.BaitCode.IdManager;
using Skipper.BaitCode.Objects;
using Skipper.BaitCode.Types;
using Xunit;

namespace Skipper.BaitCode.Tests;

public class LocalSlotManagerTests
{
    [Fact]
    public void Declare_ThrowsOnDuplicateInSameScope()
    {
        // Arrange
        var func = new BytecodeFunction(0, "main", new PrimitiveType("void"), []);
        var locals = new LocalSlotManager(func);
        locals.EnterScope();

        // Act
        locals.Declare("x", new PrimitiveType("int"));

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => locals.Declare("x", new PrimitiveType("int")));
        Assert.Contains("already declared", ex.Message);
    }

    [Fact]
    public void TryResolve_ReturnsFalseWhenMissing()
    {
        // Arrange
        var func = new BytecodeFunction(0, "main", new PrimitiveType("void"), []);
        var locals = new LocalSlotManager(func);
        locals.EnterScope();

        // Act
        var found = locals.TryResolve("missing", out var slot);

        // Assert
        Assert.False(found);
        Assert.Equal(-1, slot);
    }
}
