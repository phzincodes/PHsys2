using System.Collections.Immutable;
using System.Globalization;

namespace PHsystem.Core;

public sealed record HealthDimension(string Key, string Name, int Weight, int? Percent, double Confidence, string Evidence, string Source)
{
    public double? Earned => Percent is { } value ? Weight * value / 100d : null;
    public string Status => Percent is null ? "Não verificado" : Percent >= 85 ? "Bom nas verificações" : Percent >= 50 ? "Atenção" : "Prioridade alta";
    public string Display => Percent is null ? $"{Weight} pontos não avaliados" : $"{Earned:0.#}/{Weight} pontos";
}
public sealed record HealthAssessment(string RuleVersion, DateTimeOffset EvaluatedAt, ImmutableArray<HealthDimension> Dimensions)
{
    public int Coverage => Dimensions.Where(d => d.Percent.HasValue).Sum(d => d.Weight);
    public int LowerBound => (int)Math.Floor(Dimensions.Sum(d => d.Earned ?? 0));
    public int UpperBound => Math.Min(100, (int)Math.Ceiling(Dimensions.Sum(d => d.Earned ?? d.Weight)));
    public int? Score => Coverage == 100 ? LowerBound : null;
    public double Confidence => Coverage == 0 ? 0 : Dimensions.Where(d => d.Percent.HasValue).Sum(d => d.Weight * d.Confidence) / Coverage;
    public string Label => Score is { } score ? $"{score}/100" : Coverage == 0 ? "Sem avaliação suficiente" : $"Faixa possível: {LowerBound}–{UpperBound}/100";
    public string Band => Score is not { } s ? "Avaliação incompleta" : s >= 90 ? "Excelente nas verificações" : s >= 75 ? "Bom" : s >= 50 ? "Atenção" : "Crítico";
}

/// <summary>Versioned policy indicator, not a physical/malware certification.
/// Unknown dimensions keep their whole possible interval; never normalized away.</summary>
public static class HealthAssessmentEngine
{
    public const string RuleVersion = "health-2026.11.1";
    public static readonly ImmutableDictionary<string, int> DefaultWeights = new Dictionary<string,int>
    { ["stability"]=25, ["security"]=25, ["storage"]=15, ["integrity"]=15, ["hardware"]=10, ["network"]=10 }.ToImmutableDictionary();
    public static HealthAssessment Evaluate(DiagnosticSnapshot snapshot, DateTimeOffset? at = null, IReadOnlyDictionary<string,int>? weights = null)
    {
        var now = at ?? DateTimeOffset.UtcNow; weights ??= DefaultWeights;
        if (weights.Count != 6 || DefaultWeights.Keys.Any(k=>!weights.ContainsKey(k)) || weights.Values.Any(v=>v is < 1 or > 50) || weights.Values.Sum()!=100)
            throw new ArgumentException("Os seis pesos devem ser positivos, até 50 e somar 100.",nameof(weights));
        CategoryResult? Category(string name) => snapshot.Categories.FirstOrDefault(c=>c.Category==name && c.State is CollectionState.Partial or CollectionState.Complete
            && c.Facts is { Count: > 0 } && c.ExpiresAt>now && c.Confidence is >=.7 and <=1 && !string.IsNullOrWhiteSpace(c.Source));
        string? Fact(CategoryResult? c,string key)=>c?.Facts?.GetValueOrDefault(key);
        bool Integer(CategoryResult? c,string key,out int n)=>int.TryParse(Fact(c,key),NumberStyles.Integer,CultureInfo.InvariantCulture,out n)&&n>=0;
        var dimensions=new List<HealthDimension>();
        void Add(string key,string name,CategoryResult? c,int? percent,string evidence)=>dimensions.Add(new(key,name,weights[key],percent,c?.Confidence??0,evidence,c?.Source??"Sem fonte disponível"));
        var stability=Category("Estabilidade"); int? stabilityPercent=null;
        var stabilityEvidence="Logs System/Application de sete dias não foram lidos integralmente dentro do limite.";
        if(Fact(stability,"SystemReadComplete")=="true" && Fact(stability,"ApplicationReadComplete")=="true"
            && Integer(stability,"UnexpectedShutdowns",out var shutdowns) && Integer(stability,"HardwareErrorEvents",out var whea) && Integer(stability,"ApplicationCrashEvents",out var crashes))
        { stabilityPercent=Math.Max(0,100-Math.Min(50,Math.Min(shutdowns,4)*15)-Math.Min(35,Math.Min(whea,4)*10)-Math.Min(30,Math.Min(crashes,6)*5));stabilityEvidence=$"7 dias: {shutdowns} eventos Kernel-Power 41, {whea} registros WHEA, {crashes} Application Error 1000. Eventos não comprovam causa."; }
        Add("stability","Estabilidade",stability,stabilityPercent,stabilityEvidence);
        var security=Category("Proteção Windows"); var firewall=Category("Firewall"); int? securityPercent=null;
        var securityEvidence="Microsoft Defender/firewall sem leitura suficiente. Outro antivírus pode estar ativo; consulte Segurança do Windows.";
        if(Fact(security,"AntivirusEnabled")=="True" && bool.TryParse(Fact(security,"RealTimeProtectionEnabled"),out var realtime)
            && Integer(security,"AntivirusSignatureAge",out var age) && age<36500 && bool.TryParse(Fact(firewall,"ActiveProfilesEnabled"),out var enabled))
        { securityPercent=(realtime?45:0)+(age<=3?25:age<=7?10:0)+(enabled?30:0);securityEvidence=$"Defender ativo; proteção em tempo real: {(realtime?"ativada":"desativada")}; assinaturas: {age} dia(s); perfis ativos de firewall: {(enabled?"habilitados":"desabilitados")}. Não atesta ausência de malware."; }
        Add("security","Segurança",security,securityPercent,securityEvidence);
        if(securityPercent.HasValue && firewall is not null) dimensions[^1]=dimensions[^1] with { Confidence=Math.Min(security!.Confidence,firewall.Confidence),Source=security.Source+"; "+firewall.Source };
        var storage=Category("Armazenamento"); var ratios=new List<double>();var invalid=false;
        if(storage?.Facts is { } facts) foreach(var pair in facts.Where(p=>p.Key.EndsWith(" TotalBytes",StringComparison.Ordinal)))
        { if(!long.TryParse(pair.Value,out var total)||total<=0||!facts.TryGetValue(pair.Key.Replace(" TotalBytes"," FreeBytes",StringComparison.Ordinal),out var text)||!long.TryParse(text,out var free)||free<0||free>total) { invalid=true;break; }ratios.Add(free/(double)total); }
        var minimum=ratios.Count>0?ratios.Min():-1;
        Add("storage","Armazenamento",storage,invalid||minimum<0?null:minimum>=.2?100:minimum>=.1?70:minimum>=.03?30:0,
            minimum<0||invalid?"Tamanho/espaço livre ausente ou inválido.":$"Menor espaço livre observado: {minimum:P1}. Considera capacidade, não integridade física/SMART.");
        // An installed update, registry flag or operating-system status is NOT an integrity test.
        Add("integrity","Integridade Windows",null,null,"DISM/SFC verificados ainda não integrados. Nenhum dado de inventário substitui essa verificação.");
        var hardware=Category("Dispositivos com erro");int? hardwarePercent=null;
        if(hardware?.Facts is { } hf && !hf.ContainsKey("Limite") && Integer(hardware,"Registros exibidos",out var devices)) hardwarePercent=devices==0?100:Math.Max(0,100-Math.Min(devices,4)*25);
        Add("hardware","Dispositivos",hardware,hardwarePercent,hardwarePercent is null?"Consulta de erros PnP incompleta.":"Somente erros reportados pelo PnP; não testa defeitos físicos, memória ou temperaturas.");
        var network=Category("Rede");int? networkPercent=null;
        if(Integer(network,"Interfaces detectadas",out var interfaces) && interfaces>0 && Integer(network,"Interfaces ativas",out var active) && active<=interfaces)networkPercent=active>0?100:0;
        Add("network","Enlace de rede",network,networkPercent,"Avalia somente presença de enlace. DNS, internet, latência e throughput não são inferidos; use Network Lab.");
        return new(RuleVersion,now,dimensions.ToImmutableArray());
    }
}
