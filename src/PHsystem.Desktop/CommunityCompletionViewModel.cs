using System.IO;
using System.Net.Http;
using PHsystem.Core;
using PHsystem.SharedContracts.V1;
namespace PHsystem.Desktop;
public sealed partial class CommunityViewModel
{
    private readonly PrivateDraftStore drafts=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PHsystemNext","drafts"));
    private Guid submissionId=Guid.NewGuid();
    public string ProfileHealthLabel=>Profile?.Health is not { } health?"Saúde não compartilhada":(health.Mode=="score"?$"{health.Score}/100":health.Mode=="summary"?$"Faixa {health.Lower}–{health.Upper}/100":"Faixa: "+health.Band)+$" · cobertura {health.Coverage}% · autodeclarado, não verificado";
    public string PostKind {get;set;}="question";
    public FeedOption[] PostKinds {get;}=[new("discussion","Discussão"),new("question","Pergunta"),new("tutorial","Tutorial"),new("suggestion","Sugestão"),new("bug","Relato de bug")];
    public string FeedKind {get;set;}="";
    public bool OnlySolved {get;set;}
    public bool OnlyUnanswered {get;set;}
    public bool PublicProfileEnabled {get;set;}
    public string ProfileAccent {get;set;}="#FF3347";
    public string HealthSharingMode {get;set;}="none";
    public FeedOption[] HealthSharingModes {get;}=[new("none","Não compartilhar"),new("band","Somente faixa"),new("summary","Resumo e cobertura"),new("score","Pontuação completa")];
    public bool HealthRankingConsent {get;set;}
    public string ReplyEditBody {get;set;}="";
    public bool ReplyDeleteConsent {get;set;}
    public string PostPreview=>string.IsNullOrWhiteSpace(PostBody)?"Digite o conteúdo para pré-visualizar.":PostBody;
    public UiCommand PreviewPostCommand=>SocialCommand(nameof(PreviewPostCommand),()=>{Changed(nameof(PostPreview));return Task.CompletedTask;});
    public UiCommand SaveDraftCommand=>SocialCommand(nameof(SaveDraftCommand),()=>{drafts.Save(new(client.UserId,PostTitle,PostBody,PostCategory,PostKind,submissionId));Status="Rascunho protegido por DPAPI e salvo somente para esta conta neste Windows.";return Task.CompletedTask;});
    public UiCommand LoadDraftCommand=>SocialCommand(nameof(LoadDraftCommand),()=>
    {
        var draft=drafts.Load(client.UserId);if(draft is null){Status="Esta conta não possui rascunho local.";return Task.CompletedTask;}
        EditingPostId=null;PostTitle=draft.Title;PostBody=draft.Body;PostCategory=draft.Category;PostKind=draft.Kind;submissionId=draft.RequestId;
        foreach(var property in new[]{nameof(PostTitle),nameof(PostBody),nameof(PostCategory),nameof(PostKind)})Changed(property);Status="Rascunho desta conta restaurado. Nada foi enviado.";return Task.CompletedTask;
    });
    public UiCommand FollowSelectedPostCommand=>SocialCommand(nameof(FollowSelectedPostCommand),async()=>
    {if(SelectedPost is not { } post)throw new InvalidOperationException("Abra uma publicação.");await client.SendAsync(HttpMethod.Put,$"/api/v1/community/posts/{post.Id}/follow",new SelectionRequest(!post.Followed));await RefreshReactionAsync(post.Id);Status="Seguimento confirmado pelo servidor.";});
    public UiCommand BeginReplyEditCommand=>SocialCommand(nameof(BeginReplyEditCommand),()=>
    {if(SelectedReply is not {IsOwner:true} reply)throw new InvalidOperationException("Selecione uma resposta sua.");ReplyEditBody=reply.Body;Changed(nameof(ReplyEditBody));return Task.CompletedTask;});
    public UiCommand SaveReplyEditCommand=>SocialCommand(nameof(SaveReplyEditCommand),async()=>
    {if(SelectedReply is not {IsOwner:true} reply)throw new InvalidOperationException("Selecione uma resposta sua.");await client.SendAsync(HttpMethod.Put,$"/api/v1/community/replies/{reply.Id}",new ReplyEditRequest(ReplyEditBody,reply.Version));await ReloadReplies();Status="Resposta atualizada; revisão técnica e solução anterior retiradas quando aplicável.";});
    public UiCommand RemoveReplyCommand=>SocialCommand(nameof(RemoveReplyCommand),async()=>
    {if(!ReplyDeleteConsent||SelectedReply is not {IsOwner:true} reply)throw new InvalidOperationException("Confirme a remoção de uma resposta sua.");await client.SendAsync(HttpMethod.Post,$"/api/v1/community/replies/{reply.Id}/remove",new ReplyRemoveRequest(reply.Version));ReplyDeleteConsent=false;Changed(nameof(ReplyDeleteConsent));await ReloadReplies();Status="Resposta removida da discussão pública; encadeamento preservado.";});
    private async Task ReloadReplies(){if(SelectedPost is not { } post)return;var replies=await client.GetAsync<ReplyDto[]>($"/api/v1/community/posts/{post.Id}/replies");Replies.Clear();foreach(var reply in replies)Replies.Add(reply);SelectedReply=null;ReplyEditBody="";Changed(nameof(ReplyEditBody));await RefreshReactionAsync(post.Id);}
    public UiCommand LoadOwnProfileSettingsCommand=>SocialCommand(nameof(LoadOwnProfileSettingsCommand),async()=>
    {var own=await client.GetAsync<PublicProfileDto>("/api/v1/community/profiles/by-name/"+Uri.EscapeDataString(client.Username));PublicProfileEnabled=own.PublicVisible;ProfileBio=own.Bio;ProfileAccent=own.Accent;HealthSharingMode=own.Health?.Mode??"none";HealthRankingConsent=own.Health?.IncludeRanking??false;foreach(var name in new[]{nameof(PublicProfileEnabled),nameof(ProfileBio),nameof(ProfileAccent),nameof(HealthSharingMode),nameof(HealthRankingConsent)})Changed(name);});
    public UiCommand SavePublicSettingsCommand=>SocialCommand(nameof(SavePublicSettingsCommand),async()=>
    {await client.SendAsync(HttpMethod.Put,"/api/v1/account/public-profile",new PublicProfileSettingsRequest(PublicProfileEnabled,ProfileBio,ProfileAccent));Status=PublicProfileEnabled?"Perfil visível a visitantes. Hardware só aparece se previamente compartilhado por campo.":"Perfil retirado de visitantes; compartilhamento de saúde removido.";});
    public UiCommand PreviewVisitorProfileCommand=>SocialCommand(nameof(PreviewVisitorProfileCommand),async()=>
    {Profile=await client.GetAsync<PublicProfileDto>("/api/v1/public/profiles/"+Uri.EscapeDataString(client.Username));Status="Esta é a projeção pública vista por um visitante, sem permissões da sua sessão.";});
    public UiCommand ShareHealthCommand=>SocialCommand(nameof(ShareHealthCommand),async()=>
    {
        PublicHealthShareRequest request;
        if(HealthSharingMode=="none")request=new("none",null,"unassessed",null,null,0,DateTimeOffset.UtcNow);
        else
        {
            if(!PublicProfileEnabled||snapshots.Current is not { } snapshot)throw new InvalidOperationException("Ative explicitamente o perfil público e faça uma análise antes de compartilhar.");
            var health=HealthAssessmentEngine.Evaluate(snapshot);if(HealthSharingMode=="score"&&health.Score is null)throw new InvalidOperationException("A avaliação é incompleta. Compartilhe somente um resumo ou faixa, sem inventar uma nota.");
            var band=health.Score is not { } score?"unassessed":score>=90?"excellent":score>=75?"good":score>=50?"attention":"critical";
            request=new(HealthSharingMode,HealthSharingMode=="score"?health.Score:null,band,HealthSharingMode=="summary"?health.LowerBound:null,HealthSharingMode=="summary"?health.UpperBound:null,health.Coverage,snapshot.CapturedAt,HealthRankingConsent);
        }
        await client.SendAsync(HttpMethod.Put,"/api/v1/account/health-sharing",request);Status="Preferência confirmada. O resumo é autodeclarado, não verificado remotamente; nenhum fato bruto do PC foi enviado.";
    });
}
