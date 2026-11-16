using System.Collections.Immutable;
using PHsystem.Core;
using PHsystem.Diagnostics;
using Xunit;
namespace PHsystem.UnitTests;
public sealed class CrashInvestigatorTests
{
    [Fact]public void NearbyEventsAreContextNotProvenCauseAndMalformedEventsAreIgnored()
    {
        var now=DateTimeOffset.UtcNow;var facts=new Dictionary<string,string>{["Timeline.00.At"]=now.ToString("O"),["Timeline.00.EventId"]="41",["Timeline.00.Provider"]="Microsoft-Windows-Kernel-Power",["Timeline.00.Kind"]="Reinicialização inesperada",
            ["Timeline.01.At"]=now.AddMinutes(-1).ToString("O"),["Timeline.01.EventId"]="17",["Timeline.01.Provider"]="Microsoft-Windows-WHEA-Logger",["Timeline.02.At"]="bad",["Timeline.02.EventId"]="bad"};
        var s=new DiagnosticSnapshot(Guid.NewGuid(),now,"fixture",[new("Estabilidade",CollectionState.Partial,now.AddMinutes(5),"Fixture",.9,[],facts.ToImmutableDictionary())]);
        var investigation=CrashInvestigator.Evaluate(s);Assert.Equal(2,investigation.Events.Count);Assert.Contains("não confirma causa",investigation.Events[0].Context);Assert.Contains("parcial",investigation.Scope);
    }
}
