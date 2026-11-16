using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;

namespace PHsystem.Windows;

internal static class SecurityEvidence
{
    public static IReadOnlyDictionary<string,string> Defender(CancellationToken token)
    {
        // Deliberately omit ComputerID, paths, threat resources and device identifiers.
        string[] fields=["AntivirusEnabled","RealTimeProtectionEnabled","AMServiceEnabled","AntivirusSignatureAge","AntivirusSignatureLastUpdated","QuickScanAge","FullScanAge"];
        using var searcher=new ManagementObjectSearcher(@"root\Microsoft\Windows\Defender","SELECT "+string.Join(",",fields)+" FROM MSFT_MpComputerStatus");
        searcher.Options.Timeout=TimeSpan.FromSeconds(5);searcher.Options.ReturnImmediately=false;
        using var rows=searcher.Get();
        foreach(ManagementObject row in rows) using(row)
        {
            token.ThrowIfCancellationRequested();var facts=new Dictionary<string,string>();
            foreach(var field in fields) facts[field]=Convert.ToString(row[field],CultureInfo.InvariantCulture)??"Indisponível";
            facts["Escopo"]="Estado informado pelo Microsoft Defender. Outro antivírus pode estar ativo. Não é detecção própria de malware nem atestado de ausência de ameaças.";
            return facts;
        }
        throw new InvalidOperationException("Microsoft Defender não expôs informações neste ambiente.");
    }
    public static IReadOnlyDictionary<string,string> Firewall(CancellationToken token)
    {
        var type=Type.GetTypeFromProgID("HNetCfg.FwPolicy2")??throw new InvalidOperationException("Firewall API indisponível.");
        dynamic policy=Activator.CreateInstance(type)??throw new InvalidOperationException("Firewall API indisponível.");
        try
        {
            var facts=new Dictionary<string,string>();var all=true;var activeEnabled=true;int profiles=policy.CurrentProfileTypes;
            foreach(var (id,name) in new[]{(1,"Domain"),(2,"Private"),(4,"Public")})
            {token.ThrowIfCancellationRequested();bool enabled=policy.FirewallEnabled[id];facts[name+"Enabled"]=enabled.ToString();all&=enabled;if((profiles&id)!=0)activeEnabled&=enabled;}
            facts["AllProfilesEnabled"]=all.ToString();facts["ActiveProfilesEnabled"]=profiles>0&&(profiles&~7)==0?activeEnabled.ToString():"Indisponível";facts["Escopo"]="Configuração de três perfis e estado dos perfis ativos via INetFwPolicy2. Não verifica regras individuais, tráfego nem firewall de terceiros.";return facts;
        }
        finally { if(Marshal.IsComObject(policy))Marshal.FinalReleaseComObject(policy); }
    }
}
