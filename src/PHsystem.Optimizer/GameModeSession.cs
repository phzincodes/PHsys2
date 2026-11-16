namespace PHsystem.Optimizer;

public enum GamePriority { Idle, BelowNormal, Normal, AboveNormal, High, Realtime }
public interface IGameProcess : IDisposable
{
    int Id { get; }
    string Name { get; }
    bool HasExited { get; }
    GamePriority Priority { get; set; }
}

/// <summary>Limited priority profile. Never applies High/Realtime or changes affinity/security settings.</summary>
public sealed class GameModeSession : IDisposable
{
    private readonly object gate = new();
    private IGameProcess? process;
    private GamePriority original;
    private Timer? timer;
    public string Status { get; private set; } = "Nenhum perfil temporário ativo.";
    public event EventHandler? Changed;

    public void Start(IGameProcess target)
    {
        lock (gate)
        {
            if (process is not null) { target.Dispose(); throw new InvalidOperationException("Encerre o perfil atual antes de iniciar outro."); }
            try
            {
                if (target.HasExited || target.Priority is GamePriority.High or GamePriority.Realtime or GamePriority.AboveNormal)
                    throw new InvalidOperationException("Processo indisponível ou prioridade incompatível com o perfil limitado.");
                original = target.Priority;
                target.Priority = GamePriority.AboveNormal;
                process = target;
                Status = $"Perfil temporário em {target.Name} (PID {target.Id}). Prioridade AboveNormal; duração máxima 30 minutos. Nenhum ganho de FPS é presumido.";
                timer = new Timer(_ => Check(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            }
            catch { target.Dispose(); throw; }
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private DateTimeOffset expiresAt;
    private void Check()
    {
        lock (gate)
        {
            if (process is null) return;
            try { if (!process.HasExited && DateTimeOffset.UtcNow < expiresAt) return; }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
        }
        Stop();
    }

    public void Stop()
    {
        lock (gate)
        {
            timer?.Dispose(); timer = null;
            if (process is null) return;
            try
            {
                if (!process.HasExited) process.Priority = original;
                Status = "Perfil encerrado; prioridade anterior restaurada quando o processo permaneceu acessível.";
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            { Status = "Não foi possível restaurar a prioridade. Verifique o processo no Gerenciador de Tarefas."; }
            finally { process.Dispose(); process = null; }
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }
    public void Dispose() => Stop();
}
