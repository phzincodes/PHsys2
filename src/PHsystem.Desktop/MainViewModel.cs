using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using PHsystem.Cleanup;
using PHsystem.Community;
using PHsystem.Core;
using PHsystem.Diagnostics;
using PHsystem.Drivers;
using PHsystem.Optimizer;
using PHsystem.Windows;

namespace PHsystem.Desktop;

public sealed record FactLine(string Name, string Value);
public sealed record CategoryCard(string Name, string State, string Source, string ExpiresAt, IReadOnlyList<FactLine> Facts, string Errors)
{
    public string Detail => Facts.Count == 0 ? "Sem dados disponíveis" : $"{Facts.Count} informações coletadas";
    public bool HasErrors => !string.IsNullOrWhiteSpace(Errors);
}
public sealed record HistoryLine(string CapturedAt, string Coverage, string ScanId)
{
    public string ShortId => ScanId.Length > 8 ? ScanId[..8] : ScanId;
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly SnapshotStore snapshots;
    private readonly ScanCoordinator scans;
    private readonly ISnapshotRepository repository;
    private readonly TempCleanupService cleanup;
    private readonly RecommendationService recommendations;
    private readonly DriverInventory drivers;
    private readonly GameModeSession gameMode;
    private GameProcessChoice? selectedProcess;
    private string? storageDirectory;
    private bool storageConsent;
    private CancellationTokenSource? cancellation;
    private CleanupPlan? cleanupPlan;
    private bool busy;
    private bool cleanupConsent;
    private string status = "Pronto. Dados permanecem neste computador.";
    private string cleanupStatus = "Prévia: apenas arquivos no primeiro nível da pasta temporária do usuário, sem alteração há mais de 7 dias. Limite: 10.000 arquivos por prévia.";
    private string communityText = "Prepare um resumo sem detalhes de hardware. Nada é enviado automaticamente.";
    public ObservableCollection<CategoryCard> Categories { get; } = [];
    public ObservableCollection<HistoryLine> History { get; } = [];
    public ObservableCollection<CleanupSelection> CleanupFiles { get; } = [];
    public ObservableCollection<Recommendation> Recommendations { get; } = [];
    public ObservableCollection<DriverInfo> Drivers { get; } = [];
    public ObservableCollection<DiagnosticFinding> Findings { get; } = [];
    public ObservableCollection<GameProcessChoice> GameProcesses { get; } = [];
    public GameProcessChoice? SelectedProcess { get => selectedProcess; set { selectedProcess = value; Changed(); StartGameCommand.Refresh(); } }
    public string GameStatus => gameMode.Status;
    public System.Windows.Media.ImageSource? Logo { get; }
    public CommunityViewModel Community { get; }
    public UiPreferences Preferences { get; }
    public ObservableCollection<StorageFile> StorageFiles { get; } = [];
    public ObservableCollection<string> DuplicateFiles { get; } = [];
    public string StorageDirectory => storageDirectory ?? "Nenhuma pasta selecionada.";
    public bool StorageConsent { get => storageConsent; set { storageConsent = value; Changed(); AnalyzeStorageCommand.Refresh(); } }
    public bool CompareHashes { get; set; }
    public UiCommand SelectStorageDirectoryCommand { get; }
    public UiCommand AnalyzeStorageCommand { get; }
    public UiCommand RefreshProcessesCommand { get; }
    public UiCommand StartGameCommand { get; }
    public UiCommand StopGameCommand { get; }
    public UiCommand ScanCommand { get; }
    public UiCommand CancelCommand { get; }
    public UiCommand PreviewCleanupCommand { get; }
    public UiCommand CleanupCommand { get; }
    public UiCommand DeleteHistoryCommand { get; }
    public UiCommand ExportCommand { get; }
    public UiCommand PrepareDraftCommand { get; }
    public UiCommand ExportDraftCommand { get; }
    public string Status { get => status; private set { status = value; Changed(); } }
    public string CleanupStatus { get => cleanupStatus; private set { cleanupStatus = value; Changed(); } }
    public string CommunityText { get => communityText; private set { communityText = value; Changed(); } }
    public bool CleanupConsent { get => cleanupConsent; set { cleanupConsent = value; Changed(); CleanupCommand.Refresh(); } }
    public string SnapshotStatus => snapshots.Current is { } snapshot ? $"Coleta local: {snapshot.CapturedAt.ToLocalTime():g}" : "Nenhuma varredura realizada";
    public string Coverage => snapshots.Current is { } snapshot ? $"Categorias completas: {snapshot.Coverage:P0}. Coletas parciais aparecem abaixo." : "Cobertura indisponível";

    public MainViewModel(SnapshotStore snapshots, ScanCoordinator scans, ISnapshotRepository repository,
        TempCleanupService cleanup, RecommendationService recommendations, DriverInventory drivers, GameModeSession gameMode,
        IAssetProvider? assets = null, CommunityViewModel? community = null, UiPreferences? preferences = null)
    {
        this.snapshots = snapshots;
        this.scans = scans;
        this.repository = repository;
        this.cleanup = cleanup;
        this.recommendations = recommendations;
        this.drivers = drivers;
        this.gameMode = gameMode;
        Logo = AssetImageLoader.LoadLogo(assets);
        Community = community ?? new CommunityViewModel(snapshots);
        Preferences = preferences ?? new UiPreferences();
        Preferences.Updated += (_, _) => UpdateDashboardWidgets();
        InitializeShell();
        SelectStorageDirectoryCommand = new(() =>
        {
            var dialog = new OpenFolderDialog { Title = "Selecione a única pasta que deseja analisar" };
            if (dialog.ShowDialog() != true) return;
            storageDirectory = dialog.FolderName; StorageConsent = false;
            StorageFiles.Clear(); DuplicateFiles.Clear(); Changed(nameof(StorageDirectory));
        }, () => !busy);
        AnalyzeStorageCommand = new(() => _ = RunAsync(async token =>
        {
            Status = "Analisando somente a pasta escolhida. Nenhum arquivo será excluído.";
            var analysis = await new StorageAnalyzer().AnalyzeAsync(storageDirectory!, CompareHashes, token);
            StorageFiles.Clear(); DuplicateFiles.Clear();
            foreach (var file in analysis.LargestFiles) StorageFiles.Add(file);
            foreach (var group in analysis.Duplicates) DuplicateFiles.Add($"{group.Paths.Count} arquivos com SHA-256 igual ({group.BytesPerFile:N0} bytes cada): {string.Join(" · ", group.Paths)}");
            Status = $"Análise somente leitura: {analysis.FilesObserved} arquivos; {analysis.BytesObserved:N0} bytes observados; {analysis.FilesHashed} hashes; {analysis.Skipped} ignorados. Limite atingido: {analysis.Limited}. Nenhum arquivo removido.";
        }), () => !busy && storageConsent && storageDirectory is not null);
        gameMode.Changed += (_, _) => System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => Changed(nameof(GameStatus)));
        RefreshProcessesCommand = new(() => _ = RunAsync(_ =>
        {
            GameProcesses.Clear(); SelectedProcess = null;
            foreach (var process in GameProcess.List()) GameProcesses.Add(process);
            Status = "Lista de processos atualizada. Escolha apenas o jogo que deseja observar.";
            return Task.CompletedTask;
        }), () => !busy);
        StartGameCommand = new(() => _ = RunAsync(_ =>
        {
            gameMode.Start(new GameProcess(SelectedProcess!.Id));
            Changed(nameof(GameStatus));
            return Task.CompletedTask;
        }), () => !busy && SelectedProcess is not null);
        StopGameCommand = new(() => { gameMode.Stop(); Changed(nameof(GameStatus)); });
        ScanCommand = new(() => _ = RunAsync(async token =>
        {
            await RunScanAsync(token);
        }), () => !busy);
        CancelCommand = new(() => cancellation?.Cancel(), () => busy);
        PreviewCleanupCommand = new(() => _ = RunAsync(AnalyzeCleanupAsync), () => !busy);
        CleanupCommand = new(() => _ = RunAsync(ExecuteCleanupAsync), () => !busy && CleanupConsent && CanReviewCleanup && CleanupFiles.Any(f => f.Selected));
        DeleteHistoryCommand = new(() => _ = RunAsync(async token =>
        {
            await repository.DeleteAllAsync(token);
            snapshots.Clear();
            CommunityText = "Histórico apagado. Prepare outro resumo após uma nova coleta.";
            await RefreshAsync(token);
            Status = "Snapshots removidos do banco local. Exportações externas não são apagadas.";
        }), () => !busy);
        ExportCommand = new(() => _ = RunAsync(_ => ExportSnapshotAsync()), () => !busy && snapshots.Current is not null);
        PrepareDraftCommand = new(() =>
        {
            CommunityText = CommunityDraftBuilder.Create(snapshots.Current).Body;
            Status = "Resumo preparado localmente. Nenhum envio realizado.";
        }, () => !busy && snapshots.Current is not null);
        ExportDraftCommand = new(() => _ = RunAsync(async token =>
        {
            var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = "phsystem-resumo.json" };
            if (dialog.ShowDialog() == true)
                await File.WriteAllTextAsync(dialog.FileName, CommunityDraftBuilder.Export(CommunityDraftBuilder.Create(snapshots.Current)), token);
        }), () => !busy && snapshots.Current is not null);
    }

    public Task InitializeAsync() => RunAsync(async token =>
    {
        Status = "Carregando o histórico local…";
        var latest = await repository.LoadLatestAsync(token);
        if (latest is not null) snapshots.Set(latest);
        await RefreshAsync(token);
        Status = latest is null ? "Pronto para a primeira coleta local." : "Último snapshot carregado. Verifique a validade das evidências antes de decidir.";
    });

    private async Task ExportSnapshotAsync()
    {
        var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = "phsystem-diagnostico-privado.json" };
        if (dialog.ShowDialog() != true) return;
        await File.WriteAllTextAsync(dialog.FileName, System.Text.Json.JsonSerializer.Serialize(snapshots.Current,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Status = "Snapshot exportado. O arquivo contém detalhes locais; revise antes de compartilhar.";
    }

    private async Task RefreshAsync(CancellationToken token)
    {
        Categories.Clear();
        if (snapshots.Current is { } snapshot)
            foreach (var category in snapshot.Categories)
                Categories.Add(new(category.Category, StateText(category.State), category.Source,
                    $"Válido até {category.ExpiresAt.ToLocalTime():g}",
                    category.Facts?.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => new FactLine(FriendlyFactName(f.Key),
                        f.Key.EndsWith("Bytes", StringComparison.Ordinal) && long.TryParse(f.Value, out var size) ? FormatBytes(size) : f.Value)).ToArray() ?? [],
                    string.Join(" · ", category.Errors)));
        Recommendations.Clear();
        Findings.Clear();
        if (snapshots.Current is { } current)
            foreach (var finding in new FindingEvaluator().Evaluate(current)) Findings.Add(finding);
        foreach (var item in recommendations.Evaluate(snapshots.Current)) Recommendations.Add(item);
        Drivers.Clear();
        foreach (var driver in drivers.Read(snapshots.Current)) Drivers.Add(driver);
        History.Clear();
        foreach (var item in await repository.LoadHistoryAsync(cancellationToken: token))
            History.Add(new(item.CapturedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                $"{item.Categories.Count(c => c.Facts?.Count > 0)} com dados · {item.Categories.Count(c => c.State == CollectionState.Complete)} completas", item.ScanId.ToString()));
        Changed(nameof(SnapshotStatus));
        Changed(nameof(Coverage));
        RebuildCategoryFilter();
        NotifyPresentation();
    }

    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (busy) return;
        busy = true;
        Status = "Executando operação…";
        cancellation = new();
        RefreshCommands();
        try { await action(cancellation.Token); }
        catch (OperationCanceledException) { Status = "Operação cancelada. Ações já concluídas permanecem aplicadas."; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException
            or System.Text.Json.JsonException or Microsoft.Data.Sqlite.SqliteException or Microsoft.EntityFrameworkCore.DbUpdateException or System.ComponentModel.Win32Exception or ArgumentException)
        { Status = $"Não foi possível concluir ({ex.GetType().Name}). Verifique acesso e espaço na pasta de dados local."; }
        finally
        {
            cancellation.Dispose();
            cancellation = null;
            busy = false;
            RefreshCommands();
            UpdateResultPages();
            NotifyPresentation();
        }
    }

    private void RefreshCommands()
    {
        foreach (var command in new[] { ScanCommand, CancelCommand, PreviewCleanupCommand, CleanupCommand,
            DeleteHistoryCommand, ExportCommand, PrepareDraftCommand, ExportDraftCommand,
            RefreshProcessesCommand, StartGameCommand, StopGameCommand }) command.Refresh();
        SelectStorageDirectoryCommand.Refresh(); AnalyzeStorageCommand.Refresh();
        Changed(nameof(IsBusy)); Changed(nameof(OperationLabel)); Changed(nameof(ScanButtonText));
    }
    private static string StateText(CollectionState state) => state switch
    {
        CollectionState.Complete => "Completo", CollectionState.Partial => "Parcial",
        CollectionState.Error => "Erro", _ => "Indisponível"
    };
    private static string FormatBytes(long bytes) => DisplayFormat.Bytes(bytes);
}
