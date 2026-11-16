using System.Globalization;
using PHsystem.Core;

namespace PHsystem.Optimizer;

public sealed record Recommendation(string Title, string Detail, string Evidence = "Sem evidência específica para intervenção.",
    string Source = "https://support.microsoft.com/en-us/windows/experience/performance-optimization/tips-to-improve-pc-performance-in-windows")
{
    public string RuleVersion => "2026.11.1";
    public string ReviewedOn => "2026-11-15";
    public string Risk => "Orientação informativa. Encerrar aplicativos pode perder trabalho; exclusões podem ser permanentes. Nenhuma mudança automática.";
    public bool RequiresRefresh { get; init; }
}

public sealed class RecommendationService
{
    public IReadOnlyList<Recommendation> Evaluate(DiagnosticSnapshot? snapshot)
    {
        if (snapshot is null) return [new("Execute um diagnóstico", "As recomendações dependem de evidências locais. Nenhuma alteração é aplicada automaticamente.")];
        var result = new List<Recommendation>();
        foreach (var category in snapshot.Categories)
        {
            if (category.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                result.Add(new Recommendation($"Atualize a coleta de {category.Category}", "Evidência expirada; execute uma nova varredura antes de decidir.") { RequiresRefresh = true });
                continue;
            }
            if (category.State is CollectionState.Error or CollectionState.Unavailable || category.Facts is null) continue;
            if (category.Category == "Memória" && category.Facts.TryGetValue("UsoPercentual", out var memory)
                && double.TryParse(memory, CultureInfo.InvariantCulture, out var load) && load >= 85)
                result.Add(new("Reveja o consumo de memória", "Uso observado acima de 85%. Verifique seus aplicativos no Gerenciador de Tarefas antes de encerrar processos. Uma leitura alta isolada não comprova gargalo.", $"Uso observado: {load}% · fonte: {category.Source}"));
            if (category.Category != "Armazenamento") continue;
            foreach (var (key, value) in category.Facts.Where(f => f.Key.EndsWith(" TotalBytes", StringComparison.Ordinal)))
            {
                if (!long.TryParse(value, CultureInfo.InvariantCulture, out var total) || total <= 0) continue;
                var freeKey = key.Replace(" TotalBytes", " FreeBytes", StringComparison.Ordinal);
                if (category.Facts.TryGetValue(freeKey, out var freeValue) && long.TryParse(freeValue, CultureInfo.InvariantCulture, out var free)
                    && free >= 0 && (double)free / total < .1)
                    result.Add(new("Pouco espaço disponível", $"{key.Replace(" TotalBytes", "", StringComparison.Ordinal)} tem menos de 10% livres. Revise arquivos pessoais e a prévia de temporários.",
                        $"{free} bytes disponíveis de {total} bytes · fonte: {category.Source}",
                        "https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/free-up-drive-space-in-windows"));
            }
        }
        if (result.Count == 0) result.Add(new("Nenhuma recomendação pelas regras atuais", "Isso não é um atestado de saúde. As regras verificam apenas memória e espaço disponível; score permanece indisponível."));
        return result;
    }
}
