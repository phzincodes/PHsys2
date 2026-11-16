using System.Diagnostics;
using PHsystem.Optimizer;

namespace PHsystem.Windows;

public sealed record GameProcessChoice(int Id, string Name);
public sealed class GameProcess : IGameProcess
{
    private readonly Process process;
    private static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
        { "System", "Registry", "csrss", "wininit", "lsass", "services", "svchost", "smss", "winlogon", "dwm", "Memory Compression", "PHsystem.Desktop" };
    public int Id => process.Id;
    public string Name => process.ProcessName;
    public bool HasExited => process.HasExited;
    public GamePriority Priority
    {
        get => process.PriorityClass switch
        {
            ProcessPriorityClass.Idle => GamePriority.Idle, ProcessPriorityClass.BelowNormal => GamePriority.BelowNormal,
            ProcessPriorityClass.Normal => GamePriority.Normal, ProcessPriorityClass.AboveNormal => GamePriority.AboveNormal,
            ProcessPriorityClass.High => GamePriority.High, _ => GamePriority.Realtime
        };
        set => process.PriorityClass = value switch
        {
            GamePriority.Idle => ProcessPriorityClass.Idle, GamePriority.BelowNormal => ProcessPriorityClass.BelowNormal,
            GamePriority.Normal => ProcessPriorityClass.Normal, GamePriority.AboveNormal => ProcessPriorityClass.AboveNormal,
            _ => throw new InvalidOperationException("Prioridade fora da política.")
        };
    }
    public GameProcess(int id)
    {
        process = Process.GetProcessById(id);
        try
        {
            using var current = Process.GetCurrentProcess();
            if (process.Id == current.Id || process.SessionId != current.SessionId || Excluded.Contains(process.ProcessName))
                throw new InvalidOperationException("Processo fora do escopo permitido.");
            _ = process.SafeHandle; // Retain process identity against PID reuse.
        }
        catch { process.Dispose(); throw; }
    }
    public static IReadOnlyList<GameProcessChoice> List()
    {
        using var current = Process.GetCurrentProcess();
        var result = new List<GameProcessChoice>();
        foreach (var process in Process.GetProcesses())
            using (process)
                try
                {
                    if (process.Id != current.Id && process.SessionId == current.SessionId && !Excluded.Contains(process.ProcessName))
                        result.Add(new(process.Id, process.ProcessName));
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
        return result.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
    public void Dispose() => process.Dispose();
}
