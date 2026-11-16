using System.Collections.ObjectModel;
using System.Net.Http;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Desktop;

public sealed record FeedOption(string Key, string Label);
public sealed partial class CommunityViewModel
{
    public Guid? ParentReplyId { get; private set; }
    public string ReplyContext => ParentReplyId is null ? "Respondendo à pergunta" : "Respondendo à resposta selecionada";
    public UiCommand ReplyToSelectedCommand => SocialCommand(nameof(ReplyToSelectedCommand), () =>
    {
        if (SelectedReply is null || SelectedReply.Depth >= 4) throw new InvalidOperationException("Selecione uma resposta com menos de quatro níveis.");
        ParentReplyId = SelectedReply.Id; Changed(nameof(ReplyContext)); return Task.CompletedTask;
    });
    public UiCommand ReplyToPostCommand => SocialCommand(nameof(ReplyToPostCommand), () => { ParentReplyId = null; Changed(nameof(ReplyContext)); return Task.CompletedTask; });
    public UiCommand ClearCategoryCommand => SocialCommand(nameof(ClearCategoryCommand), async () => { FeedCategory = ""; Changed(nameof(FeedCategory)); await LoadFeedAsync(true); });
    private bool feedLoading;
    public bool IsFeedLoading { get => feedLoading; private set { feedLoading = value; Changed(); } }
    private int nextPage = 1;
    private bool hasMore;
    private bool feedLoaded;
    private int feedTabIndex;
    private string feedStatus = "Carregue as publicações da comunidade. Nenhum post de demonstração é exibido.";
    public string FeedStatus { get => feedStatus; private set { feedStatus = value; Changed(); } }
    public bool HasMore { get => hasMore; private set { hasMore = value; Changed(); } }
    public bool FeedLoaded { get => feedLoaded; private set { feedLoaded = value; Changed(); } }
    public int FeedTabIndex { get => feedTabIndex; set { feedTabIndex = value; Changed(); } }
    public FeedOption[] FeedSorts { get; } = [new("recent", "Recentes"), new("relevant", "Relevantes"), new("followed", "Seguidos"), new("saved", "Salvos"), new("mine", "Meus posts")];
    public FeedOption SelectedFeedSort { get; set; } = new("recent", "Recentes");
    public string FeedCategory { get; set; } = "";
    public string ProfileName { get; set; } = "";
    public string ProfileBio { get; set; } = "";
    private PublicProfileDto? profile;
    public PublicProfileDto? Profile { get => profile; private set { profile = value; Changed(); Changed(nameof(ProfileHealthLabel)); } }
    public ObservableCollection<NotificationDto> Notifications { get; } = [];
    public ObservableCollection<string> FollowedCategories { get; } = [];
    public string NotificationSummary => Notifications.Count == 0 ? "Nenhuma notificação carregada." : $"{Notifications.Count(n => !n.Read)} não lidas";
    private readonly Dictionary<string, UiCommand> socialCommands = [];
    private UiCommand SocialCommand(string name, Func<Task> action) => socialCommands.TryGetValue(name, out var command) ? command : socialCommands[name] = new(() => _ = Run(action));
    public UiCommand LoadFeedCommand => SocialCommand(nameof(LoadFeedCommand), () => LoadFeedAsync(true));
    public UiCommand MoreFeedCommand => SocialCommand(nameof(MoreFeedCommand), () => HasMore ? LoadFeedAsync(false) : Task.CompletedTask);
    public UiCommand NewPostCommand => SocialCommand(nameof(NewPostCommand), () => { EditingPostId = null; PostTitle = ""; PostBody = ""; PostKind="question"; submissionId=Guid.NewGuid(); Changed(nameof(PostKind)); Changed(nameof(PostTitle)); Changed(nameof(PostBody)); FeedTabIndex = 2; return Task.CompletedTask; });
    public UiCommand SavePostCommand => SocialCommand(nameof(SavePostCommand), async () =>
    {
        if (EditingPostId is { } id) await client.SendAsync(HttpMethod.Put, $"/api/v1/community/posts/{id}", new PostRequest(PostTitle, PostBody, PostCategory,PostKind,editingVersion));
        else await client.SendAsync(HttpMethod.Post, "/api/v1/community/posts", new PostRequest(PostTitle, PostBody, PostCategory,PostKind,ClientRequestId:submissionId));
        Status = "API recebeu a publicação. Conteúdo sinalizado aguarda moderação.";
        if(EditingPostId is null&&client.UserId.Length>0){try{drafts.Delete(client.UserId);}catch(Exception ex)when(ex is System.IO.IOException or UnauthorizedAccessException){Status+=" O rascunho local não pôde ser removido.";}}submissionId=Guid.NewGuid();
        FeedTabIndex = 0; EditingPostId = null; await LoadFeedAsync(true);
    });
    private Guid? EditingPostId { get; set; }
    private int? editingVersion;
    public UiCommand EditSelectedCommand => SocialCommand(nameof(EditSelectedCommand), async () =>
    {
        if (SelectedPost is not { IsOwner: true } selected) throw new InvalidOperationException("Selecione uma publicação sua.");
        var post=await client.GetAsync<PostDto>($"/api/v1/community/posts/{selected.Id}");editingVersion=post.Version;
        EditingPostId = post.Id; PostTitle = post.Title; PostBody = post.Body; PostCategory = post.Category;PostKind=post.Kind;Changed(nameof(PostKind));
        Changed(nameof(PostTitle)); Changed(nameof(PostBody)); Changed(nameof(PostCategory)); FeedTabIndex = 2;
    });
    public bool DeletePostConsent { get; set; }
    public UiCommand DeleteSelectedCommand => SocialCommand(nameof(DeleteSelectedCommand), async () =>
    {
        if (!DeletePostConsent || SelectedPost is not { IsOwner: true } post) throw new InvalidOperationException("Confirme a remoção de uma publicação sua.");
        await client.SendAsync(HttpMethod.Delete, $"/api/v1/community/posts/{post.Id}");
        SelectedPost = null; DeletePostConsent = false; Changed(nameof(DeletePostConsent)); FeedTabIndex = 0; await LoadFeedAsync(true);
    });
    public UiCommand FollowCategoryCommand => SocialCommand(nameof(FollowCategoryCommand), async () =>
    {
        if (string.IsNullOrWhiteSpace(FeedCategory)) throw new InvalidOperationException("Selecione uma categoria.");
        await client.SendAsync(HttpMethod.Put, $"/api/v1/community/categories/{Uri.EscapeDataString(FeedCategory ?? "")}/follow", new SelectionRequest(true));
        await LoadFollowing(); Status = "Categoria seguida.";
    });
    public UiCommand UnfollowCategoryCommand => SocialCommand(nameof(UnfollowCategoryCommand), async () =>
    {
        if (string.IsNullOrWhiteSpace(FeedCategory)) throw new InvalidOperationException("Selecione uma categoria.");
        await client.SendAsync(HttpMethod.Put, $"/api/v1/community/categories/{Uri.EscapeDataString(FeedCategory ?? "")}/follow", new SelectionRequest(false));
        await LoadFollowing(); Status = "Categoria removida dos seguidos.";
    });
    private async Task LoadFollowing() { var rows = await client.GetAsync<string[]>("/api/v1/community/following"); FollowedCategories.Clear(); foreach (var row in rows) FollowedCategories.Add(row); }
    private UiParameterCommand? upvote, downvote, savePost, openPost, readNotification;
    public UiParameterCommand UpvoteCommand => upvote ??= new(value => { if (value is PostDto post) _ = Vote(post, post.MyVote == 1 ? 0 : 1); }, value => IsReady && value is PostDto { IsOwner: false });
    public UiParameterCommand DownvoteCommand => downvote ??= new(value => { if (value is PostDto post) _ = Vote(post, post.MyVote == -1 ? 0 : -1); }, value => IsReady && value is PostDto { IsOwner: false });
    private Task Vote(PostDto post, int value) => Run(async () => { await client.SendAsync(HttpMethod.Put, $"/api/v1/community/posts/{post.Id}/vote", new VoteRequest(value)); await RefreshReactionAsync(post.Id); Status = value == 1 ? "Curtida confirmada pelo servidor." : value == 0 ? "Reação retirada pelo servidor." : "Voto confirmado pelo servidor."; });
    public UiParameterCommand ToggleSaveCommand => savePost ??= new(value => { if (value is PostDto post) _ = Run(async () => { await client.SendAsync(HttpMethod.Put, $"/api/v1/community/posts/{post.Id}/saved", new SelectionRequest(!post.Saved)); await RefreshReactionAsync(post.Id); Status = "Estado salvo confirmado pelo servidor."; }); }, _ => IsReady);
    private async Task RefreshReactionAsync(Guid id)
    {
        var state = await client.GetAsync<PostReactionDto>($"/api/v1/community/posts/{id}/reaction");
        if (state.Id != id) throw new InvalidOperationException("O servidor não confirmou a publicação correta.");
        PostDto Apply(PostDto item) => item with { Score = state.Score, Likes = state.Likes, MyVote = state.MyVote, Followed=state.Followed,Saved = state.Saved, ReplyCount = state.ReplyCount, Solved = state.Solved };
        var index = Posts.ToList().FindIndex(item => item.Id == id);
        if (index >= 0) Posts[index] = Apply(Posts[index]);
        if (SelectedPost?.Id == id) SelectedPost = Apply(SelectedPost);
    }
    private void RefreshReactionCommands() { upvote?.Refresh(); downvote?.Refresh(); savePost?.Refresh(); }
    public UiParameterCommand OpenPostCommand => openPost ??= new(value =>
    {
        if (value is not PostDto post) return;
        SelectedPost = post; ParentReplyId = null; Changed(nameof(ReplyContext)); FeedTabIndex = 1; Replies.Clear(); LoadRepliesCommand.Execute(null);
    });
    public UiCommand LoadProfileCommand => SocialCommand(nameof(LoadProfileCommand), async () =>
    { Profile = await client.GetAsync<PublicProfileDto>("/api/v1/community/profiles/by-name/" + Uri.EscapeDataString(ProfileName)); Status = "Perfil público carregado; hardware é somente o compartilhado explicitamente."; });
    public UiCommand SaveProfileCommand => SocialCommand(nameof(SaveProfileCommand), async () => { await client.SendAsync(HttpMethod.Put, "/api/v1/community/profile", new ProfileRequest(ProfileBio)); Status = "Descrição pública atualizada pela API."; });
    public UiCommand LoadNotificationsCommand => SocialCommand(nameof(LoadNotificationsCommand), async () =>
    {
        var rows = await client.GetAsync<NotificationDto[]>("/api/v1/community/notifications");
        Notifications.Clear(); foreach (var row in rows) Notifications.Add(row); Changed(nameof(NotificationSummary));
        Status = "Notificações carregadas da sua conta.";
    });
    public UiParameterCommand ReadNotificationCommand => readNotification ??= new(value =>
    { if (value is NotificationDto note) _ = Run(async () => { await client.SendAsync(HttpMethod.Put, $"/api/v1/community/notifications/{note.Id}/read"); var i = Notifications.IndexOf(note); if (i >= 0) Notifications[i] = note with { Read = true }; Changed(nameof(NotificationSummary)); }); });
    private async Task LoadFeedAsync(bool reset)
    {
        var page = reset ? 1 : nextPage;
        FeedStatus = "Carregando publicações…";
        IsFeedLoading = true;
        try
        {
        var result = await client.GetAsync<FeedPage>($"/api/v1/community/feed?page={page}&sort={Uri.EscapeDataString(SelectedFeedSort.Key)}&search={Uri.EscapeDataString(Search)}&category={Uri.EscapeDataString(FeedCategory ?? "")}&kind={Uri.EscapeDataString(FeedKind)}&solved={OnlySolved.ToString().ToLowerInvariant()}&unanswered={OnlyUnanswered.ToString().ToLowerInvariant()}");
        if (reset) Posts.Clear();
        foreach (var row in result.Items) if (!Posts.Any(p => p.Id == row.Id)) Posts.Add(row);
        nextPage = result.NextPage; HasMore = result.HasMore; FeedLoaded = true;
        FeedStatus = Posts.Count == 0 ? "Nenhuma publicação encontrada. Você pode iniciar uma pergunta." : $"{Posts.Count} publicações reais carregadas";
        } finally { IsFeedLoading = false; }
    }
}
