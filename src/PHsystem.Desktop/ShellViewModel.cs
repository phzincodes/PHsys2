using System.Collections.ObjectModel;
using System.Globalization;
using PHsystem.Core;
using PHsystem.Optimizer;

namespace PHsystem.Desktop;

public sealed record NavigationEntry(string Title, string Group, string Icon, string Description);
public sealed record VolumeSummary(string Name, string Capacity, string Available, double UsedPercent);

public static class DisplayFormat
{
    public static string Bytes(long bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB", "PiB"];
        double value = Math.Max(0, bytes); var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value.ToString(unit == 0 ? "N0" : "N1", CultureInfo.CurrentCulture)} {units[unit]}";
    }
}

public sealed partial class MainViewModel
{
    public IReadOnlyList<NavigationEntry> Navigation { get; } =
    [
        new("Início", "COMPUTADOR", "M3,10 L12,3 21,10 M5,9 V21 H10 V15 H14 V21 H19 V9", "Uma análise, todos os resultados."),
        new("Diagnóstico", "COMPUTADOR", "M3,12 H7 L10,5 14,19 17,12 H21", "Explore os componentes e as informações da última análise."),
        new("Limpeza", "COMPUTADOR", "M4,7 H20 M9,7 V4 H15 V7 M6,7 L7,21 H17 L18,7 M10,10 V17 M14,10 V17", "Revise os arquivos antes de liberar espaço."),
        new("Armazenamento", "COMPUTADOR", "M4,4 H20 V20 H4 Z M4,15 H20 M15,17 H17", "Entenda o espaço disponível e analise uma pasta."),
        new("Drivers", "COMPUTADOR", "M7,3 V7 M12,3 V7 M17,3 V7 M7,17 V21 M12,17 V21 M17,17 V21 M3,7 H7 M17,7 H21 M3,17 H7 M17,17 H21 M7,7 H17 V17 H7 Z", "Consulte o inventário de vídeo e rede."),
        new("PH CORE", "FERRAMENTAS", "M12,3 L21,8 V16 L12,21 3,16 V8 Z M12,7 V13 M12,16 V17", "Orientações a partir das evidências do diagnóstico."),
        new("Modo Jogo", "FERRAMENTAS", "M7,7 H17 Q20,7 21,12 L22,17 Q22,20 19,19 L16,16 H8 L5,19 Q2,20 2,17 L3,12 Q4,7 7,7 M7,10 V15 M4.5,12.5 H9.5 M16,11 H17 M18,14 H19", "Um perfil temporário para o processo escolhido."),
        new("Histórico", "FERRAMENTAS", "M4,8 A9,9 0 1 1 3,15 M3,3 V9 H9 M12,7 V12 L16,15", "Consulte e exporte as análises salvas neste computador."),
        new("Comunidade", "PESSOAL", "M3,4 H21 V17 H10 L5,21 V17 H3 Z M7,8 H17 M7,12 H14", "Conta, perguntas, mensagens e perfil público."),
        new("Privacidade", "PESSOAL", "M12,3 L20,6 V12 Q20,18 12,22 Q4,18 4,12 V6 Z M8,12 L11,15 16,9", "Controle o que permanece local e o que você compartilha."),
        new("Hardware", "COMPUTADOR", "M4,4 H20 V17 H4 Z M8,21 H16 M12,17 V21", "Componentes da mesma análise central."),
        new("Segurança", "COMPUTADOR", "M12,3 L20,6 V12 Q20,18 12,22 Q4,18 4,12 V6 Z M8,12 L11,15 16,9", "Proteções e evidências; estados parciais não equivalem a segurança comprovada."),
        new("Desempenho", "COMPUTADOR", "M4,19 V13 M9,19 V8 M14,19 V11 M19,19 V4", "Memória e inicialização da análise. Não são métricas em tempo real."),
        new("Configurações", "PESSOAL", "M4,6 H20 M4,12 H20 M4,18 H20 M8,3 V9 M16,9 V15 M10,15 V21", "Preferências locais do aplicativo.")
    ];
    private int selectedPageIndex => (int)NavigationState.Current;
    public NavigationService NavigationState { get; } = new();
    private bool sidebarExpanded = true;
    private string categoryFilter = "";
    private CategoryCard? selectedCategory;
    private int communityTabIndex;
    private int socialTabIndex;
    public int SocialTabIndex { get => socialTabIndex; set { socialTabIndex = value; Changed(); } }
    public int CommunityTabIndex { get => communityTabIndex; set { if (value is < 0 or > 4) return; communityTabIndex = value; Changed(); } }
    public int SelectedPageIndex
    {
        get => selectedPageIndex;
        set
        {
            if (value < 0 || value >= Navigation.Count || value == selectedPageIndex) return;
            NavigationState.Navigate((AppPage)value);
        }
    }
    public NavigationEntry SelectedNavigationItem { get => Navigation[selectedPageIndex]; set { var index = Navigation.ToList().IndexOf(value); if (index >= 0) SelectedPageIndex = index; } }
    public bool IsHome => selectedPageIndex == 0;
    public string PageTitle => Navigation[selectedPageIndex].Title;
    public string PageDescription => Navigation[selectedPageIndex].Description;
    public string PageGroup => Navigation[selectedPageIndex].Group;
    public bool SidebarExpanded { get => sidebarExpanded; private set { sidebarExpanded = value; Changed(); Changed(nameof(SidebarWidth)); Changed(nameof(SidebarToggleLabel)); } }
    public System.Windows.GridLength SidebarWidth => new(SidebarExpanded ? Preferences.CurrentTheme.SidebarWidth : 68);
    public string SidebarToggleLabel => SidebarExpanded ? "Recolher menu" : "Expandir menu";
    public UiCommand ToggleSidebarCommand { get; private set; } = null!;
    public UiCommand OpenOverviewCommand { get; private set; } = null!;
    public UiCommand OpenDiagnosticsCommand { get; private set; } = null!;
    public UiCommand OpenCleanupCommand { get; private set; } = null!;
    public UiCommand OpenStorageCommand { get; private set; } = null!;
    public UiCommand OpenDriversCommand { get; private set; } = null!;
    public UiCommand OpenRecommendationsCommand { get; private set; } = null!;
    public UiCommand OpenHistoryCommand { get; private set; } = null!;
    public UiCommand OpenCommunityCommand { get; private set; } = null!;
    public UiCommand OpenSharingCommand { get; private set; } = null!;
    public bool IsBusy => busy;
    public string OperationLabel => busy ? "Em andamento" : "Pronto";
    public string ScanButtonText => busy ? "Analisando…" : "Analisar computador";
    public bool HasSnapshot => snapshots.Current is not null;
    public string LastScanLabel => snapshots.Current is { } scan ? $"{scan.CapturedAt.ToLocalTime():dd/MM 'às' HH:mm}" : "Não realizada";
    public int CategoryCount => Categories.Count;
    public int AvailableCategoryCount => snapshots.Current?.Categories.Count(c => c.Facts?.Count > 0 && c.State is CollectionState.Complete or CollectionState.Partial) ?? 0;
    public string CollectionSummary => HasSnapshot ? $"{AvailableCategoryCount} de {CategoryCount} categorias com informações" : "Conheça o estado do seu computador";
    public string CpuModel => Fact("Processador", "Modelo") ?? "Aguardando análise";
    public string CpuDetail => Fact("Processador", "Processadores lógicos") is { } cores ? $"{cores} processadores lógicos" : "Modelo e núcleos do processador";
    public string MemoryTotal => LongFact("Memória", "TotalBytes") is { } bytes ? DisplayFormat.Bytes(bytes) : "—";
    public string MemoryDetail => LongFact("Memória", "AvailableBytes") is { } bytes ? $"{DisplayFormat.Bytes(bytes)} disponíveis" : "Capacidade e uso de memória";
    public double MemoryUsedPercent => double.TryParse(Fact("Memória", "UsoPercentual"), CultureInfo.InvariantCulture, out var value) ? Math.Clamp(value, 0, 100) : 0;
    public bool HasMemoryData => LongFact("Memória", "TotalBytes") is > 0;
    public string MemoryUsageLabel => HasMemoryData ? $"{MemoryUsedPercent:N0}% em uso" : "Sem leitura";
    public string OperatingSystem => Fact("Sistema", "Sistema") ?? "Informações disponíveis após a análise";
    public string Architecture => Fact("Sistema", "Arquitetura") ?? "—";
    public string StorageAvailable => Volumes.Count > 0 ? DisplayFormat.Bytes(StorageBytes(" FreeBytes")) : "—";
    public string StorageDetail => Volumes.Count > 0 ? $"livres de {DisplayFormat.Bytes(StorageBytes(" TotalBytes"))}" : "Volumes e espaço disponível";
    public string FindingsSummary => !HasSnapshot ? "Aguardando evidências" : Findings.Count switch
    {
        0 when snapshots.Current!.Coverage < .8 => "Cobertura insuficiente para concluir",
        0 => "Nenhum alerta pelas regras atuais", 1 => "1 ponto para revisar", _ => $"{Findings.Count} pontos para revisar"
    };
    public string CleanupSize => cleanupPlan is null ? "—" : DisplayFormat.Bytes(cleanupPlan.TotalBytes);
    public string CleanupCount => cleanupPlan is not null ? $"{CleanupFiles.Count} arquivos elegíveis" : CleanupState == CleanupScreenState.NotAnalyzed ? "Nenhum arquivo avaliado ainda" : "Consulte o estado e o resultado da operação";
    public string StorageSelectionHint => storageDirectory is null ? "Escolha uma pasta para começar" : "Pasta selecionada · análise somente leitura";
    public string DriverCountLabel => Drivers.Count == 0 ? "Nenhum registro carregado" : $"{Drivers.Count} registros no último diagnóstico";
    public string HistoryCountLabel => History.Count == 0 ? "Nenhuma análise salva" : $"{History.Count} análises recentes";
    public ObservableCollection<VolumeSummary> Volumes { get; } = [];
    public ObservableCollection<Recommendation> CurrentRecommendations { get; } = [];
    public ObservableCollection<Recommendation> RefreshRecommendations { get; } = [];
    public bool HasExpiredEvidence => RefreshRecommendations.Count > 0;
    public string ExpiredEvidenceLabel => $"{RefreshRecommendations.Count} categorias precisam de uma nova leitura. Atualize antes de tomar decisões.";
    public ObservableCollection<CategoryCard> FilteredCategories { get; } = [];
    public string CategoryFilter { get => categoryFilter; set { categoryFilter = value; Changed(); RebuildCategoryFilter(); } }
    public CategoryCard? SelectedCategory { get => selectedCategory; set { selectedCategory = value; Changed(); } }

    private void InitializeShell()
    {
        NavigationState.Changed += (_, _) =>
        {
            Changed(nameof(SelectedPageIndex)); Changed(nameof(SelectedNavigationItem)); Changed(nameof(IsHome));
            Changed(nameof(PageTitle)); Changed(nameof(PageDescription)); Changed(nameof(PageGroup));
        };
        ToggleSidebarCommand = new(() => SidebarExpanded = !SidebarExpanded);
        OpenOverviewCommand = new(() => SelectedPageIndex = 0);
        OpenDiagnosticsCommand = new(() => SelectedPageIndex = 1);
        OpenCleanupCommand = new(() => SelectedPageIndex = 2);
        OpenStorageCommand = new(() => SelectedPageIndex = 3);
        OpenDriversCommand = new(() => SelectedPageIndex = 4);
        OpenRecommendationsCommand = new(() => SelectedPageIndex = 5);
        OpenHistoryCommand = new(() => SelectedPageIndex = 7);
        OpenCommunityCommand = new(() => SelectedPageIndex = 8);
        OpenSharingCommand = new(() => { CommunityTabIndex = 3; SocialTabIndex = 1; SelectedPageIndex = 8; });
    }
    public void AdaptToWidth(double width)
    {
        if (width < 1120 && SidebarExpanded) SidebarExpanded = false;
    }
    private string? Fact(string category, string name) => snapshots.Current?.Categories.FirstOrDefault(c => c.Category == category)?.Facts?.GetValueOrDefault(name);
    private long? LongFact(string category, string name) => long.TryParse(Fact(category, name), CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : null;
    private long StorageBytes(string suffix) => snapshots.Current?.Categories.FirstOrDefault(c => c.Category == "Armazenamento")?.Facts?
        .Where(f => f.Key.EndsWith(suffix, StringComparison.Ordinal)).Sum(f => long.TryParse(f.Value, CultureInfo.InvariantCulture, out var bytes) && bytes >= 0 ? bytes : 0) ?? 0;
    private void RebuildCategoryFilter()
    {
        var previous = SelectedCategory;
        FilteredCategories.Clear();
        foreach (var category in Categories.Where(c => c.Name.Contains(categoryFilter, StringComparison.OrdinalIgnoreCase) || c.State.Contains(categoryFilter, StringComparison.OrdinalIgnoreCase))) FilteredCategories.Add(category);
        SelectedCategory = FilteredCategories.FirstOrDefault(c => c.Name == previous?.Name) ?? FilteredCategories.FirstOrDefault();
    }
    private void NotifyPresentation()
    {
        UpdateHealthPresentation();
        UpdateDashboardWidgets();
        CurrentRecommendations.Clear(); RefreshRecommendations.Clear();
        foreach (var item in Recommendations)
            (item.RequiresRefresh ? RefreshRecommendations : CurrentRecommendations).Add(item);
        Changed(nameof(HasExpiredEvidence)); Changed(nameof(ExpiredEvidenceLabel));
        Volumes.Clear();
        var facts = snapshots.Current?.Categories.FirstOrDefault(c => c.Category == "Armazenamento")?.Facts;
        if (facts is not null)
            foreach (var (key, text) in facts.Where(f => f.Key.EndsWith(" TotalBytes", StringComparison.Ordinal)).OrderBy(f => f.Key, StringComparer.Ordinal))
                if (long.TryParse(text, CultureInfo.InvariantCulture, out var total) && total > 0
                    && facts.TryGetValue(key.Replace(" TotalBytes", " FreeBytes", StringComparison.Ordinal), out var freeText)
                    && long.TryParse(freeText, CultureInfo.InvariantCulture, out var free) && free >= 0)
                    Volumes.Add(new(key.Replace(" TotalBytes", "", StringComparison.Ordinal), DisplayFormat.Bytes(total), $"{DisplayFormat.Bytes(free)} livres", Math.Clamp(100d * (total - free) / total, 0, 100)));
        foreach (var property in new[] { nameof(HasSnapshot), nameof(LastScanLabel), nameof(CategoryCount), nameof(AvailableCategoryCount), nameof(CollectionSummary),
            nameof(CpuModel), nameof(CpuDetail), nameof(MemoryTotal), nameof(MemoryDetail), nameof(MemoryUsedPercent), nameof(HasMemoryData), nameof(MemoryUsageLabel),
            nameof(OperatingSystem), nameof(Architecture), nameof(StorageAvailable), nameof(StorageDetail), nameof(FindingsSummary), nameof(CleanupSize), nameof(CleanupCount),
            nameof(StorageSelectionHint), nameof(DriverCountLabel), nameof(HistoryCountLabel) }) Changed(property);
    }
    private static string FriendlyFactName(string name)
    {
        var parts = name.Split(" · ", 2);
        var field = parts[^1];
        var label = field switch
        {
            "AntivirusEnabled" => "Defender ativo", "RealTimeProtectionEnabled" => "Proteção em tempo real", "AMServiceEnabled" => "Serviço antimalware",
            "AntivirusSignatureAge" => "Idade das assinaturas (dias)", "AntivirusSignatureLastUpdated" => "Assinaturas atualizadas em", "QuickScanAge" => "Última verificação rápida (dias)", "FullScanAge" => "Última verificação completa (dias)",
            "DomainEnabled" => "Firewall domínio", "PrivateEnabled" => "Firewall privado", "PublicEnabled" => "Firewall público", "AllProfilesEnabled" => "Todos os perfis habilitados", "ActiveProfilesEnabled" => "Perfis ativos habilitados",
            "TotalBytes" => "Capacidade total", "AvailableBytes" => "Memória disponível", "UsoPercentual" => "Memória em uso (%)",
            "Name" => "Nome", "DriverVersion" => "Versão do driver", "AdapterRAM" => "Memória informada pelo adaptador",
            "ConfigManagerErrorCode" => "Código informado pelo Windows", "State" => "Estado", "StartMode" => "Inicialização",
            "HotFixID" => "Atualização", "InstalledOn" => "Instalada em", "displayName" => "Produto", "productState" => "Estado informado pelo Windows",
            "Model" => "Modelo", "Status" => "Estado informado", "Location" => "Origem de inicialização", _ => field.Replace(" TotalBytes", " · capacidade").Replace(" FreeBytes", " · disponível")
        };
        return parts.Length > 1 ? $"{parts[0]} · {label}" : label;
    }
}
