using Microsoft.Extensions.Configuration;
using PHsystem.Server;
using Xunit;
namespace PHsystem.SecurityTests;
public sealed class EmailLinkTests
{
    [Fact]
    public void LinksKeepSecretsOutOfRequestUrlsAndRejectProductionLoopback()
    {
        var settings=new Dictionary<string,string?> { ["Email:PublicBaseUrl"]="https://localhost:5178/", ["ASPNETCORE_ENVIRONMENT"]="Development" };
        var config=new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var body=AccountEmailContent.Create("fixture-id","a+b/c=",false,config);
        var address=new Uri(body.Split('\n').Single(l=>l.StartsWith("Confirmar conta: "))["Confirmar conta: ".Length..]);
        Assert.Equal("",address.Query);Assert.Equal("/confirmar",address.AbsolutePath);Assert.Contains("token=a%2Bb%2Fc%3D",address.Fragment,StringComparison.OrdinalIgnoreCase);
        config["ASPNETCORE_ENVIRONMENT"]="Production";
        Assert.Throws<InvalidOperationException>(()=>AccountEmailContent.Create("id","token",true,config));
        config["Email:PublicBaseUrl"]="https://accounts.example.com/";
        Assert.Contains("https://accounts.example.com/redefinir#",AccountEmailContent.Create("id","token",true,config));
    }
}
