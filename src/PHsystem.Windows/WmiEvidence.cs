using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Management;

namespace PHsystem.Windows;

internal static class WmiEvidence
{
    public static IReadOnlyDictionary<string, string> Read(string scope, string query, string[] fields, CancellationToken token)
    {
        using var searcher = new ManagementObjectSearcher(scope, query);
        searcher.Options.Timeout = TimeSpan.FromSeconds(5);
        searcher.Options.ReturnImmediately = false;
        using var rows = searcher.Get();
        var facts = new Dictionary<string, string>();
        var index = 0;
        foreach (ManagementObject row in rows)
        {
            using (row)
            {
                token.ThrowIfCancellationRequested();
                if (++index > 50) { facts["Limite"] = "Exibidos os primeiros 50 registros."; break; }
                foreach (var field in fields)
                {
                    var value = Convert.ToString(row[field], CultureInfo.InvariantCulture) ?? "Indisponível";
                    facts[$"{index:D2} · {field}"] = value.Length > 512 ? value[..512] : value;
                }
            }
        }
        facts["Escopo"] = "Leitura parcial de WMI. Ausência de registros não comprova saúde. Serviços automáticos parados podem estar nesse estado por projeto; atualizações instaladas não indicam ausência de pendências.";
        facts["Registros exibidos"] = Math.Min(index, 50).ToString(CultureInfo.InvariantCulture);
        return facts;
    }

    public static IReadOnlyDictionary<string, string> ReadEvents(CancellationToken token)
    {
        var query = new EventLogQuery("System", PathType.LogName,
            "*[System[(Level=1 or Level=2) and TimeCreated[timediff(@SystemTime) <= 604800000]]]") { ReverseDirection = true };
        using var reader = new EventLogReader(query);
        var facts = new Dictionary<string, string>();
        for (var index = 1; index <= 50; index++)
        {
            token.ThrowIfCancellationRequested();
            using var record = reader.ReadEvent(TimeSpan.FromSeconds(2));
            if (record is null) break;
            facts[$"Evento {index:D2}"] = $"{record.ProviderName} · ID {record.Id} · {record.TimeCreated:O}";
        }
        facts["Escopo"] = "Até 50 eventos críticos/erros do log System nos últimos 7 dias, sem mensagens ou dados brutos. Recorrência e causa não são inferidas automaticamente.";
        return facts;
    }
}
