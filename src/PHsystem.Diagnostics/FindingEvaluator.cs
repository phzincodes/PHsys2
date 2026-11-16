using System.Globalization;
using PHsystem.Core;

namespace PHsystem.Diagnostics;

/// <summary>Versioned conservative rules. Missing evidence does not become a diagnosis.</summary>
public sealed class FindingEvaluator
{
    public const string RuleVersion = "2026.11.1";
    public IReadOnlyList<DiagnosticFinding> Evaluate(DiagnosticSnapshot snapshot)
    {
        var findings = new List<DiagnosticFinding>();
        foreach (var category in snapshot.Categories)
        {
            if (category.State is CollectionState.Error or CollectionState.Unavailable || category.Facts is null
                || category.ExpiresAt <= DateTimeOffset.UtcNow) continue;
            void Add(string title,string description,string evidence,FindingSeverity severity,string action)=>findings.Add(new(title,description,evidence,"Revise o estado informado e o contexto antes de intervir.",severity,category.Confidence,category.Source,snapshot.CapturedAt,action,"Não desative proteções nem aplique comandos não verificados.",false,false));
            if(category.Category=="Estabilidade" && category.Facts.TryGetValue("UnexpectedShutdowns",out var shutdownText) && int.TryParse(shutdownText,out var shutdowns) && shutdowns>0)
                Add("Reinicializações inesperadas registradas","Kernel-Power 41 indica encerramento incompleto, não a causa. A janela pode ser parcial.",$"{shutdowns} registros nos últimos 7 dias.",FindingSeverity.Warning,"Abra Investigar travamentos e compare horários/origens no Visualizador de Eventos.");
            if(category.Category=="Proteção Windows" && category.Facts.GetValueOrDefault("AntivirusEnabled")=="True" && category.Facts.GetValueOrDefault("RealTimeProtectionEnabled")=="False")
                Add("Defender informa proteção em tempo real desativada","Verifique se outra solução é responsável pela proteção. Não foi detectado malware pelo PHsystem.","AntivirusEnabled=True; RealTimeProtectionEnabled=False",FindingSeverity.Warning,"Abra Segurança do Windows e confira o mecanismo ativo.");
            if(category.Category=="Firewall" && category.Facts.GetValueOrDefault("ActiveProfilesEnabled")=="False")
                Add("Revise os perfis de firewall","Um ou mais perfis ativos do firewall Windows estão desabilitados. Pode haver política ou produto de terceiros.","ActiveProfilesEnabled=False",FindingSeverity.Information,"Confira o firewall ativo em Segurança do Windows antes de alterar qualquer política.");
            if (category.Category == "Dispositivos com erro")
                foreach (var fact in category.Facts.Where(f => f.Key.EndsWith("ConfigManagerErrorCode", StringComparison.Ordinal)))
                    if (int.TryParse(fact.Value, out var code) && code != 0)
                        findings.Add(new("Dispositivo reportou erro", "O Windows reportou um código no gerenciador de dispositivos; a causa requer análise contextual.",
                            $"ConfigManagerErrorCode={code}", "O dispositivo pode não funcionar como esperado.", FindingSeverity.Warning,
                            category.Confidence, category.Source, snapshot.CapturedAt, "Consulte o código no Gerenciador de Dispositivos e a documentação oficial do fabricante.",
                            "Não reinstale drivers sem verificar compatibilidade e origem.", false, false));
            if (category.Category != "Armazenamento") continue;
            foreach (var (key, value) in category.Facts.Where(f => f.Key.EndsWith(" TotalBytes", StringComparison.Ordinal)))
            {
                if (!long.TryParse(value, CultureInfo.InvariantCulture, out var total) || total <= 0) continue;
                if (!category.Facts.TryGetValue(key.Replace(" TotalBytes", " FreeBytes", StringComparison.Ordinal), out var freeText)
                    || !long.TryParse(freeText, CultureInfo.InvariantCulture, out var free) || free < 0 || (double)free / total >= .1) continue;
                findings.Add(new("Espaço disponível abaixo de 10%", "A medição local indica pouco espaço disponível no volume.",
                    $"{free} bytes disponíveis de {total} bytes", "Gravações e atualizações podem ficar limitadas.", FindingSeverity.Warning,
                    category.Confidence, category.Source, snapshot.CapturedAt, "Revise o armazenamento e a prévia de temporários antes de excluir arquivos.",
                    "Exclusões podem ser permanentes; não remova dados pessoais sem backup.", false, false));
            }
        }
        return findings;
    }
}
