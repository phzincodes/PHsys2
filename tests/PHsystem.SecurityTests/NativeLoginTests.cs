using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using PHsystem.Server;
using PHsystem.SharedContracts.V1;
using Xunit;
namespace PHsystem.SecurityTests;
public sealed class NativeLoginTests
{
    [Fact]
    public async Task BrokerRequiresPossessionExpiresAndAllowsOnlyOneConcurrentRedemption()
    {
        var time=new Clock();var broker=new NativeLoginBroker(time);
        var verifier=WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));var challenge=WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var flow=broker.Start(challenge);var request=new NativeRedeemRequest(flow.FlowId,flow.PollToken,verifier);
        Assert.Null(broker.Consume(request));Assert.True(broker.Authorize(flow.FlowId,"fixture-user"));Assert.False(broker.Authorize(flow.FlowId,"attacker"));
        Assert.Throws<InvalidOperationException>(()=>broker.Consume(request with {PollToken=new string('A',43)}));
        Assert.Throws<InvalidOperationException>(()=>broker.Consume(request with {Verifier=new string('A',43)}));
        var winners=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>Task.Run(()=>{try{return broker.Consume(request)=="fixture-user";}catch(InvalidOperationException){return false;}})));
        Assert.Equal(1,winners.Count(v=>v));Assert.Throws<InvalidOperationException>(()=>broker.Consume(request));
        var expired=broker.Start(challenge);time.Now=time.Now.AddMinutes(5);Assert.False(broker.Exists(expired.FlowId));Assert.Throws<InvalidOperationException>(()=>broker.Consume(new(expired.FlowId,expired.PollToken,verifier)));
    }
    private sealed class Clock:TimeProvider {public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now;}
}
