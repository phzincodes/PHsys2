using System.Globalization;
using PHsystem.Core;
namespace PHsystem.Diagnostics;
public sealed record StabilityEvent(DateTimeOffset At,string Kind,string Provider,int EventId,string Context);
public sealed record StabilityInvestigation(IReadOnlyList<StabilityEvent> Events,string Scope,string Summary);
public static class CrashInvestigator
{
    public static StabilityInvestigation Evaluate(DiagnosticSnapshot? snapshot)
    {
        var category=snapshot?.Categories.FirstOrDefault(c=>c.Category=="Estabilidade");var facts=category?.Facts;
        if(facts is null)return new([],"Não disponível","Faça uma análise para investigar os logs de estabilidade.");
        var parsed=new List<StabilityEvent>();
        foreach(var item in facts.Where(f=>f.Key.StartsWith("Timeline.",StringComparison.Ordinal)&&f.Key.EndsWith(".At",StringComparison.Ordinal)).Take(40))
        {
            var prefix=item.Key[..^2];
            if(!DateTimeOffset.TryParse(item.Value,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var at)
                || !int.TryParse(facts.GetValueOrDefault(prefix+"EventId"),out var id))continue;
            var kind=facts.GetValueOrDefault(prefix+"Kind")??"Evento";var provider=facts.GetValueOrDefault(prefix+"Provider")??"Windows";
            parsed.Add(new(at,kind,provider,id,"Este registro não comprova causa."));
        }
        var events=parsed.OrderByDescending(e=>e.At).Select(e=>e with { Context=e.EventId==41 && e.Provider=="Microsoft-Windows-Kernel-Power" && parsed.Any(other=>other.Provider=="Microsoft-Windows-WHEA-Logger" && Math.Abs((other.At-e.At).TotalMinutes)<=10)
            ? "Há um registro WHEA a até 10 minutos. Proximidade temporal não confirma causa; examine ambos no Visualizador de Eventos."
            : "Use o horário e a origem para investigar no Visualizador de Eventos. Não confirma defeito físico ou causa do travamento." }).ToArray();
        var complete=facts.GetValueOrDefault("SystemReadComplete")=="true" && facts.GetValueOrDefault("ApplicationReadComplete")=="true";
        return new(events,complete?"Janela de 7 dias lida dentro do limite":"Resultado parcial: limite ou log indisponível",
            events.Length==0?"Nenhum evento dos tipos investigados foi exibido. Isso não exclui outros problemas.":$"{events.Length} registros exibidos por horário. Correlação não é causa; eventos WHEA podem representar erros corrigidos.");
    }
}
