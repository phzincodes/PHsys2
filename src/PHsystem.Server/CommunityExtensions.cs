using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PHsystem.Community;
using PHsystem.SharedContracts.V1;
namespace PHsystem.Server;
public static class CommunityExtensions
{
    private static string Id(ClaimsPrincipal p)=>p.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static Task<bool> Visible(CommunityDb db,Guid post,string reader,CancellationToken token)=>db.Posts.AnyAsync(p=>p.Id==post&&!p.Hidden&&!p.NeedsReview&&!db.Blocks.Any(b=>(b.OwnerId==reader&&b.BlockedId==p.AuthorId)||(b.BlockedId==reader&&b.OwnerId==p.AuthorId)),token);
    public static void MapCommunityExtensions(this WebApplication app)
    {
        var group=app.MapGroup("/api/v1/community").RequireAuthorization();
        group.MapPut("/posts/{id:guid}/follow",async(Guid id,SelectionRequest request,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            var user=Id(principal);if(!await Visible(db,id,user,token))return Results.NotFound();
            if(request.Selected)await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"FollowedPosts\" (\"PostId\",\"UserId\") VALUES ({id},{user}) ON CONFLICT (\"PostId\",\"UserId\") DO NOTHING",token);
            else await db.FollowedPosts.Where(f=>f.UserId==user&&f.PostId==id).ExecuteDeleteAsync(token);
            return Results.NoContent();
        });
        group.MapPut("/replies/{id:guid}",async(Guid id,ReplyEditRequest request,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            if(!CommunityPolicy.ValidBody(request.Body)||request.ExpectedVersion<1)return Results.BadRequest();
            var user=Id(principal);var reply=await db.Replies.SingleOrDefaultAsync(r=>r.Id==id&&r.AuthorId==user&&!r.Hidden,token);
            if(reply is null||!await Visible(db,reply.PostId,user,token))return Results.NotFound();
            if(reply.Version!=request.ExpectedVersion)return Results.Conflict();if(reply.Body==request.Body)return Results.NoContent();
            db.ReplyRevisions.Add(new(){ReplyId=id,AuthorId=user,Body=reply.Body});
            reply.Body=request.Body;reply.ReviewState="Unverified";reply.Hidden=CommunityPolicy.RequiresReview(request.Body);reply.Version++;
            var post=await db.Posts.SingleAsync(p=>p.Id==reply.PostId,token);if(post.AcceptedReplyId==id){post.AcceptedReplyId=null;post.Version++;}
            db.Audit.Add(new(){ActorId=user,Action="reply-edited-review-invalidated",TargetId=id.ToString()});
            await db.SaveChangesAsync(token);return Results.NoContent();
        });
        group.MapPost("/replies/{id:guid}/remove",async(Guid id,ReplyRemoveRequest request,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            var user=Id(principal);var reply=await db.Replies.SingleOrDefaultAsync(r=>r.Id==id&&r.AuthorId==user&&!r.Hidden,token);
            if(reply is null||!await Visible(db,reply.PostId,user,token))return Results.NotFound();
            if(reply.Version!=request.ExpectedVersion)return Results.Conflict();
            db.ReplyRevisions.Add(new(){ReplyId=id,AuthorId=user,Body=reply.Body});reply.Hidden=true;reply.Body="";reply.ReviewState="Unverified";reply.Version++;
            var post=await db.Posts.SingleAsync(p=>p.Id==reply.PostId,token);if(post.AcceptedReplyId==id){post.AcceptedReplyId=null;post.Version++;}
            db.Audit.Add(new(){ActorId=user,Action="reply-removed",TargetId=id.ToString()});await db.SaveChangesAsync(token);return Results.NoContent();
        });
        group.MapGet("/replies/{id:guid}/revisions",async(Guid id,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            if(!await db.Replies.AnyAsync(r=>r.Id==id&&r.AuthorId==Id(principal),token))return Results.NotFound();
            return Results.Ok(await db.ReplyRevisions.AsNoTracking().Where(r=>r.ReplyId==id).OrderByDescending(r=>r.CreatedAt).Select(r=>new{r.Body,r.CreatedAt}).Take(50).ToArrayAsync(token));
        });
    }
}
