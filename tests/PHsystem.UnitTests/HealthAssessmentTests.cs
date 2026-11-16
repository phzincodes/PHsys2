using System.Collections.Immutable;
using PHsystem.Core;
using Xunit;
namespace PHsystem.UnitTests;
public sealed class HealthAssessmentTests
{
    [Fact]public void InactiveFirewallProfileDoesNotPenalizeProtectedActiveProfiles()
    {
        var assessment=HealthAssessmentEngine.Evaluate(Snapshot(Category("Proteção Windows",("AntivirusEnabled","True"),("RealTimeProtectionEnabled","True"),("AntivirusSignatureAge","0")),Category("Firewall",("AllProfilesEnabled","False"),("ActiveProfilesEnabled","True"))));
        Assert.Equal(100,assessment.Dimensions.Single(d=>d.Key=="security").Percent);
    }
    private static CategoryResult Category(string name,params(string Key,string Value)[] facts)=>new(name,CollectionState.Partial,DateTimeOffset.UtcNow.AddMinutes(5),"Isolated fixture",.9,[],facts.ToImmutableDictionary(x=>x.Key,x=>x.Value));
    private static DiagnosticSnapshot Snapshot(params CategoryResult[] categories)=>new(Guid.NewGuid(),DateTimeOffset.UtcNow,"fixture",categories.ToImmutableArray());
    [Fact] public void MissingOrExpiredEvidenceCannotBecomeApproval()
    {
        var snapshot=Snapshot();var empty=HealthAssessmentEngine.Evaluate(snapshot);Assert.Equal(0,empty.Coverage);Assert.Null(empty.Score);Assert.Equal(0,empty.LowerBound);Assert.Equal(100,empty.UpperBound);
        var expired=Category("Armazenamento",("Volume 1 TotalBytes","100"),("Volume 1 FreeBytes","25")) with {ExpiresAt=DateTimeOffset.UtcNow.AddMinutes(-1)};
        Assert.Equal(0,HealthAssessmentEngine.Evaluate(Snapshot(expired)).Coverage);
    }
    [Fact] public void ObservedStorageIsWeightedAndUnknownDimensionsKeepTheirInterval()
    {
        var assessment=HealthAssessmentEngine.Evaluate(Snapshot(Category("Armazenamento",("Volume 1 TotalBytes","100"),("Volume 1 FreeBytes","1"))));
        Assert.Equal(15,assessment.Coverage);Assert.Equal(0,assessment.LowerBound);Assert.Equal(85,assessment.UpperBound);Assert.Null(assessment.Score);
        Assert.Equal(0,HealthAssessmentEngine.Evaluate(Snapshot(Category("Armazenamento",("Volume 1 TotalBytes","100"),("Volume 1 FreeBytes","101")))).Coverage);
    }
    [Fact] public void EvenAllSupportedChecksDoNotInventAnIntegrityTest()
    {
        var s=Snapshot(Category("Estabilidade",("SystemReadComplete","true"),("ApplicationReadComplete","true"),("UnexpectedShutdowns","0"),("HardwareErrorEvents","0"),("ApplicationCrashEvents","0")),
            Category("Proteção Windows",("AntivirusEnabled","True"),("RealTimeProtectionEnabled","True"),("AntivirusSignatureAge","1")),Category("Firewall",("ActiveProfilesEnabled","True")),
            Category("Armazenamento",("Volume 1 TotalBytes","100"),("Volume 1 FreeBytes","25")),Category("Dispositivos com erro",("Registros exibidos","0")),Category("Rede",("Interfaces detectadas","1"),("Interfaces ativas","1")));
        var score=HealthAssessmentEngine.Evaluate(s);Assert.Equal(85,score.Coverage);Assert.Equal(85,score.LowerBound);Assert.Equal(100,score.UpperBound);Assert.Null(s.HealthScore);Assert.Equal("Avaliação incompleta",score.Band);
    }
    [Fact] public void DisabledDefenderWithAnotherProviderIsUnknownNotAutomaticMalware()
    {var s=Snapshot(Category("Proteção Windows",("AntivirusEnabled","False"),("RealTimeProtectionEnabled","False"),("AntivirusSignatureAge","999")),Category("Firewall",("ActiveProfilesEnabled","True")));Assert.Equal(0,HealthAssessmentEngine.Evaluate(s).Coverage);}
    [Fact] public void WeightConfigurationIsValidated()
    {var invalid=HealthAssessmentEngine.DefaultWeights.SetItem("security",0);Assert.Throws<ArgumentException>(()=>HealthAssessmentEngine.Evaluate(Snapshot(),weights:invalid));}
}
