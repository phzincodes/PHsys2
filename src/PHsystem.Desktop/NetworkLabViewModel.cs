using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
namespace PHsystem.Desktop;
public sealed class NetworkLabViewModel : ObservableObject
{
    public string Target { get; set; } = "";
    private string result = "Informe um destino. O teste envia consultas DNS e até cinco pacotes ICMP somente após sua ação. Não mede a velocidade contratada.";
    public string Result { get => result; private set { result=value; Changed(); } }
    private CancellationTokenSource? cancellation;
    private UiCommand? start, cancel;
    public UiCommand StartCommand => start ??= new(() => _ = RunAsync(), () => cancellation is null);
    public UiCommand CancelCommand => cancel ??= new(() => cancellation?.Cancel(), () => cancellation is not null);
    public static bool ValidTarget(string value) => value.Length is > 0 and <= 253 && !value.Any(char.IsWhiteSpace)
        && (IPAddress.TryParse(value, out _) || Regex.IsMatch(value, "^(?=.{1,253}$)([A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\\.)*[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$"));
    public async Task RunAsync()
    {
        if (cancellation is not null) return;
        var target=Target.Trim(); if (!ValidTarget(target)) { Result="Use um hostname ou IP válido, sem URL, caminho ou argumentos."; return; }
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20)); cancellation=timeout; start?.Refresh(); cancel?.Refresh();
        Result="Resolvendo DNS e medindo cinco amostras…";
        try
        {
            var addresses=await Dns.GetHostAddressesAsync(target,timeout.Token); var address=addresses.FirstOrDefault(a=>a.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork) ?? addresses.First();
            var samples=new List<long>();
            for(var i=0;i<5;i++) { using var ping=new Ping(); var reply=await ping.SendPingAsync(address,TimeSpan.FromSeconds(2),new byte[32],null,timeout.Token); if(reply.Status==IPStatus.Success) samples.Add(reply.RoundtripTime); await Task.Delay(120,timeout.Token); }
            Result=$"Destino resolvido: {address}\nRecebidos {samples.Count}/5 · perda {(5-samples.Count)*20}%\n"+(samples.Count>0?$"Latência média {samples.Average():N1} ms · variação min–máx {samples.Min()}–{samples.Max()} ms":"Sem resposta ICMP. Firewall ou destino podem bloquear ping; isso não comprova falha de internet.")+"\nAmostra sob demanda; não comprova defeito permanente nem throughput.";
        }
        catch(OperationCanceledException) { Result="Teste interrompido ou prazo de 20 segundos atingido."; }
        catch(Exception ex) when(ex is System.Net.Sockets.SocketException or PingException or InvalidOperationException) { Result="Não foi possível concluir DNS/ICMP. Verifique o destino, a rede e as permissões. Nenhuma configuração foi alterada."; }
        finally { cancellation=null; start?.Refresh(); cancel?.Refresh(); }
    }
}
public sealed partial class MainViewModel
{
    public NetworkLabViewModel NetworkLab { get; } = new();
    public UiCommand OpenWindowsSecurityCommand => new(() => OpenOfficial("windowsdefender:"));
    public UiCommand OpenWindowsUpdateCommand => new(() => OpenOfficial("ms-settings:windowsupdate"));
    public UiCommand OpenStartupSettingsCommand => new(() => OpenOfficial("ms-settings:startupapps"));
    public static bool IsOfficialDestination(string value) => value is "windowsdefender:" or "ms-settings:windowsupdate" or "ms-settings:startupapps";
    private void OpenOfficial(string uri)
    {
        if(!IsOfficialDestination(uri)) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute=true }); Status="Ferramenta oficial aberta. Ações e resultados são gerenciados pelo Windows; nenhum reparo foi declarado concluído."; }
        catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException) { Status="Não foi possível abrir a ferramenta do Windows. Abra-a pelas Configurações do sistema."; }
    }
}
