using PHsystem.Core;
namespace PHsystem.Diagnostics;
public sealed class SnapshotStore : ISnapshotStore
{
    private DiagnosticSnapshot? current;
    public DiagnosticSnapshot? Current => Volatile.Read(ref current);
    public void Set(DiagnosticSnapshot snapshot) => Volatile.Write(ref current, snapshot);
    public void Clear() => Volatile.Write(ref current, null);
}
