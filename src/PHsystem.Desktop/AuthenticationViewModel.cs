using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using PHsystem.Community;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Desktop;

public enum AuthenticationPage { Welcome, Login, Register, ConfirmEmail, Recovery, ResetPassword, Terms, Privacy }

public sealed partial class AuthenticationViewModel : ObservableObject
{
    private readonly CommunityClient client;
    private readonly ApiConnectionService connection;
    private AuthenticationPage page;
    private bool busy;
    private string status = "";
    public event EventHandler? Authenticated;
    public event EventHandler? CloseRequested;
    public UiCommand ReturnToLocalCommand => returnToLocal ??= new(() => { googleCancellation?.Cancel(); CloseRequested?.Invoke(this, EventArgs.Empty); });
    private UiCommand? returnToLocal;
    public AuthenticationViewModel(CommunityClient client, ApiConnectionService? connection = null)
    {
        this.client = client;
        this.connection = connection ?? new(client, ApiEndpointConfiguration.Installed());
        this.connection.Changed += (_, _) => NotifyConnection();
        RetryCommand = new(() => _ = RestoreAsync(), () => !busy);
        ResendEmailCommand = new(() => _ = RunAsync(async () =>
        {
            if (!await this.connection.EnsureAsync() || !CanRecover) throw new InvalidOperationException(ApiConnectionService.UnavailableMessage);
            await client.SendAsync(HttpMethod.Post, "/api/v1/account/resend-confirmation", new ForgotPasswordRequest(Email));
            Status = "Se a conta for elegível, as instruções serão enviadas por e-mail.";
        }), () => !busy);
    }
    public AuthenticationPage Page { get => page; set { page = value; Status = ""; Changed(); Changed(nameof(Title)); NotifyConnection(); } }
    public string Title => Page switch { AuthenticationPage.Login => "Bem-vindo de volta", AuthenticationPage.Register => "Crie sua conta", AuthenticationPage.ConfirmEmail => "Confirme seu e-mail", AuthenticationPage.Recovery => "Recuperar acesso", AuthenticationPage.ResetPassword => "Definir nova senha", AuthenticationPage.Terms => "Termos de desenvolvimento", AuthenticationPage.Privacy => "Sua privacidade", _ => "Seu computador, em foco." };
    public bool IsReady => !busy;
    public bool IsChecking => connection.Current.State == ApiConnectionState.Checking;
    public bool IsConnected => connection.Available;
    public bool CanLogin => !busy && IsConnected;
    public bool CanCreateAccount => !busy && connection.Current.Health?.RegistrationAvailable == true && IsConnected;
    public bool CanRecover => connection.Current.Health?.RecoveryAvailable == true && IsConnected;
    public bool CanSubmit => !busy && IsConnected && (Page is AuthenticationPage.Register ? connection.Current.Health?.RegistrationAvailable == true
        : Page is AuthenticationPage.Recovery or AuthenticationPage.ResetPassword ? CanRecover : true);
    public string ConnectionMessage => IsChecking ? "Verificando os serviços do PHsystem…" : IsConnected ? "Serviços do PHsystem conectados." : connection.Current.State == ApiConnectionState.RateLimited ? "Serviços temporariamente limitados." : connection.Current.State == ApiConnectionState.AccessDenied ? "Acesso aos serviços não autorizado." : ApiConnectionService.UnavailableMessage;
    public string ConnectionDetail => connection.Detail;
    public string Status { get => status; private set { status = value; Changed(); } }
    public UiCommand RetryCommand { get; }
    public UiCommand ResendEmailCommand { get; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string MfaCode { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Token { get; set; } = "";
    public bool AcceptTerms { get; set; }
    public bool RememberSession { get; set; }
    public string PasswordFeedback { get; private set; } = "Use 12+ caracteres. Mínimo: 8, maiúscula, minúscula, número e símbolo.";
    public double PasswordStrength { get; private set; }
    public void ValidatePassword(string password)
    {
        PasswordStrength = new[] { password.Length >= 8, password.Length >= 12, password.Any(char.IsUpper), password.Any(char.IsLower), password.Any(char.IsDigit), password.Any(c => !char.IsLetterOrDigit(c)) }.Count(x => x);
        PasswordFeedback = PasswordValid(password) ? "Formato válido. A API também verifica senhas comprometidas." : "8–128 caracteres · maiúscula · minúscula · número · símbolo";
        Changed(nameof(PasswordFeedback)); Changed(nameof(PasswordStrength));
    }
    public static bool PasswordValid(string password) => password.Length is >= 8 and <= 128 && password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c));

    public Task SubmitAsync(string password, string confirmation) => RunAsync(async () =>
    {
        if (Page is AuthenticationPage.Register or AuthenticationPage.ResetPassword)
        {
            if (!PasswordValid(password)) throw new InvalidOperationException("Revise os requisitos da senha; ela não será truncada.");
            if (password != confirmation) throw new InvalidOperationException("As senhas não coincidem.");
        }
        if (Page == AuthenticationPage.Register)
        {
            if (!AcceptTerms) throw new InvalidOperationException("Leia e aceite os termos e a política de privacidade.");
            if (!CommunityPolicy.ValidUsername(Username)) throw new InvalidOperationException("Use um nome disponível com 3–24 letras ASCII, números ou sublinhado, começando com letra.");
            ValidateEmail();
        }
        if (Page == AuthenticationPage.Recovery) ValidateEmail();
        if (Page == AuthenticationPage.Login && (string.IsNullOrWhiteSpace(Username) || Username.Length > 24 || password.Length is 0 or > 128)) throw new InvalidOperationException("Informe seu nome de usuário e senha.");
        if (!await connection.EnsureAsync()) throw new InvalidOperationException(ApiConnectionService.UnavailableMessage);
        if (Page == AuthenticationPage.Register && connection.Current.Health?.RegistrationAvailable != true) throw new InvalidOperationException("Cadastro temporariamente indisponível. Tente novamente mais tarde.");
        if (Page is AuthenticationPage.Recovery or AuthenticationPage.ResetPassword && !CanRecover) throw new InvalidOperationException("Recuperação temporariamente indisponível.");
        switch (Page)
        {
            case AuthenticationPage.Login:
                await client.SendAsync(HttpMethod.Post, "/api/v1/account/login", new LoginRequest(Username, password, MfaCode, RememberSession));
                await ValidateSessionAsync();
                client.RememberSession(RememberSession);
                Status = "Sessão confirmada.";
                Authenticated?.Invoke(this, EventArgs.Empty);
                break;
            case AuthenticationPage.Register:
                await client.SendAsync(HttpMethod.Post, "/api/v1/account/register", new RegisterRequest(Username, Email, password, AcceptTerms));
                Page = AuthenticationPage.ConfirmEmail;
                Status = "Se os dados forem elegíveis, você receberá instruções por e-mail.";
                break;
            case AuthenticationPage.ConfirmEmail:
                await client.SendAsync(HttpMethod.Post, "/api/v1/account/confirm-email", new ConfirmEmailRequest(UserId, Token));
                Page = AuthenticationPage.Login; Status = "E-mail confirmado. Entre na sua conta.";
                break;
            case AuthenticationPage.Recovery:
                await client.SendAsync(HttpMethod.Post, "/api/v1/account/forgot-password", new ForgotPasswordRequest(Email));
                Page = AuthenticationPage.ResetPassword; Status = "Se a conta for elegível, as instruções serão enviadas por e-mail.";
                break;
            case AuthenticationPage.ResetPassword:
                await client.SendAsync(HttpMethod.Post, "/api/v1/account/reset-password", new ResetPasswordRequest(UserId, Token, password));
                Page = AuthenticationPage.Login; Status = "Senha redefinida pela API.";
                break;
        }
    });
    public Task RestoreAsync() => RunAsync(async () =>
    {
        if ((await connection.CheckAsync()).State != ApiConnectionState.Available) { Status = ""; return; }
        if (!client.RestoreSession()) { Status = ""; return; }
        await ValidateSessionAsync();
        Authenticated?.Invoke(this, EventArgs.Empty);
    });
    public Task RestoreRememberedOnlyAsync() => client.HasRememberedSession ? RestoreAsync() : Task.CompletedTask;
    private async Task ValidateSessionAsync()
    {
        var identity = await client.GetAsync<JsonElement>("/api/v1/account/me");
        if (!identity.TryGetProperty("id", out var id) || string.IsNullOrEmpty(id.GetString()))
        { client.ClearSession(); throw new InvalidDataException("Sessão não confirmada."); }
    }
    private void ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(Email) || Email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(Email, out _)) throw new InvalidOperationException("Informe um e-mail válido.");
    }
    private void NotifyConnection()
    {
        foreach (var name in new[] { nameof(IsReady), nameof(IsChecking), nameof(IsConnected), nameof(CanLogin), nameof(CanCreateAccount), nameof(CanRecover), nameof(CanSubmit), nameof(ConnectionMessage), nameof(ConnectionDetail) }) Changed(name);
        RetryCommand?.Refresh(); ResendEmailCommand?.Refresh();
    }
    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true; NotifyConnection(); Status = "Conectando com segurança…";
        try { await action(); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException)
        { connection.ReportFailure(ex); Status = ApiConnectionService.UnavailableMessage; }
        catch (ApiResponseException ex)
        {
            if ((int)ex.StatusCode >= 500) { connection.ReportFailure(ex); Status = ApiConnectionService.UnavailableMessage; }
            else Status = ex.Message;
        }
        catch (InvalidOperationException ex) { Status = ex.Message; }
        finally { busy = false; NotifyConnection(); }
    }
}
