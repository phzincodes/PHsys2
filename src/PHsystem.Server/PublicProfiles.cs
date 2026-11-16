using System.Security.Claims;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PHsystem.Community;
using PHsystem.SharedContracts.V1;
namespace PHsystem.Server;
public static class PublicProfiles
{
    public static async Task<PublicProfileDto?> Project(string name,bool publicOnly,string? reader,UserManager<CommunityUser> users,CommunityDb db,CancellationToken token)
    {
        if(name.Length>24)return null;var user=await users.FindByNameAsync(name);if(user is null)return null;
        if(reader is not null&&await db.Blocks.AnyAsync(b=>(b.OwnerId==reader&&b.BlockedId==user.Id)||(b.BlockedId==reader&&b.OwnerId==user.Id),token))return null;
        var profile=await db.SocialProfiles.AsNoTracking().SingleOrDefaultAsync(p=>p.UserId==user.Id,token);
        if(publicOnly&&profile?.PublicVisible!=true)return null;
        var posts=await db.Posts.CountAsync(p=>p.AuthorId==user.Id&&!p.Hidden&&!p.NeedsReview,token);
        var replies=await db.Replies.CountAsync(r=>r.AuthorId==user.Id&&!r.Hidden&&db.Posts.Any(p=>p.Id==r.PostId&&!p.Hidden&&!p.NeedsReview),token);
        var accepted=await db.Replies.CountAsync(r=>r.AuthorId==user.Id&&!r.Hidden&&db.Posts.Any(p=>p.AcceptedReplyId==r.Id&&!p.Hidden&&!p.NeedsReview&&p.AuthorId!=user.Id),token);
        var likes=await db.PostVotes.CountAsync(v=>v.Value==1&&db.Posts.Any(p=>p.Id==v.PostId&&p.AuthorId==user.Id&&!p.Hidden&&!p.NeedsReview&&p.AuthorId!=v.UserId),token);
        var health=profile?.PublicVisible==true?await db.PublicHealthShares.AsNoTracking().SingleOrDefaultAsync(h=>h.UserId==user.Id,token):null;
        return new(user.UserName!,profile?.Bio??"",posts,replies,accepted,await db.PublicHardware.Where(h=>h.UserId==user.Id).ToDictionaryAsync(h=>h.Field,h=>h.Value,token),
            (int)Math.Min(100000L,(long)accepted*10+likes),profile?.PublicVisible??false,profile?.Accent??"#FF3347",health is null?null:new(health.Mode,health.Score,health.Band,health.Lower,health.Upper,health.Coverage,new DateTimeOffset(DateTime.SpecifyKind(health.CapturedAt,DateTimeKind.Utc)),health.IncludeRanking,"SelfDeclared",health.CapturedAt<DateTime.UtcNow.AddDays(-30)));
    }
    public static void MapPublicProfiles(this WebApplication app)
    {
        app.MapGet("/api/v1/public/profiles/{name}",async(string name,UserManager<CommunityUser> users,CommunityDb db,HttpContext context,CancellationToken token)=>
        {context.Response.Headers.CacheControl="no-store";var profile=await Project(name,true,null,users,db,token);return profile is null?Results.NotFound():Results.Ok(profile);});
        app.MapGet("/api/v1/public/health-ranking",async(CommunityDb db,CancellationToken token)=>
        {
            var cutoff=DateTime.UtcNow.AddDays(-30);
            return Results.Ok(await (from health in db.PublicHealthShares.AsNoTracking() join profile in db.SocialProfiles on health.UserId equals profile.UserId join user in db.Users on health.UserId equals user.Id
                where profile.PublicVisible&&health.IncludeRanking&&health.Mode=="score"&&health.Score!=null&&health.CapturedAt>=cutoff
                orderby health.Score descending,health.Coverage descending,health.CapturedAt descending,user.UserName
                select new HealthRankingDto(user.UserName!,health.Score!.Value,health.Coverage,health.CapturedAt,"SelfDeclared")).Take(100).ToArrayAsync(token));
        });
        var account=app.MapGroup("/api/v1/account").RequireAuthorization();
        account.MapPut("/public-profile",async(PublicProfileSettingsRequest request,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            if(request.Bio is null||request.Bio.Length>300||CommunityPolicy.RequiresReview(request.Bio)||request.Accent is null||!Regex.IsMatch(request.Accent,"^#[a-fA-F0-9]{6}$"))return Results.BadRequest();
            await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
            var user=principal.FindFirstValue(ClaimTypes.NameIdentifier)!;var profile=await db.SocialProfiles.FindAsync([user],token);
            if(profile is null){profile=new(){UserId=user};db.SocialProfiles.Add(profile);}profile.Bio=request.Bio;profile.Accent=request.Accent;profile.PublicVisible=request.Enabled;
            if(!request.Enabled)await db.PublicHealthShares.Where(h=>h.UserId==user).ExecuteDeleteAsync(token);
            db.Audit.Add(new(){ActorId=user,TargetId=user,Action=request.Enabled?"public-profile-enabled":"public-profile-withdrawn"});await db.SaveChangesAsync(token);await transaction.CommitAsync(token);return Results.NoContent();
        });
        account.MapPut("/health-sharing",async(PublicHealthShareRequest request,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
            var user=principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if(request.Mode=="none") {await db.PublicHealthShares.Where(h=>h.UserId==user).ExecuteDeleteAsync(token);db.Audit.Add(new(){ActorId=user,TargetId=user,Action="health-sharing-withdrawn"});await db.SaveChangesAsync(token);await transaction.CommitAsync(token);return Results.NoContent();}
            if(request.Mode is not ("band" or "score" or "summary")||request.Coverage is <0 or >100||request.CapturedAt>DateTimeOffset.UtcNow.AddMinutes(1)||request.CapturedAt<DateTimeOffset.UtcNow.AddDays(-30))return Results.BadRequest();
            if(request.Mode=="score"&&(request.Score is null or <0 or >100||request.Lower is not null||request.Upper is not null))return Results.BadRequest();
            if(request.Mode!="score"&&request.Score is not null)return Results.BadRequest();
            if(request.Mode=="summary"&&(request.Lower is null or <0 or >100||request.Upper is null or <0 or >100||request.Lower>request.Upper))return Results.BadRequest();
            if(request.Mode=="band"&&(request.Band is not ("excellent" or "good" or "attention" or "critical" or "unassessed")||request.Lower is not null||request.Upper is not null))return Results.BadRequest();
            if(request.IncludeRanking&&request.Mode!="score")return Results.BadRequest();
            if(!await db.SocialProfiles.AnyAsync(p=>p.UserId==user&&p.PublicVisible,token))return Results.Conflict();
            var share=await db.PublicHealthShares.FindAsync([user],token);if(share is null){share=new(){UserId=user};db.PublicHealthShares.Add(share);}
            share.Mode=request.Mode;share.Score=request.Score;share.Band=request.Mode=="score"?request.Score>=90?"excellent":request.Score>=75?"good":request.Score>=50?"attention":"critical":request.Mode=="band"?request.Band:"unassessed";
            share.Lower=request.Mode=="summary"?request.Lower:null;share.Upper=request.Mode=="summary"?request.Upper:null;share.Coverage=request.Coverage;share.CapturedAt=request.CapturedAt.UtcDateTime;share.IncludeRanking=request.IncludeRanking;
            db.Audit.Add(new(){ActorId=user,TargetId=user,Action="health-sharing:"+request.Mode+(request.IncludeRanking?":ranking-opt-in":"")});await db.SaveChangesAsync(token);await transaction.CommitAsync(token);return Results.NoContent();
        });
    }
}
