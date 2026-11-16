using System.Collections.ObjectModel;
using PHsystem.Core;

namespace PHsystem.Desktop;

public sealed record SnapshotDifference(string Category, string Field, string Before, string After);

public static class SnapshotComparison
{
    public static IReadOnlyList<SnapshotDifference> Compare(DiagnosticSnapshot before, DiagnosticSnapshot after)
    {
        var results = new List<SnapshotDifference>();
        foreach (var category in before.Categories.Select(c => c.Category).Union(after.Categories.Select(c => c.Category)))
        {
            var old = before.Categories.FirstOrDefault(c => c.Category == category);
            var current = after.Categories.FirstOrDefault(c => c.Category == category);
            if (old?.State != current?.State) results.Add(new(category, "Estado da coleta", old?.State.ToString() ?? "Sem categoria", current?.State.ToString() ?? "Sem categoria"));
            foreach (var field in (old?.Facts?.Keys ?? []).Union(current?.Facts?.Keys ?? []))
            {
                var oldValue = old?.Facts?.GetValueOrDefault(field);
                var newValue = current?.Facts?.GetValueOrDefault(field);
                if (oldValue != newValue) results.Add(new(category, field, oldValue ?? "Sem dado", newValue ?? "Sem dado"));
            }
        }
        return results;
    }
}

public sealed partial class MainViewModel
{
    public ObservableCollection<CategoryCard> HardwareCategories { get; } = [];
    public ObservableCollection<CategoryCard> SecurityCategories { get; } = [];
    public ObservableCollection<CategoryCard> PerformanceCategories { get; } = [];
    public ObservableCollection<SnapshotDifference> HistoryDifferences { get; } = [];
    public HistoryLine? CompareBefore { get; set; }
    public HistoryLine? CompareAfter { get; set; }
    private string comparisonStatus = "Selecione duas análises para comparar. O diagnóstico atual será preservado.";
    public string ComparisonStatus { get => comparisonStatus; private set { comparisonStatus = value; Changed(); } }
    public UiCommand CompareHistoryCommand => compareHistoryCommand ??= new(() => _ = RunAsync(async token =>
    {
        if (CompareBefore is null || CompareAfter is null || CompareBefore.ScanId == CompareAfter.ScanId)
        { ComparisonStatus = "Selecione duas análises diferentes."; return; }
        var history = await repository.LoadHistoryAsync(100, token);
        var before = history.FirstOrDefault(s => s.ScanId.ToString() == CompareBefore.ScanId);
        var after = history.FirstOrDefault(s => s.ScanId.ToString() == CompareAfter.ScanId);
        if (before is null || after is null) { ComparisonStatus = "Uma análise não está mais disponível na retenção local."; return; }
        HistoryDifferences.Clear();
        foreach (var difference in SnapshotComparison.Compare(before, after)) HistoryDifferences.Add(difference);
        ComparisonStatus = HistoryDifferences.Count == 0 ? "Nenhuma diferença nos dados coletados." : $"{HistoryDifferences.Count} diferenças. Ausência de dado não significa melhora ou piora.";
        var oldHealth=PHsystem.Core.HealthAssessmentEngine.Evaluate(before,before.CapturedAt);var newHealth=PHsystem.Core.HealthAssessmentEngine.Evaluate(after,after.CapturedAt);
        ComparisonStatus += $" Recalculado com {oldHealth.RuleVersion}: antes {oldHealth.LowerBound}–{oldHealth.UpperBound}, cobertura {oldHealth.Coverage}%; depois {newHealth.LowerBound}–{newHealth.UpperBound}, cobertura {newHealth.Coverage}%. Mudança de cobertura não prova reparo.";
    }), () => !busy);
    private UiCommand? compareHistoryCommand;

    private void UpdateResultPages()
    {
        void Populate(ObservableCollection<CategoryCard> target, params string[] names)
        { target.Clear(); foreach (var category in Categories.Where(c => names.Contains(c.Name, StringComparer.OrdinalIgnoreCase))) target.Add(category); }
        Populate(HardwareCategories, "Processador", "GPU", "Memória", "Armazenamento", "Controladores", "Saúde das unidades", "Placa-mãe");
        Populate(SecurityCategories, "Segurança", "Proteção Windows", "Firewall", "Atualizações instaladas", "Eventos críticos", "Dispositivos com erro", "Sistema");
        Populate(PerformanceCategories, "Memória", "Inicialização", "Serviços", "Rede");
    }
}
