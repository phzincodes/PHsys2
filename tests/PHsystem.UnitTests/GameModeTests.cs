using PHsystem.Optimizer;
using Xunit;

namespace PHsystem.UnitTests;

public sealed class GameModeTests
{
    [Fact]
    public void TemporaryPriorityIsRestoredWhenSessionIsDisposed()
    {
        var process = new FakeProcess();
        using (var session = new GameModeSession())
        {
            session.Start(process);
            Assert.Equal(GamePriority.AboveNormal, process.Priority);
        }
        Assert.Equal(GamePriority.Normal, process.Priority);
        Assert.True(process.Disposed);
    }
    [Theory]
    [InlineData(GamePriority.High)]
    [InlineData(GamePriority.Realtime)]
    public void ElevatedPrioritiesAreNeverApplied(GamePriority priority)
    {
        using var session = new GameModeSession();
        var process = new FakeProcess { Priority = priority };
        Assert.Throws<InvalidOperationException>(() => session.Start(process));
        Assert.True(process.Disposed);
        Assert.Equal(priority, process.Priority);
    }
    private sealed class FakeProcess : IGameProcess
    {
        public int Id => 123;
        public string Name => "simulator";
        public bool HasExited => false;
        public bool Disposed { get; private set; }
        public GamePriority Priority { get; set; } = GamePriority.Normal;
        public void Dispose() => Disposed = true;
    }
}
