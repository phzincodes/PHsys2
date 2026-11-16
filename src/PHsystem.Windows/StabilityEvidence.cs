using System.Diagnostics.Eventing.Reader;
using System.Globalization;

namespace PHsystem.Windows;
internal static class StabilityEvidence
{
    public static IReadOnlyDictionary<string,string> Read(CancellationToken token)
    {
        var facts=new Dictionary<string,string> { ["WindowDays"]="7" };var entries=new List<(DateTimeOffset At,string Kind,string Provider,int Id)>();
        var shutdowns=0;var whea=0;var crashes=0;
        foreach(var log in new[]{"System","Application"})
        {
            var complete=true;
            try
            {
                var query=new EventLogQuery(log,PathType.LogName,"*[System[(Level=1 or Level=2 or Level=3) and TimeCreated[timediff(@SystemTime) <= 604800000]]]") { ReverseDirection=true };
                using var reader=new EventLogReader(query);
                for(var index=0;index<=500;index++)
                {
                    token.ThrowIfCancellationRequested();using var record=reader.ReadEvent(TimeSpan.FromSeconds(1));if(record is null)break;
                    if(index==500){complete=false;break;}
                    string? kind=null;
                    if(log=="System" && record.Id==41 && record.ProviderName=="Microsoft-Windows-Kernel-Power"){shutdowns++;kind="Reinicialização inesperada";}
                    if(log=="System" && record.ProviderName=="Microsoft-Windows-WHEA-Logger"){whea++;kind="Evento de hardware (WHEA)";}
                    if(log=="Application" && record.Id==1000 && record.ProviderName=="Application Error"){crashes++;kind="Falha de aplicativo";}
                    if(kind is not null && record.TimeCreated is { } created)entries.Add((new DateTimeOffset(created),kind,record.ProviderName??log,record.Id));
                }
            }
            catch(Exception ex) when(ex is EventLogException or UnauthorizedAccessException or System.Security.SecurityException)
            {complete=false;facts[log+"Unavailable"]="Acesso ou log indisponível. Não significa ausência de eventos.";}
            facts[log+"ReadComplete"]=complete?"true":"false";
        }
        facts["UnexpectedShutdowns"]=shutdowns.ToString(CultureInfo.InvariantCulture);facts["HardwareErrorEvents"]=whea.ToString(CultureInfo.InvariantCulture);facts["ApplicationCrashEvents"]=crashes.ToString(CultureInfo.InvariantCulture);
        var shown=0;
        foreach(var entry in entries.OrderByDescending(e=>e.At).Take(40))
        {var prefix=$"Timeline.{shown++:D2}.";facts[prefix+"At"]=entry.At.ToUniversalTime().ToString("O");facts[prefix+"Kind"]=entry.Kind;facts[prefix+"Provider"]=entry.Provider;facts[prefix+"EventId"]=entry.Id.ToString(CultureInfo.InvariantCulture);}
        facts["Escopo"]="Últimos 7 dias, até 500 erros/avisos por log System/Application e 40 itens visuais. Sem mensagens, caminhos ou dumps. Proximidade temporal é correlação, não causa; WHEA pode ser erro corrigido. Eventos 6008 não são somados a 41 para evitar dupla contagem.";
        return facts;
    }
}
