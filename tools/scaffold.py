"""Historical M0 bootstrap. Do not rerun: overwrites subsequent edits. No legacy input."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[1]
def put(name, content):
    p = ROOT / name
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(content.strip() + '\n', encoding='utf-8')

modules = ['Core','Windows','Diagnostics','Cleanup','Drivers','Optimizer','LocalData','PrivilegedHelper','SharedContracts','Server','Community','Admin','Desktop']
refs = {'Windows':['Core'],'Diagnostics':['Core'],'Cleanup':['Core'],'Drivers':['Core'],'Optimizer':['Core'],'LocalData':['Core'],'Community':['Core'],'Server':['Community','SharedContracts'],'Desktop':['Core','Diagnostics','Windows','LocalData']}
for name in modules:
    windows = name in ['Windows','Desktop','PrivilegedHelper']
    framework = 'net10.0-windows' if windows else 'net10.0'
    sdk = 'Microsoft.NET.Sdk.Web' if name in ['Server','Admin'] else 'Microsoft.NET.Sdk'
    extra = '<OutputType>WinExe</OutputType><UseWPF>true</UseWPF>' if name == 'Desktop' else '<OutputType>Exe</OutputType>' if name == 'PrivilegedHelper' else ''
    items = ''.join(f'<ProjectReference Include="../PHsystem.{r}/PHsystem.{r}.csproj" />' for r in refs.get(name, []))
    if name == 'Desktop':
        items += '<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />'
    put(f'src/PHsystem.{name}/PHsystem.{name}.csproj', f'<Project Sdk="{sdk}"><PropertyGroup><TargetFramework>{framework}</TargetFramework>{extra}</PropertyGroup><ItemGroup>{items}</ItemGroup></Project>')

put('Directory.Build.props', '<Project><PropertyGroup><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><TreatWarningsAsErrors>true</TreatWarningsAsErrors><AnalysisLevel>latest</AnalysisLevel><Deterministic>true</Deterministic></PropertyGroup></Project>')
put('global.json', json.dumps({'sdk':{'version':'10.0.100','rollForward':'latestFeature'}}, indent=2))
projects = [f'src/PHsystem.{n}/PHsystem.{n}.csproj' for n in modules]
for name in ['UnitTests','IntegrationTests','WindowsTests','SecurityTests','UiTests','E2ETests']:
    projects.append(f'tests/PHsystem.{name}/PHsystem.{name}.csproj')
    put(projects[-1], '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject><IsPackable>false</IsPackable></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1"/><PackageReference Include="xunit" Version="2.9.3"/><PackageReference Include="xunit.runner.visualstudio" Version="3.1.4"><PrivateAssets>all</PrivateAssets></PackageReference><ProjectReference Include="../../src/PHsystem.Core/PHsystem.Core.csproj"/></ItemGroup></Project>')
put('PHsystem.slnx', '<Solution>\n' + '\n'.join(f'  <Project Path="{p}" />' for p in projects) + '\n</Solution>')
put('src/PHsystem.Core/DiagnosticSnapshot.cs', '''using System.Collections.Immutable;
namespace PHsystem.Core;
public enum CollectionState { Unavailable, Partial, Complete, Error }
public sealed record CategoryResult(string Category, CollectionState State, DateTimeOffset ExpiresAt, string Source, double Confidence, ImmutableArray<string> Errors);
public sealed record DiagnosticSnapshot(Guid ScanId, DateTimeOffset CapturedAt, string CollectorVersion, ImmutableArray<CategoryResult> Categories)
{
    public double Coverage => Categories.IsEmpty ? 0 : (double)Categories.Count(c => c.State == CollectionState.Complete) / Categories.Length;
    public int? HealthScore => null; // No score until evidence-based rules exist.
}
public interface ISnapshotStore { DiagnosticSnapshot? Current { get; } }
''')
put('src/PHsystem.Diagnostics/SnapshotStore.cs', '''using PHsystem.Core;
namespace PHsystem.Diagnostics;
public sealed class SnapshotStore : ISnapshotStore
{
    public DiagnosticSnapshot? Current { get; private set; }
    public void Set(DiagnosticSnapshot snapshot) => Current = snapshot;
}
''')
put('tests/PHsystem.UnitTests/SnapshotTests.cs', '''using System.Collections.Immutable;
using PHsystem.Core;
using Xunit;
namespace PHsystem.UnitTests;
public sealed class SnapshotTests
{
    [Fact] public void MissingEvidenceNeverProducesPerfectScore()
    {
        var snapshot = new DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "0.1.0", ImmutableArray<CategoryResult>.Empty);
        Assert.Equal(0, snapshot.Coverage);
        Assert.Null(snapshot.HealthScore);
    }
    [Fact] public void PartialEvidenceIsNotCompleteCoverage()
    {
        var category = new CategoryResult("CPU", CollectionState.Partial, DateTimeOffset.UtcNow, "test", .5, []);
        Assert.Equal(0, new DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", [category]).Coverage);
    }
}
''')
put('src/PHsystem.PrivilegedHelper/Program.cs', 'Console.Error.WriteLine("Helper indisponÃ­vel: nenhuma operaÃ§Ã£o privilegiada foi implementada ou autorizada.");\nreturn 2;')
for name in ['Server','Admin']:
    put(f'src/PHsystem.{name}/Program.cs', '''var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "foundation", identityConfigured = false }));
app.MapGet("/", () => Results.Problem("MÃ³dulo ainda nÃ£o implementado. NÃ£o hÃ¡ autenticaÃ§Ã£o nem funÃ§Ãµes administrativas disponÃ­veis.", statusCode: 503));
app.Run();
''')
put('src/PHsystem.Desktop/App.xaml', '<Application x:Class="PHsystem.Desktop.App" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"><Application.Resources><ResourceDictionary Source="Theme.xaml" /></Application.Resources></Application>')
put('src/PHsystem.Desktop/App.xaml.cs', '''using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PHsystem.Core;
using PHsystem.Diagnostics;
namespace PHsystem.Desktop;
public partial class App : Application
{
    private ServiceProvider? services;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        services = new ServiceCollection().AddSingleton<ISnapshotStore, SnapshotStore>().AddSingleton<MainViewModel>().AddSingleton<MainWindow>().BuildServiceProvider();
        services.GetRequiredService<MainWindow>().Show();
    }
    protected override void OnExit(ExitEventArgs e) { services?.Dispose(); base.OnExit(e); }
}
''')
put('src/PHsystem.Desktop/MainViewModel.cs', '''using PHsystem.Core;
namespace PHsystem.Desktop;
public sealed class MainViewModel(ISnapshotStore snapshots)
{
    public string SnapshotStatus => snapshots.Current is null ? "Nenhuma varredura realizada" : $"Varredura {snapshots.Current.ScanId}";
    public string Coverage => snapshots.Current is null ? "Cobertura indisponÃ­vel" : $"Cobertura: {snapshots.Current.Coverage:P0}";
}
''')
put('src/PHsystem.Desktop/Theme.xaml', '''<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
<SolidColorBrush x:Key="BackgroundBrush" Color="#050B17"/><SolidColorBrush x:Key="PanelBrush" Color="#0D1A2B"/><SolidColorBrush x:Key="TextBrush" Color="#F0F5FF"/>
<Style TargetType="Window"><Setter Property="Background" Value="{DynamicResource BackgroundBrush}"/><Setter Property="Foreground" Value="{DynamicResource TextBrush}"/><Setter Property="FontFamily" Value="Segoe UI"/></Style>
<Style TargetType="TextBlock"><Setter Property="TextWrapping" Value="Wrap"/><Setter Property="Margin" Value="0,0,0,12"/></Style>
<Style TargetType="Button"><Setter Property="Padding" Value="16,10"/><Setter Property="Background" Value="#0F2135"/><Setter Property="Foreground" Value="#F0F5FF"/></Style>
</ResourceDictionary>''')
put('src/PHsystem.Desktop/MainWindow.xaml', '''<Window x:Class="PHsystem.Desktop.MainWindow" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Title="PHsystem Next" Width="1280" Height="720" MinWidth="800" MinHeight="560">
<Grid Margin="24"><Grid.ColumnDefinitions><ColumnDefinition Width="220"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
<StackPanel Margin="0,0,24,0"><TextBlock Text="PHsystem Next" FontSize="26" Foreground="#12E5C9"/><TextBlock Text="FundaÃ§Ã£o local Â· M0"/><Button Content="_VisÃ£o geral" Click="OverviewClick" AutomationProperties.Name="Abrir visÃ£o geral"/><TextBlock Margin="0,24,0,0" Text="DiagnÃ³stico, limpeza, drivers e comunidade serÃ£o habilitados conforme implementaÃ§Ã£o e testes."/></StackPanel>
<ScrollViewer Grid.Column="1" VerticalScrollBarVisibility="Auto"><StackPanel><TextBlock Text="Seu computador. Suas decisÃµes." FontSize="32"/><Border Background="{DynamicResource PanelBrush}" Padding="24" CornerRadius="12"><StackPanel><TextBlock Text="{Binding SnapshotStatus}" FontSize="22"/><TextBlock Text="{Binding Coverage}"/><TextBlock Text="Score indisponÃ­vel â€” ainda nÃ£o existem evidÃªncias coletadas." Foreground="#BA8BFF"/><TextBlock Text="Esta versÃ£o valida a estrutura do aplicativo. Nenhuma limpeza, instalaÃ§Ã£o de driver ou transmissÃ£o de hardware Ã© executada."/></StackPanel></Border><TextBlock Margin="0,24,0,0" Text="Status do marco" FontSize="22"/><TextBlock x:Name="StatusText" Text="M0 em preparaÃ§Ã£o. Consulte docs/STATUS.md para validaÃ§Ãµes reais."/></StackPanel></ScrollViewer></Grid></Window>''')
put('src/PHsystem.Desktop/MainWindow.xaml.cs', '''using System.Windows;
namespace PHsystem.Desktop;
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel model) { InitializeComponent(); DataContext = model; }
    private void OverviewClick(object sender, RoutedEventArgs e) => StatusText.Text = "VisÃ£o geral aberta. Recursos operacionais aguardam os prÃ³ximos marcos.";
}
''')
put('.gitignore', 'bin/\nobj/\n.vs/\n.env\n.env.*\n!.env.example\n*.db*\n*.log\n*.dmp\nartifacts/\nsecrets/\n.local/\n__pycache__/')
put('.env.example', '# Somente nomes; configurar localmente, nunca versionar valores reais.\nConnectionStrings__Community=\nEmail__Provider=\nStorage__Endpoint=')
put('phsystem-next.code-workspace', json.dumps({'folders':[{'path':'.'}]},indent=2))
put('.vscode/extensions.json', json.dumps({'recommendations':['ms-dotnettools.csdevkit','ms-dotnettools.csharp','openai.chatgpt']},indent=2))
tasks = [{'label':label,'type':'process','command':'dotnet','args':args,'problemMatcher':'$msCompile'} for label,args in [('restore',['restore','PHsystem.slnx']),('build',['build','PHsystem.slnx']),('test',['test','PHsystem.slnx']),('desktop',['run','--project','src/PHsystem.Desktop']),('publish',['publish','src/PHsystem.Desktop','-c','Release','-r','win-x64','--self-contained','true','-o','artifacts/desktop'])]]
put('.vscode/tasks.json', json.dumps({'version':'2.0.0','tasks':tasks},indent=2))
put('.vscode/launch.json', json.dumps({'version':'0.2.0','configurations':[{'name':'PHsystem Desktop','type':'coreclr','request':'launch','preLaunchTask':'build','program':'${workspaceFolder}/src/PHsystem.Desktop/bin/Debug/net10.0-windows/PHsystem.Desktop.dll','cwd':'${workspaceFolder}'}]},indent=2))
slots = {'branding':['logo-horizontal.svg','logo-symbol.svg','app-icon.png'],'backgrounds':['login-cover.webp','dashboard-decoration.webp','community-header.webp'],'illustrations':['scan-empty.webp','scan-complete.webp','cleanup-empty.webp','driver-warning.webp','optimizer-guide.webp','offline.webp'],'icons':[x+'.svg' for x in ['cpu','gpu','ram','disk','network','shield','driver','repair']],'community/categories':[x+'.webp' for x in ['hardware','windows','games','drivers','network']],'avatars':['default-avatar.png'],'badges':['verified-expert.svg','moderator.svg','administrator.svg'],'sounds':['click.wav','success.wav','warning.wav']}
catalog=[]
for directory,names in slots.items():
    (ROOT/'assets'/directory).mkdir(parents=True,exist_ok=True)
    put(f'assets/{directory}/.gitkeep','')
    for name in names:
        catalog.append({'path':directory+'/'+name,'required':False,'fallback':'silÃªncio' if directory=='sounds' else 'texto ou geometria neutra','maxBytes':4194304})
put('assets/assets-manifest.json',json.dumps({'version':1,'assets':catalog},indent=2,ensure_ascii=False))
put('assets/README-IMAGENS.md', '# CatÃ¡logo de assets\n\nNenhuma arte gerada. Arquivos ainda ausentes; nÃ£o adicionar dados de terceiros sem licenÃ§a. AssetProvider com validaÃ§Ã£o/cache estÃ¡ pendente. UI M0 usa texto e geometria XAML.\n\nArquivo | Componente/tela | FunÃ§Ã£o | DimensÃ£o sugerida | Formato | ObrigatÃ³rio | AusÃªncia\n---|---|---|---|---|---|---\n'+'\n'.join(f"{a['path']} | {a['path'].split('/')[0]} | identidade/estado local | {'48 kHz mono' if a['path'].endswith('wav') else '256Ã—256 (1:1); capas 1920Ã—1080 (16:9); logo 640Ã—160 (4:1)'} | {a['path'].split('.')[-1]} | opcional | {a['fallback']}" for a in catalog))
put('AGENTS.md', '# PHsystem Next\n\nCÃ³digo novo; nÃ£o importar motores antigos. .NET 10, WPF, monÃ³lito modular. UI sem elevaÃ§Ã£o. Nunca executar comandos da comunidade. Dados locais privados. Nenhuma publicaÃ§Ã£o ou instalaÃ§Ã£o sem autorizaÃ§Ã£o explÃ­cita.\n\nValidar: dotnet restore PHsystem.slnx; dotnet build PHsystem.slnx; dotnet test PHsystem.slnx; dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop.\n\nAtualizar docs/STATUS.md com evidÃªncias reais; testes vazios nÃ£o contam como cobertura. NÃ£o declarar mÃ³dulos operacionais por existirem projetos.')
put('docs/ARCHITECTURE.md', '# Arquitetura\n\nMonorepo novo, monÃ³lito modular .NET 10. Core contÃ©m contratos imutÃ¡veis sem UI. Diagnostics coordena o snapshot; Windows implementarÃ¡ coletores nativos. Desktop usa WPF, MVVM e DI com ISnapshotStore singleton. Cleanup, Drivers e Optimizer consomem o snapshot sem novas varreduras por navegaÃ§Ã£o. LocalData implementarÃ¡ SQLite/EF Core. Server terÃ¡ Identity/PostgreSQL e SignalR; Admin separado e MFA obrigatÃ³rio. Helper atualmente recusa toda operaÃ§Ã£o.\n\nM0 somente: shell desktop, contratos iniciais e health endpoints. Nenhum coletor, autenticaÃ§Ã£o, persistÃªncia ou aÃ§Ã£o privilegiada estÃ¡ implementado. APIs pÃºblicas do produto, AssetProvider e acessibilidade completa permanecem pendentes.')
put('docs/STATUS.md', '# Estado verificÃ¡vel\n\nFuncionalidade | Implementado | Testado localmente | Testado Windows | Pendente | Risco\n---|---|---|---|---|---\nM0 soluÃ§Ã£o modular | arquivos criados | nÃ£o | nÃ£o | SDK 10 e validaÃ§Ã£o | build nÃ£o comprovado\nWPF / DI / MVVM | shell inicial | nÃ£o | nÃ£o | compilaÃ§Ã£o, execuÃ§Ã£o e UI | XAML nÃ£o validado\nSnapshot | contrato inicial | nÃ£o | nÃ£o | executar testes | sem coletores\nAssets | catÃ¡logo | inspeÃ§Ã£o | nÃ£o | provider, formatos/cache | catÃ¡logo ainda sem arquivos\nM1â€“M8 | nÃ£o | nÃ£o | nÃ£o | implementaÃ§Ã£o sequencial | nÃ£o disponÃ­vel\n\nAmbiente: Windows 11 Pro 10.0.26200 x64; SDK 8.0.427 apenas; runtimes 10.0.12 disponÃ­veis. InstalaÃ§Ã£o de SDK 10 depende de autorizaÃ§Ã£o solicitada. Docker/Podman/gh nÃ£o encontrados. Git sem nome/e-mail configurados; commit depende de identidade escolhida pelo proprietÃ¡rio. VS Code 1.120.0; extensÃµes C# e Codex ausentes.')
put('README.md', '# PHsystem Next\n\nPlataforma Windows em implementaÃ§Ã£o incremental. CÃ³digo novo em .NET 10/WPF; nenhum componente do PHsystem antigo importado.\n\nEstado: M0 em preparaÃ§Ã£o, sem diagnÃ³stico operacional. Veja [STATUS](docs/STATUS.md) e [arquitetura](docs/ARCHITECTURE.md). NÃ£o usar para limpar ou alterar o computador.\n\n## Abrir e validar\n\nPrÃ©-requisito: SDK .NET 10 LTS. Abra `phsystem-next.code-workspace` no VS Code. Terminal â†’ Executar Tarefa â†’ restore, build, test e desktop. Para depurar: instale as extensÃµes recomendadas, F5 â†’ PHsystem Desktop; breakpoint em MainWindow.xaml.cs.\n\n```powershell\ndotnet restore PHsystem.slnx\ndotnet build PHsystem.slnx\ndotnet test PHsystem.slnx\ndotnet run --project src/PHsystem.Desktop\n```\n\nNenhum serviÃ§o externo ou envio de dados estÃ¡ configurado. CI sÃ³ executarÃ¡ apÃ³s autorizaÃ§Ã£o de repositÃ³rio remoto. Recursos M1â€“M8 pendentes; nenhuma release disponÃ­vel.')
put('CHANGELOG.md', '# Changelog\n\n## 0.1.0 â€” em desenvolvimento\n\nFundaÃ§Ã£o modular, shell WPF, contrato de snapshot, catÃ¡logo de assets e configuraÃ§Ãµes VS Code; build pendente de SDK 10.')
put('.github/workflows/build.yml', '''name: Validate
on: [push, pull_request]
permissions:
  contents: read
jobs:
  windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore PHsystem.slnx
      - run: dotnet build PHsystem.slnx --no-restore
      - run: dotnet test PHsystem.slnx --no-build --logger trx
      - run: dotnet list PHsystem.slnx package --vulnerable --include-transitive
      - run: dotnet publish src/PHsystem.Desktop -c Release -r win-x64 --self-contained true -o artifacts/desktop
''')
for folder in ['site','installer','tools/ProvisionAdmin']:
    put(folder+'/README.md','# Pendente\n\nImplementaÃ§Ã£o prevista em marco posterior. Nenhum artefato de produÃ§Ã£o disponÃ­vel.')
print(f'Foundation written at {ROOT}')
