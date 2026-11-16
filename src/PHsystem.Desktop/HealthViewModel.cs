using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using PHsystem.Core;
using PHsystem.Diagnostics;
namespace PHsystem.Desktop;
public sealed partial class MainViewModel
{
    private HealthAssessment? health;
    public ObservableCollection<HealthDimension> HealthDimensions { get; }=[];
    public ObservableCollection<StabilityEvent> StabilityEvents { get; }=[];
    public string HealthLabel=>health?.Label??"Aguardando análise";
    public string HealthCoverage=>health is null?"Sem dados":$"{health.Coverage}% dos pesos avaliados · confiança nas leituras usadas: {health.Confidence:P0}";
    public string HealthExplanation=>"Indicador de verificações, não garantia de saúde ou ausência de malware. Pontos não verificados permanecem na faixa possível; integridade DISM/SFC ainda não integrada.";
    public string StabilitySummary { get; private set; }="Aguardando análise";
    public string StabilityScope { get; private set; }="";
    private void UpdateHealthPresentation()
    {
        health=snapshots.Current is { } current?HealthAssessmentEngine.Evaluate(current):null;
        HealthDimensions.Clear();if(health is not null)foreach(var dimension in health.Dimensions)HealthDimensions.Add(dimension);
        var stability=CrashInvestigator.Evaluate(snapshots.Current);StabilityEvents.Clear();foreach(var item in stability.Events)StabilityEvents.Add(item);
        StabilitySummary=stability.Summary;StabilityScope=stability.Scope;
        foreach(var property in new[]{nameof(HealthLabel),nameof(HealthCoverage),nameof(StabilitySummary),nameof(StabilityScope)})Changed(property);
    }
    public UiCommand ExportSafeReportCommand=>new(()=>_ = RunAsync(async token=>
    {
        if(snapshots.Current is not { } snapshot)throw new InvalidOperationException("Faça uma análise antes de exportar.");
        var dialog=new Microsoft.Win32.SaveFileDialog { Filter="Relatório HTML|*.html",FileName="phsystem-relatorio-sanitizado.html" };
        if(dialog.ShowDialog()!=true)return;
        await File.WriteAllTextAsync(dialog.FileName,SafeDiagnosticReport.Create(snapshot),Encoding.UTF8,token);
        Status="Relatório reduzido exportado. Não inclui hardware, hostname, IP, caminhos, seriais, eventos brutos ou inventário. Revise o arquivo antes de compartilhar.";
    }),()=>!IsBusy&&HasSnapshot);
}
public static class SafeDiagnosticReport
{
    public static string Create(DiagnosticSnapshot snapshot)
    {
        var assessment=HealthAssessmentEngine.Evaluate(snapshot);
        var b=new StringBuilder("<!doctype html><html lang=pt-BR><meta charset=utf-8><meta name=viewport content=\"width=device-width,initial-scale=1\"><meta http-equiv=Content-Security-Policy content=\"default-src 'none'; style-src 'unsafe-inline'\"><title>PHsystem — relatório reduzido</title><style>body{font:16px/1.7 system-ui;background:#07080b;color:#f3f4f6;max-width:850px;margin:40px auto;padding:24px}article{padding:20px;border:1px solid #30343d;margin:16px 0}h1{color:#ff3347}</style><h1>Relatório de verificações</h1>");
        b.Append("<p>Coleta UTC: ").Append(snapshot.CapturedAt.UtcDateTime.ToString("u")).Append("</p><h2>").Append(WebUtility.HtmlEncode(assessment.Label)).Append("</h2><p>").Append(assessment.Coverage).Append("% dos pesos avaliados. Não é garantia contra defeitos ou malware.</p>");
        foreach(var dimension in assessment.Dimensions)b.Append("<article><h2>").Append(WebUtility.HtmlEncode(dimension.Name)).Append("</h2><p>").Append(WebUtility.HtmlEncode(dimension.Display)).Append(" · ").Append(WebUtility.HtmlEncode(dimension.Status)).Append("</p></article>");
        b.Append("<p>Regra ").Append(assessment.RuleVersion).Append(". Sem hardware, identificadores, caminhos ou logs brutos. Relatório local autodeclarado, sem selo de verificação remota.</p></html>");
        return b.ToString();
    }
}
