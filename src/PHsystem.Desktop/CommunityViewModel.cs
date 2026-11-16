using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using PHsystem.Core;
using PHsystem.Diagnostics;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Desktop;

public sealed class ShareField(string key, string value) : ObservableObject
{
    private bool selected;
    public string Key => key;
    public string Value => value;
    public string Label => key switch { "cpu" => "Processador", "gpu" => "Vídeo", "ramTotal" => "Memória total", "os" => "Sistema operacional", "storageAggregate" => "Armazenamento", "driverVersions" => "Drivers", _ => key };
    public string DisplayValue => key == "ramTotal" && long.TryParse(value, out var bytes) ? DisplayFormat.Bytes(bytes) : value;
    public bool Selected { get => selected; set { selected = value; Changed(); } }
}

public sealed partial class CommunityViewModel : ObservableObject, IDisposable
{
    private readonly Dictionary<string, UiCommand> legacyCommands = [];
    private UiCommand LegacyCommand(string name, Action action) => legacyCommands.TryGetValue(name, out var command) ? command : legacyCommands[name] = new(action);
    private readonly CommunityClient client;
    private readonly ApiConnectionService connection;
    private readonly SnapshotStore snapshots;
    private readonly bool ownsClient;
    private int sessionGeneration;
    private string onlineAccountId="";
    public CommunityViewModel(SnapshotStore snapshots, CommunityClient? sharedClient = null, ApiConnectionService? connection = null)
    {
        this.snapshots = snapshots; ownsClient = sharedClient is null;
        client = sharedClient ?? new(); this.connection = connection ?? new(client, ApiEndpointConfiguration.Installed()); client.SessionEnded += EndSession; client.SessionStarted += StartSession;
        this.connection.Changed += ConnectionChanged;
        if (client.Username.Length > 0) StartSession(this, EventArgs.Empty);
    }
    private void StartSession(object? sender, EventArgs e)
    {
        if (onlineAccountId!=client.UserId || Username != client.Username) ClearPrivateViews();
        onlineAccountId=client.UserId;
        Username = client.Username; ApiUrl = client.ApiAddress; ProfileName = client.Username;
        Changed(nameof(Username)); Changed(nameof(ApiUrl)); Changed(nameof(ProfileName));
    }
    private void EndSession(object? sender, EventArgs e) { sessionGeneration++; ClearPrivateViews(); }
    private void ClearPrivateViews()
    {
        Posts.Clear(); Replies.Clear(); Messages.Clear(); ShareFields.Clear(); Notifications.Clear(); FollowedCategories.Clear();
        onlineAccountId="";PublicProfileEnabled=false;HealthSharingMode="none";HealthRankingConsent=false;ProfileAccent="#FF3347";ReplyEditBody="";ReplyDeleteConsent=false;submissionId=Guid.NewGuid();PostKind="question";
        foreach(var property in new[]{nameof(PublicProfileEnabled),nameof(HealthSharingMode),nameof(HealthRankingConsent),nameof(ProfileAccent),nameof(ReplyEditBody),nameof(ReplyDeleteConsent),nameof(PostKind),nameof(PostPreview)})Changed(property);
        SelectedPost = null; SelectedReply = null; Profile = null; ProfileBio = ""; ProfileName = "";
        PostBody = ""; PostTitle = ""; ReplyBody = ""; MessageBody = ""; RecipientId = ""; Email = ""; Username = ""; MfaCode = "";
        ConfirmationToken = ""; ConfirmationUserId = ""; DeleteConsent = false; RegistrationTermsConsent = false;
        FeedLoaded = false; HasMore = false; FeedTabIndex = 0; FeedStatus = "Entre e atualize para carregar publicações.";
        foreach (var property in new[] { nameof(ProfileBio), nameof(ProfileName), nameof(PostBody), nameof(PostTitle), nameof(ReplyBody), nameof(MessageBody), nameof(RecipientId), nameof(Email), nameof(Username), nameof(MfaCode), nameof(NotificationSummary) }) Changed(property);
    }
    private bool busy;
    private PostDto? selectedPost;
    public bool IsReady => !busy;
    public bool IsLoading => busy;
    public bool DeleteConsent { get; set; }
    public bool RegistrationTermsConsent { get; set; }
    private string status = "Desconectado. Nenhum inventário ou credencial é enviado sem uma ação sua.";
    public string Status { get => status; private set { status = value; Changed(); } }
    public string ApiUrl { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string MfaCode { get; set; } = "";
    public string ConfirmationUserId { get; set; } = "";
    public string ConfirmationToken { get; set; } = "";
    public string Search { get; set; } = "";
    public string PostTitle { get; set; } = "";
    public string PostBody { get; set; } = "";
    public string PostCategory { get; set; } = "hardware";
    public string ReplyBody { get; set; } = "";
    public PostDto? SelectedPost { get => selectedPost; set { selectedPost = value; Changed(); } }
    private ReplyDto? selectedReply;
    public ReplyDto? SelectedReply { get=>selectedReply;set{selectedReply=value;Changed();} }
    public string RecipientId { get; set; } = "";
    public string MessageBody { get; set; } = "";
    public bool AcceptMessages { get; set; }
    public string ReportReason { get; set; } = "";
    public string PasswordFeedback { get; private set; } = "8–128 caracteres; maiúscula, minúscula, número e símbolo. Recomendamos 12 ou mais.";
    public ObservableCollection<PostDto> Posts { get; } = [];
    public ObservableCollection<ReplyDto> Replies { get; } = [];
    public ObservableCollection<string> Messages { get; } = [];
    public ObservableCollection<ShareField> ShareFields { get; } = [];
    public string[] Categories { get; } = PHsystem.Community.CommunityPolicy.Categories;
    public UiCommand ConnectCommand => LegacyCommand(nameof(ConnectCommand), () => _ = Run(async () => { if (!await connection.EnsureAsync()) throw new InvalidOperationException(ApiConnectionService.UnavailableMessage); Status = "Conexão com os serviços verificada."; }));
    public UiCommand ConfirmEmailCommand => LegacyCommand(nameof(ConfirmEmailCommand), () => _ = Run(async () => { await client.SendAsync(HttpMethod.Post, "/api/v1/account/confirm-email", new ConfirmEmailRequest(ConfirmationUserId, ConfirmationToken)); Status = "Email confirmado pela API."; }));
    public UiCommand ForgotPasswordCommand => LegacyCommand(nameof(ForgotPasswordCommand), () => _ = Run(async () => { await client.SendAsync(HttpMethod.Post, "/api/v1/account/forgot-password", new ForgotPasswordRequest(Email)); Status = "Se a conta for elegível, a API enviará instruções por email."; }));
    public UiCommand RenewSessionCommand => LegacyCommand(nameof(RenewSessionCommand), () => _ = Run(async () => { await client.SendAsync(HttpMethod.Post, "/api/v1/account/refresh-session"); Status = "Sessão renovada pela API; o prazo de reautenticação administrativa foi preservado."; }));
    public UiCommand LogoutCommand => LegacyCommand(nameof(LogoutCommand), () => _ = Run(async () => { await client.SendAsync(HttpMethod.Post, "/api/v1/account/logout"); Posts.Clear(); Replies.Clear(); Messages.Clear(); client.ClearSession(); Status = "Sessão encerrada."; }));
    public UiCommand SearchPostsCommand => LegacyCommand(nameof(SearchPostsCommand), () => _ = Run(async () =>
    {
        var rows = await client.GetAsync<PostDto[]>("/api/v1/community/posts?search=" + Uri.EscapeDataString(Search));
        Posts.Clear(); Replies.Clear(); foreach (var row in rows) Posts.Add(row);
        Status = $"{rows.Length} posts carregados da API. Conteúdo da comunidade é não confiável; nunca o execute automaticamente.";
    }));
    public UiCommand PublishPostCommand => LegacyCommand(nameof(PublishPostCommand), () => _ = Run(async () =>
    {
        await client.SendAsync(HttpMethod.Post, "/api/v1/community/posts", new PostRequest(PostTitle, PostBody, PostCategory));
        Status = "Post recebido pela API. Conteúdo sinalizado aguarda revisão humana e pode não aparecer na listagem.";
    }));
    public UiCommand LoadRepliesCommand => LegacyCommand(nameof(LoadRepliesCommand), () => _ = Run(async () =>
    {
        if (SelectedPost is null) throw new InvalidOperationException("Selecione uma pergunta.");
        var rows = await client.GetAsync<ReplyDto[]>($"/api/v1/community/posts/{SelectedPost.Id}/replies");
        Replies.Clear(); foreach (var row in rows) Replies.Add(row);
        Status = "Respostas carregadas. Selo técnico é contextual e não garante segurança universal.";
    }));
    public UiCommand ReplyCommand => LegacyCommand(nameof(ReplyCommand), () => _ = Run(async () =>
    {
        if (SelectedPost is null) throw new InvalidOperationException("Selecione uma pergunta.");
        await client.SendAsync(HttpMethod.Post, $"/api/v1/community/posts/{SelectedPost.Id}/replies", new ReplyRequest(ReplyBody, ParentReplyId));
        ParentReplyId = null; Changed(nameof(ReplyContext));
        var rows = await client.GetAsync<ReplyDto[]>($"/api/v1/community/posts/{SelectedPost.Id}/replies");
        Replies.Clear(); foreach (var row in rows) Replies.Add(row);
        Status = "Resposta recebida como não verificada. Conteúdo sinalizado fica em revisão humana.";
    }));
    public UiCommand AcceptSolutionCommand => LegacyCommand(nameof(AcceptSolutionCommand), () => _ = Run(async () =>
    {
        if (SelectedPost is null || SelectedReply is null) throw new InvalidOperationException("Selecione uma pergunta e uma resposta.");
        await client.SendAsync(HttpMethod.Post, $"/api/v1/community/posts/{SelectedPost.Id}/solution/{SelectedReply.Id}");
        await RefreshReactionAsync(SelectedPost.Id);
        for (var i = 0; i < Replies.Count; i++) Replies[i] = Replies[i] with { Accepted = Replies[i].Id == SelectedReply.Id };
        Status = "Solução registrada pela API. Isso não equivale a revisão técnica.";
    }));
    public UiCommand ReportCommand => LegacyCommand(nameof(ReportCommand), () => _ = Run(async () =>
    {
        if (SelectedPost is null) throw new InvalidOperationException("Selecione uma pergunta.");
        await client.SendAsync(HttpMethod.Post, "/api/v1/community/reports", new ReportRequest(SelectedPost.Id, ReportReason));
        Status = "Denúncia recebida para análise humana.";
    }));
    public UiCommand MessagePrivacyCommand => LegacyCommand(nameof(MessagePrivacyCommand), () => _ = Run(async () => { await client.SendAsync(HttpMethod.Put, $"/api/v1/account/direct-messages/{AcceptMessages.ToString().ToLowerInvariant()}"); Status = "Preferência de mensagens confirmada pela API."; }));
    public UiCommand SendMessageCommand => LegacyCommand(nameof(SendMessageCommand), () => _ = Run(async () => { await client.SendAsync(HttpMethod.Post, "/api/v1/community/messages", new MessageRequest(RecipientId, MessageBody)); Status = "Mensagem salva pela API. Não há criptografia ponta a ponta."; }));
    public UiCommand LoadMessagesCommand => LegacyCommand(nameof(LoadMessagesCommand), () => _ = Run(async () =>
    {
        var rows = await client.GetAsync<JsonElement[]>("/api/v1/community/messages");
        Messages.Clear(); foreach (var row in rows) Messages.Add($"{row.GetProperty("createdAt").GetString()} · {row.GetProperty("body").GetString()}");
        Status = $"{rows.Length} mensagens da sua sessão carregadas. Não há criptografia ponta a ponta.";
    }));
    public UiCommand BlockCommand => LegacyCommand(nameof(BlockCommand), () => _ = Run(async () => { await client.SendAsync(HttpMethod.Put, "/api/v1/community/blocks/" + Uri.EscapeDataString(RecipientId)); Status = "Bloqueio registrado."; }));
    public UiCommand PrepareSharingCommand => LegacyCommand(nameof(PrepareSharingCommand), PrepareSharing);
    public UiCommand PublishSharingCommand => LegacyCommand(nameof(PublishSharingCommand), () => _ = Run(async () =>
    {
        var fields = ShareFields.Where(f => f.Selected).ToDictionary(f => f.Key, f => f.Value);
        await client.SendAsync(HttpMethod.Put, "/api/v1/community/hardware-sharing", new HardwareShareRequest(fields));
        Status = $"API confirmou {fields.Count} campos públicos. Os demais permanecem privados.";
    }));
    public UiCommand RevokeSharingCommand => LegacyCommand(nameof(RevokeSharingCommand), () => _ = Run(async () =>
    {
        await client.SendAsync(HttpMethod.Put, "/api/v1/community/hardware-sharing", new HardwareShareRequest(new Dictionary<string, string>()));
        foreach (var item in ShareFields) item.Selected = false;
        Status = "Compartilhamento retirado na API. O snapshot local foi preservado.";
    }));
    public Task LoginAsync(string password) => Run(async () => { await client.SendAsync(HttpMethod.Post, "/api/v1/account/login", new LoginRequest(Username, password, MfaCode)); Status = "Sessão autenticada pela API. Cookies ficam apenas na memória deste processo."; });
    public Task RegisterAsync(string password) => Run(async () => { await client.SendAsync(HttpMethod.Post, "/api/v1/account/register", new RegisterRequest(Username, Email, password, RegistrationTermsConsent)); Status = "Se os dados forem elegíveis, a API enviará confirmação por email. O registro depende de email e triagem de senhas configurados."; });
    public Task ResetPasswordAsync(string password) => Run(async () => { await client.SendAsync(HttpMethod.Post, "/api/v1/account/reset-password", new ResetPasswordRequest(ConfirmationUserId, ConfirmationToken, password)); Status = "Redefinição confirmada pela API. Entre novamente."; });
    public Task DeleteAccountAsync(string password) => Run(async () =>
    {
        if (!DeleteConsent) throw new InvalidOperationException("Marque a confirmação de exclusão da conta antes de prosseguir.");
        await client.SendAsync(HttpMethod.Post, "/api/v1/account/delete", new PasswordConfirmation(password));
        var localDraftRemoved=true;if(client.UserId.Length>0){try{drafts.Delete(client.UserId);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){localDraftRemoved=false;}}
        Posts.Clear(); Replies.Clear(); Messages.Clear();
        client.ClearSession();
        Status = "Exclusão/anonimização confirmada. Auditoria e backups seguem a política de retenção documentada."+(localDraftRemoved?"":" Não foi possível remover o rascunho local protegido; verifique as permissões.");
    });
    public void ValidatePassword(string password)
    {
        PasswordFeedback = $"Tamanho 8–128: {password.Length is >= 8 and <= 128} · Maiúscula: {password.Any(char.IsUpper)} · Minúscula: {password.Any(char.IsLower)} · Número: {password.Any(char.IsDigit)} · Símbolo: {password.Any(c => !char.IsLetterOrDigit(c))}. Recomendamos 12+.";
        Changed(nameof(PasswordFeedback));
    }
    private void PrepareSharing()
    {
        ShareFields.Clear();
        if (snapshots.Current is not { } snapshot) { Status = "Faça uma coleta local antes de escolher campos."; return; }
        void Add(string field, string category, string name)
        {
            var evidence = snapshot.Categories.FirstOrDefault(c => c.Category == category && c.State is CollectionState.Complete or CollectionState.Partial);
            if (evidence?.Facts?.TryGetValue(name, out var value) == true && PHsystem.Community.HardwareSharingPolicy.SafeValue(value)) ShareFields.Add(new(field, value));
        }
        Add("cpu", "Processador", "Modelo"); Add("ramTotal", "Memória", "TotalBytes"); Add("os", "Sistema", "Sistema"); Add("gpu", "GPU", "01 · Name");
        Status = "Prévia dos valores que podem ser públicos. Nada foi enviado; todos começam desmarcados. A publicação substitui a seleção anterior.";
    }
    private async Task Run(Func<Task> action)
    {
        if (busy) return;
        var generation = sessionGeneration;
        busy = true; RefreshReactionCommands(); Changed(nameof(IsReady)); Changed(nameof(IsLoading)); Status = "Aguardando confirmação da API…";
        try { await action(); }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException or JsonException or TaskCanceledException)
        { if (FeedStatus == "Carregando publicações…") FeedStatus = "Feed indisponível. Revise a conexão e tente atualizar."; Status = ex is InvalidOperationException ? ex.Message : "Comunidade indisponível. Verifique a conexão, o serviço e o certificado HTTPS. Nenhum diagnóstico foi enviado automaticamente."; }
        finally { if (generation != sessionGeneration) ClearPrivateViews(); busy = false; RefreshReactionCommands(); Changed(nameof(IsReady)); Changed(nameof(IsLoading)); }
    }
    public void Dispose() { connection.Changed -= ConnectionChanged; client.SessionEnded -= EndSession; client.SessionStarted -= StartSession; if (ownsClient) client.Dispose(); }
}
