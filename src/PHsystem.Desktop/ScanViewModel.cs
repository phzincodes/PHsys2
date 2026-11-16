using System.Collections.ObjectModel;
using PHsystem.Diagnostics;

namespace PHsystem.Desktop;

public sealed class CollectorLine(string name) : ObservableObject
{
    private string state = "Na fila";
    private bool finished;
    public string Name => name;
    public string State { get => state; set { state = value; Changed(); } }
    public bool Finished { get => finished; set { finished = value; Changed(); } }
}

public sealed partial class MainViewModel
{
    private bool scanning;
    private int scanGeneration;
    private string scanState = "Pronto para analisar";
    public bool IsScanning { get => scanning; private set { scanning = value; Changed(); } }
    public string ScanState { get => scanState; private set { scanState = value; Changed(); } }
    public ObservableCollection<CollectorLine> ScanCollectors { get; } = [];
    public int CompletedCollectors => ScanCollectors.Count(c => c.Finished);
    public int TotalCollectors => ScanCollectors.Count;
    public string ScanProgressLabel => $"{CompletedCollectors} de {TotalCollectors} coletores concluídos";

    private async Task RunScanAsync(CancellationToken token)
    {
        var generation = ++scanGeneration;
        IsScanning = true; ScanState = "Analisando";
        ScanCollectors.Clear();
        foreach (var name in scans.Categories) ScanCollectors.Add(new(name));
        void NotifyProgress() { Changed(nameof(CompletedCollectors)); Changed(nameof(TotalCollectors)); Changed(nameof(ScanProgressLabel)); }
        NotifyProgress();
        var progress = new Progress<CollectorProgress>(update =>
        {
            if (generation != scanGeneration || !IsScanning) return;
            var line = ScanCollectors.FirstOrDefault(c => c.Name == update.Category);
            if (line is null) return;
            line.State = update.State; line.Finished = update.Finished;
            NotifyProgress();
        });
        try
        {
            Status = "Coletando informações locais…";
            await Task.Run(() => scans.ScanAsync(true, token, progress), token);
            await RefreshAsync(token);
            foreach (var line in ScanCollectors) line.Finished = true;
            NotifyProgress();
            ScanState = "Análise concluída";
            Status = "Análise salva. Consulte a cobertura e os limites nos resultados.";
            SelectedPageIndex = 1;
        }
        catch (OperationCanceledException) { ScanState = "Análise interrompida"; throw; }
        catch { ScanState = "Erro na análise"; throw; }
        finally { IsScanning = false; }
    }
}
