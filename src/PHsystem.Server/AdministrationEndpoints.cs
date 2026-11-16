using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PHsystem.Community;
using PHsystem.SharedContracts.V1;
namespace PHsystem.Server;
public static class AdministrationEndpoints
{
    private static string Id(ClaimsPrincipal p)=>p.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static async Task<CommunityUser?> Confirm(ClaimsPrincipal principal,string? password,UserManager<CommunityUser> users)
    {
        if(password is null||password.Length>128)return null;var actor=await users.GetUserAsync(principal);
        if(actor is null||!actor.TwoFactorEnabled||await users.IsLockedOutAsync(actor)||!await users.IsInRoleAsync(actor,"Admin"))return null;
        if(await users.CheckPasswordAsync(actor,password))return actor;
        await users.AccessFailedAsync(actor);if(await users.IsLockedOutAsync(actor))await users.UpdateSecurityStampAsync(actor);return null;
    }
    public static void MapAdministration(this WebApplication app)
    {
        var admin=app.MapGroup("/api/v1/admin").RequireAuthorization("Admin");
        admin.MapGet("/stats",async(CommunityDb db,CancellationToken token)=>Results.Ok(new AdminStatsDto(
            await db.Users.CountAsync(token),await db.Posts.CountAsync(p=>!p.Hidden&&!p.NeedsReview,token),await db.Posts.CountAsync(p=>!p.Hidden&&p.NeedsReview,token),await db.Replies.CountAsync(r=>r.Hidden&&r.Body!="",token),await db.Reports.CountAsync(r=>r.Resolution=="pending",token))));
        admin.MapGet("/users",async(int? page,string? search,CommunityDb db,CancellationToken token)=>
        {
            var number=page??1;if(number is <1 or >1000||search?.Length>64)return Results.BadRequest();
            var query=db.Users.AsNoTracking();if(!string.IsNullOrWhiteSpace(search)){var normalized=search.ToUpperInvariant();query=query.Where(u=>u.NormalizedUserName!=null&&u.NormalizedUserName.StartsWith(normalized));}
            var users=await query.OrderBy(u=>u.UserName).Skip((number-1)*20).Take(21).Select(u=>new{u.Id,u.UserName,u.LockoutEnd}).ToArrayAsync(token);var ids=users.Take(20).Select(u=>u.Id).ToArray();
            var roles=await(from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id where ids.Contains(ur.UserId) select new{ur.UserId,role.Name}).ToArrayAsync(token);
            return Results.Ok(new AdminUsersPage(users.Take(20).Select(u=>new AdminUserDto(u.Id,u.UserName!,roles.Where(r=>r.UserId==u.Id).Select(r=>r.Name!).ToArray(),u.LockoutEnd)).ToArray(),users.Length>20));
        });
        admin.MapPost("/users/{id}/role",async(string id,AdminRoleRequest request,ClaimsPrincipal principal,CommunityDb db,UserManager<CommunityUser> users,RoleManager<IdentityRole> roles,CancellationToken token)=>
        {
            if(!CommunityPolicy.Roles.Contains(request.Role)||id.Length>128)return Results.BadRequest();
            await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
            var actor=await Confirm(principal,request.Password,users);if(actor is null){await transaction.CommitAsync(token);return Results.Unauthorized();}
            if(id==actor.Id)return Results.Conflict(new{message="Alterações de seus próprios papéis exigem outro administrador autorizado."});
            var target=await users.FindByIdAsync(id);if(target is null)return Results.NotFound();
            if(request.Enabled&&request.Role!="User"&&!target.TwoFactorEnabled)return Results.Conflict(new{message="O destinatário precisa configurar MFA antes de receber papel da equipe."});
            if(!request.Enabled&&request.Role=="Admin"&&(await users.GetUsersInRoleAsync("Admin")).Count<=1)return Results.Conflict();
            if(request.Enabled&&!await roles.RoleExistsAsync(request.Role)){var created=await roles.CreateAsync(new(request.Role));if(!created.Succeeded)return Results.Conflict();}
            var present=await users.IsInRoleAsync(target,request.Role);
            if(present!=request.Enabled)
            {
                var changed=request.Enabled?await users.AddToRoleAsync(target,request.Role):await users.RemoveFromRoleAsync(target,request.Role);if(!changed.Succeeded)return Results.Conflict();
                await users.UpdateSecurityStampAsync(target);
                db.Audit.Add(new(){ActorId=actor.Id,TargetId=id,Action="role:"+request.Role+":"+(request.Enabled?"grant":"revoke")});await db.SaveChangesAsync(token);
            }
            await transaction.CommitAsync(token);return Results.NoContent();
        }).RequireRateLimiting("auth");
        admin.MapPost("/users/{id}/suspension",async(string id,AdminSuspensionRequest request,ClaimsPrincipal principal,CommunityDb db,UserManager<CommunityUser> users,CancellationToken token)=>
        {
            if(request.Days is <0 or >365||id.Length>128)return Results.BadRequest();
            await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);
            var actor=await Confirm(principal,request.Password,users);if(actor is null){await transaction.CommitAsync(token);return Results.Unauthorized();}if(id==actor.Id)return Results.Conflict();
            var target=await users.FindByIdAsync(id);if(target is null)return Results.NotFound();
            if(request.Days>0&&await users.IsInRoleAsync(target,"Admin")&&(await users.GetUsersInRoleAsync("Admin")).Count<=1)return Results.Conflict();
            if(!(await users.SetLockoutEnabledAsync(target,true)).Succeeded||!(await users.SetLockoutEndDateAsync(target,request.Days==0?null:DateTimeOffset.UtcNow.AddDays(request.Days))).Succeeded)return Results.Conflict();
            await users.UpdateSecurityStampAsync(target);db.Audit.Add(new(){ActorId=actor.Id,TargetId=id,Action=request.Days==0?"suspension-lifted":"suspension-days:"+request.Days});await db.SaveChangesAsync(token);await transaction.CommitAsync(token);return Results.NoContent();
        }).RequireRateLimiting("auth");
        var staff=app.MapGroup("/api/v1/staff").RequireAuthorization("Staff");
        staff.MapGet("/queue",async(CommunityDb db,CancellationToken token)=>Results.Ok(new{
            posts=await db.Posts.AsNoTracking().Where(p=>!p.Hidden&&p.NeedsReview).OrderBy(p=>p.CreatedAt).Take(50).Select(p=>new{p.Id,p.Title,p.Body,p.Kind,p.Category,p.CreatedAt}).ToArrayAsync(token),
            replies=await db.Replies.AsNoTracking().Where(r=>r.Hidden&&r.Body!="").OrderBy(r=>r.CreatedAt).Take(50).Select(r=>new{r.Id,r.PostId,r.Body,r.CreatedAt}).ToArrayAsync(token),
            reports=await db.Reports.AsNoTracking().Where(r=>r.Resolution=="pending").OrderBy(r=>r.CreatedAt).Take(50).ToArrayAsync(token)}));
        staff.MapPost("/reports/{id:guid}/resolve",async(Guid id,ResolveReportRequest request,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            if(request.Decision is not ("dismiss" or "hide"))return Results.BadRequest();
            await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);var report=await db.Reports.FindAsync([id],token);if(report is null)return Results.NotFound();
            if(report.Resolution!="pending")return Results.NoContent();
            if(request.Decision=="hide"){var post=await db.Posts.FindAsync([report.PostId],token);if(post is not null){post.Hidden=true;post.Version++;}}
            report.Resolution=request.Decision;report.ResolvedAt=DateTime.UtcNow;db.Audit.Add(new(){ActorId=Id(principal),TargetId=id.ToString(),Action="report-resolved:"+request.Decision});await db.SaveChangesAsync(token);await transaction.CommitAsync(token);return Results.NoContent();
        });
        staff.MapPost("/posts/{id:guid}/publish",async(Guid id,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            var post=await db.Posts.FindAsync([id],token);if(post is null||post.Hidden)return Results.NotFound();if(!post.NeedsReview)return Results.NoContent();post.NeedsReview=false;post.Version++;db.Audit.Add(new(){ActorId=Id(principal),TargetId=id.ToString(),Action="post-moderation-published"});await db.SaveChangesAsync(token);return Results.NoContent();
        });
        staff.MapPost("/replies/{id:guid}/moderate",async(Guid id,SelectionRequest request,ClaimsPrincipal principal,CommunityDb db,CancellationToken token)=>
        {
            var reply=await db.Replies.FindAsync([id],token);if(reply is null||reply.Body=="")return Results.NotFound();if(reply.Hidden==!request.Selected)return Results.NoContent();reply.Hidden=!request.Selected;reply.ReviewState="Unverified";reply.Version++;
            if(reply.Hidden){var post=await db.Posts.FindAsync([reply.PostId],token);if(post?.AcceptedReplyId==id){post.AcceptedReplyId=null;post.Version++;}}
            db.Audit.Add(new(){ActorId=Id(principal),TargetId=id.ToString(),Action=request.Selected?"reply-moderation-published":"reply-moderation-hidden"});await db.SaveChangesAsync(token);return Results.NoContent();
        });
    }
}
