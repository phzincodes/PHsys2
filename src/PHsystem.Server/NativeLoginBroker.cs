using PHsystem.SharedContracts.V1;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
namespace PHsystem.Server;
public sealed class NativeLoginBroker(TimeProvider time)
{
    private sealed record Grant(string Challenge,byte[] PollHash,DateTimeOffset Expires,string? UserId=null);
    private readonly Dictionary<Guid,Grant> grants=[];
    private readonly object gate=new();
    public NativeStartResponse Start(string challenge)
    {
        if(challenge is null || !Regex.IsMatch(challenge,"^[A-Za-z0-9_-]{43}$"))throw new ArgumentException("PKCE inválido.");
        lock(gate)
        {
            foreach(var key in grants.Where(p=>p.Value.Expires<=time.GetUtcNow()).Select(p=>p.Key).ToArray())grants.Remove(key);
            if(grants.Count>=128)throw new InvalidOperationException("Limite de autenticações em andamento.");
            var flow=Guid.NewGuid();var poll=WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));var expiry=time.GetUtcNow().AddMinutes(5);
            grants.Add(flow,new(challenge,SHA256.HashData(Encoding.UTF8.GetBytes(poll)),expiry));return new(flow,poll,expiry);
        }
    }
    public bool Exists(Guid id) { lock(gate)return grants.TryGetValue(id,out var grant)&&grant.Expires>time.GetUtcNow(); }
    public bool Authorize(Guid id,string userId)
    { lock(gate){if(string.IsNullOrWhiteSpace(userId)||!grants.TryGetValue(id,out var g)||g.Expires<=time.GetUtcNow()||g.UserId is not null)return false;grants[id]=g with {UserId=userId};return true;} }
    // null = still waiting, exception = invalid/expired/replay. Only one caller can consume.
    public string? Consume(NativeRedeemRequest request)
    {
        if(request.PollToken is null||request.PollToken.Length!=43||request.Verifier is null||!Regex.IsMatch(request.Verifier,"^[A-Za-z0-9_-]{43,128}$"))throw new InvalidOperationException("Autenticação inválida.");
        lock(gate)
        {
            if(!grants.TryGetValue(request.FlowId,out var grant)||grant.Expires<=time.GetUtcNow())throw new InvalidOperationException("Autenticação expirada ou já utilizada.");
            var hash=SHA256.HashData(Encoding.UTF8.GetBytes(request.PollToken));var challenge=WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(request.Verifier)));
            if(!CryptographicOperations.FixedTimeEquals(hash,grant.PollHash)||!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(challenge),Encoding.ASCII.GetBytes(grant.Challenge)))throw new InvalidOperationException("Prova de posse inválida.");
            if(grant.UserId is null)return null;grants.Remove(request.FlowId);return grant.UserId;
        }
    }
}
