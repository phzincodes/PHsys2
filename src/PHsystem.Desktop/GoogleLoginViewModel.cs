using System.Security.Cryptography;
using System.Text;
using PHsystem.SharedContracts.V1;
namespace PHsystem.Desktop;
public sealed partial class AuthenticationViewModel
{
    private CancellationTokenSource? googleCancellation;
    public bool IsGooglePending=>googleCancellation is not null;
    public UiCommand GoogleLoginCommand=>new(()=>_ = RunAsync(GoogleLoginAsync),()=>!busy);
    public UiCommand CancelGoogleCommand=>new(()=>googleCancellation?.Cancel());
    private async Task GoogleLoginAsync()
    {
        if(!AcceptTerms)throw new InvalidOperationException("Confirme os termos para continuar com Google.");
        if(!await connection.EnsureAsync())throw new InvalidOperationException(ApiConnectionService.UnavailableMessage);
        var providers=await client.GetAsync<ExternalProvidersDto>("/api/v1/auth/providers");
        if(!providers.NativeGoogle)throw new InvalidOperationException("Login Google para Windows não configurado neste ambiente. Use sua conta convencional ou continue no diagnóstico local.");
        using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(5));googleCancellation=timeout;Changed(nameof(IsGooglePending));
        try
        {
            string B64(byte[] data)=>Convert.ToBase64String(data).TrimEnd('=').Replace('+','-').Replace('/','_');
            var verifier=B64(RandomNumberGenerator.GetBytes(32));var challenge=B64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            var flow=await client.SendResultAsync<NativeStartResponse>("/api/v1/auth/native/start",new NativeStartRequest(challenge));
            var origin=new Uri(client.ApiAddress);var browser=new Uri(origin,"/api/v1/auth/google?acceptTerms=true&flow="+flow.FlowId.ToString("D"));
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(browser.AbsoluteUri){UseShellExecute=true});
            Status="Continue no navegador do sistema. Nenhuma senha Google é recebida pelo PHsystem. Você pode cancelar aqui.";
            while(DateTimeOffset.UtcNow<flow.ExpiresAt)
            {
                await Task.Delay(TimeSpan.FromSeconds(8),timeout.Token);
                var result=await client.SendResultAsync<NativeRedeemResponse>("/api/v1/auth/native/redeem",new NativeRedeemRequest(flow.FlowId,flow.PollToken,verifier,RememberSession));
                if(result.Status=="pending")continue;
                if(result.Status!="authenticated")throw new InvalidOperationException("O servidor não confirmou a autenticação.");
                await client.GetAsync<System.Text.Json.JsonElement>("/api/v1/account/me");client.RememberSession(RememberSession);
                Status="Autenticado pelo Google.";Authenticated?.Invoke(this,EventArgs.Empty);return;
            }
            Status="Autenticação Google expirada. Tente novamente.";
        }
        catch(System.ComponentModel.Win32Exception) {Status="Não foi possível abrir o navegador padrão. Nenhuma autenticação foi simulada.";}
        catch(OperationCanceledException) {Status="Autenticação Google cancelada ou expirada.";}
        finally {googleCancellation=null;Changed(nameof(IsGooglePending));}
    }
}
