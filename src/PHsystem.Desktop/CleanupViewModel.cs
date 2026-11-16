using System.Collections.ObjectModel;
using PHsystem.Cleanup;

namespace PHsystem.Desktop;

public enum CleanupScreenState { NotAnalyzed, Analyzing, Cancelled, NoEligibleItems, Partial, Failed, ReadyForReview, Executing, Completed, CompletedWithWarnings }
public sealed class CleanupSelection(CleanupCandidate candidate, Action changed) : ObservableObject
{
    private bool selected = true;
    public string Name => candidate.Name;
    public long Bytes => candidate.Bytes;
    public DateTime LastWriteUtc => candidate.LastWriteUtc;
    public bool Selected { get => selected; set { selected = value; Changed(); changed(); } }
}

public sealed partial class MainViewModel
{
    private CleanupScreenState cleanupState;
    private CleanupAnalysis? cleanupAnalysis;
    public CleanupScreenState CleanupState { get => cleanupState; private set { cleanupState = value; Changed(); NotifyCleanup(); } }
    public string CleanupStateLabel => CleanupState switch
    {
        CleanupScreenState.Analyzing => "Analisando os temporários", CleanupScreenState.Cancelled => "Operação interrompida",
        CleanupScreenState.NoEligibleItems => "Nenhum arquivo elegível", CleanupScreenState.Partial => "Análise parcial",
        CleanupScreenState.Failed => "Não foi possível concluir", CleanupScreenState.ReadyForReview => "Pronto para revisão",
        CleanupScreenState.Executing => "Excluindo a seleção", CleanupScreenState.Completed => "Operação concluída",
        CleanupScreenState.CompletedWithWarnings => "Concluído com ressalvas", _ => "Ainda não analisado"
    };
    public string CleanupEmptyMessage => CleanupState switch
    {
        CleanupScreenState.NoEligibleItems => "A análise terminou. Nenhum arquivo atendeu à política. Veja abaixo os motivos; zero elegíveis não significa falha nem que todo o disco está limpo.",
        CleanupScreenState.Analyzing => "Examinando apenas o primeiro nível da pasta temporária…",
        CleanupScreenState.Partial => "O escopo não pôde ser examinado completamente. Revise os limites e erros antes de decidir.",
        CleanupScreenState.Cancelled => "A operação foi interrompida. Uma análise cancelada não autoriza exclusão.",
        CleanupScreenState.Failed => "Nenhuma nova prévia foi autorizada. Verifique a permissão de acesso e tente novamente.",
        CleanupScreenState.Completed or CleanupScreenState.CompletedWithWarnings => "Veja o resultado por arquivo abaixo. Uma nova análise verifica novamente o escopo.",
        _ => "Analise os temporários para conhecer o escopo e os motivos de exclusão. Nenhum arquivo será apagado durante a análise."
    };
    public bool HasCleanupFiles => CleanupFiles.Count > 0;
    public bool ShowCleanupEmpty => !HasCleanupFiles;
    public bool CanReviewCleanup => cleanupPlan is { Files.Length: > 0 } && CleanupState is CleanupScreenState.ReadyForReview or CleanupScreenState.Partial;
    public string CleanupRoot => cleanup.ScanRoot;
    public string CleanupSelectionLabel => $"{CleanupFiles.Count(f => f.Selected)} selecionados · {DisplayFormat.Bytes(CleanupFiles.Where(f => f.Selected).Sum(f => f.Bytes))}";
    public ObservableCollection<FactLine> CleanupAnalysisLines { get; } = [];
    public ObservableCollection<CleanupFileResult> CleanupResults { get; } = [];
    public bool HasCleanupResults => CleanupResults.Count > 0;

    private void NotifyCleanup()
    {
        foreach (var property in new[] { nameof(CleanupSize), nameof(CleanupCount), nameof(CleanupStateLabel), nameof(CleanupEmptyMessage), nameof(HasCleanupFiles), nameof(ShowCleanupEmpty), nameof(CanReviewCleanup), nameof(CleanupSelectionLabel), nameof(HasCleanupResults) }) Changed(property);
        CleanupCommand?.Refresh();
    }
    private async Task AnalyzeCleanupAsync(CancellationToken token)
    {
        cleanupPlan = null; CleanupFiles.Clear(); CleanupResults.Clear(); CleanupAnalysisLines.Clear(); CleanupConsent = false;
        CleanupState = CleanupScreenState.Analyzing; CleanupStatus = "Somente leitura; nada será apagado.";
        try
        {
            cleanupPlan = await cleanup.PreviewAsync(token); cleanupAnalysis = cleanupPlan.Analysis;
            foreach (var file in cleanupPlan.Files) CleanupFiles.Add(new(file, NotifyCleanup));
            if (cleanupAnalysis is { } analysis)
            {
                CleanupAnalysisLines.Add(new("Diretórios examinados", analysis.DirectoriesExamined.ToString()));
                CleanupAnalysisLines.Add(new("Subpastas fora do escopo", $"{analysis.DirectoriesExcluded} (incluindo {analysis.DirectoryLinksExcluded} links)"));
                CleanupAnalysisLines.Add(new("Arquivos considerados", analysis.FilesConsidered.ToString()));
                CleanupAnalysisLines.Add(new("Elegíveis / ignorados", $"{analysis.Eligible} / {analysis.Ignored}"));
                string Label(CleanupSkipReason reason) => reason switch { CleanupSkipReason.Recent => "Recentes (até 7 dias)", CleanupSkipReason.Protected => "Protegidos", CleanupSkipReason.InUse => "Em uso", CleanupSkipReason.PermissionDenied => "Permissão negada", CleanupSkipReason.ReparsePoint => "Links / reparse points", CleanupSkipReason.HardLink => "Hard links", CleanupSkipReason.ReadError => "Erro de leitura", _ => "Fora da política" };
                foreach (var reason in Enum.GetValues<CleanupSkipReason>()) CleanupAnalysisLines.Add(new(Label(reason), analysis.Count(reason).ToString()));
                CleanupAnalysisLines.Add(new("Entradas sem identificação", $"{analysis.UnidentifiedEntries} (acesso: {analysis.EntryPermissionErrors}; leitura: {analysis.EntryReadErrors})"));
                CleanupAnalysisLines.Add(new("Limite / erros de enumeração", $"{(analysis.LimitReached ? "10.000 entradas atingidas" : "Limite não atingido")} / {analysis.EnumerationErrors}"));
            }
            CleanupState = cleanupAnalysis?.IsPartial == true ? CleanupScreenState.Partial : cleanupPlan.Files.Length == 0 ? CleanupScreenState.NoEligibleItems : CleanupScreenState.ReadyForReview;
            CleanupStatus = cleanupPlan.Files.Length == 0 ? "Nada foi excluído. A regra mantém arquivos recentes, protegidos e fora do escopo." : "Revise e selecione os arquivos. A exclusão permanente exige consentimento e uma prévia de até 10 minutos.";
            Status = CleanupStateLabel;
        }
        catch (OperationCanceledException) { cleanupPlan = null; CleanupState = CleanupScreenState.Cancelled; CleanupStatus = "Análise cancelada; nada foi excluído."; throw; }
        catch { cleanupPlan = null; CleanupState = CleanupScreenState.Failed; CleanupStatus = "A prévia falhou; nenhuma exclusão foi autorizada."; throw; }
        finally { NotifyCleanup(); }
    }
    private async Task ExecuteCleanupAsync(CancellationToken token)
    {
        var plan = cleanupPlan!; var selection = CleanupFiles.Where(f => f.Selected).Select(f => f.Name).ToArray();
        cleanupPlan = null; CleanupConsent = false; CleanupState = CleanupScreenState.Executing;
        try
        {
            var outcome = await cleanup.ExecuteAsync(plan.Id, token, selection);
            CleanupFiles.Clear(); CleanupResults.Clear();
            foreach (var item in outcome.Items) CleanupResults.Add(item);
            CleanupState = outcome.Cancelled ? CleanupScreenState.Cancelled : outcome.Skipped > 0 ? CleanupScreenState.CompletedWithWarnings : CleanupScreenState.Completed;
            var delta = outcome.ObservedFreeSpaceChange is { } bytes ? $"{(bytes < 0 ? "−" : "+")}{DisplayFormat.Bytes(Math.Abs(bytes))}" : "indisponível";
            CleanupStatus = $"{outcome.Deleted} excluídos; {outcome.Skipped} preservados. Tamanho nominal removido: {DisplayFormat.Bytes(outcome.FreedBytes)}. Variação observada de espaço livre: {delta}; outras atividades do Windows também influenciam essa medida.";
            Status = CleanupStateLabel;
        }
        catch (OperationCanceledException) { CleanupState = CleanupScreenState.Cancelled; CleanupStatus = "Operação interrompida. Ações confirmadas não são desfeitas automaticamente."; throw; }
        catch { CleanupState = CleanupScreenState.Failed; CleanupStatus = "Operação não concluída. Faça nova análise para verificar o estado atual."; throw; }
        finally { NotifyCleanup(); }
    }
}
