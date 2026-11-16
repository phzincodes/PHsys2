using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PHsystem.Community;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Server;

public static class CommunityEndpoints
{
    private static string UserId(ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public static void MapCommunityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/community").RequireAuthorization();
        group.MapGet("/categories", () => CommunityPolicy.Categories);
        group.MapGet("/posts", async (string? search, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (search?.Length > 100) return Results.BadRequest();
            var viewer=UserId(principal);
            var query = db.Posts.AsNoTracking().Where(p => !p.Hidden && !p.NeedsReview && !db.Blocks.Any(b=>(b.OwnerId==viewer&&b.BlockedId==p.AuthorId)||(b.BlockedId==viewer&&b.OwnerId==p.AuthorId)));
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Title.Contains(search) || p.Body.Contains(search));
            var rows = await (from post in query join user in db.Users on post.AuthorId equals user.Id
                orderby post.CreatedAt descending select new PostDto(post.Id, user.UserName!, post.Title, post.Body, post.Category,post.CreatedAt,0,0,false,0,post.AuthorId==viewer,post.AcceptedReplyId!=null,null,post.Kind,false,post.Version))
                .Take(50).ToListAsync(token);
            return Results.Ok(rows);
        });
        group.MapPost("/posts", async (PostRequest request, ClaimsPrincipal principal, CommunityDb db, IAuthorizationService authorization, CancellationToken token) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 160 || !CommunityPolicy.ValidBody(request.Body)
                || !CommunityPolicy.Categories.Contains(request.Category, StringComparer.Ordinal) || !CommunityPolicy.PostKinds.Contains(request.Kind)) return Results.BadRequest();
            if(request.ClientRequestId==Guid.Empty)return Results.BadRequest();
            if(request.ClientRequestId is { } key){var existing=await db.Posts.AsNoTracking().SingleOrDefaultAsync(p=>p.AuthorId==UserId(principal)&&p.ClientRequestId==key,token);if(existing is not null){if(existing.Title!=request.Title.Trim()||existing.Body!=request.Body||existing.Category!=request.Category||existing.Kind!=request.Kind)return Results.Conflict();return Results.Created($"/api/v1/community/posts/{existing.Id}",new{existing.Id,state=existing.NeedsReview?"PendingReview":"Unverified"});}}
            if(request.Kind=="announcement" && !(await authorization.AuthorizeAsync(principal,"Admin")).Succeeded)return Results.Forbid();
            var post = new ForumPost { ClientRequestId=request.ClientRequestId,Kind=request.Kind, AuthorId = UserId(principal), Title = request.Title.Trim(), Body = request.Body,
                Category = request.Category, NeedsReview = CommunityPolicy.RequiresReview(request.Title + " " + request.Body) };
            db.Posts.Add(post);
            await db.SaveChangesAsync(token);
            return Results.Created($"/api/v1/community/posts/{post.Id}", new { post.Id, state = post.NeedsReview ? "PendingReview" : "Unverified" });
        });
        group.MapGet("/posts/{id:guid}", async (Guid id, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var post = await db.Posts.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, token);
            if (post is null || post.Hidden || (post.NeedsReview && post.AuthorId != UserId(principal)) || await db.Blocks.AnyAsync(b=>(b.OwnerId==UserId(principal)&&b.BlockedId==post.AuthorId)||(b.BlockedId==UserId(principal)&&b.OwnerId==post.AuthorId),token)) return Results.NotFound();
            var author = await db.Users.Where(u => u.Id == post.AuthorId).Select(u => u.UserName).SingleAsync(token);
            return Results.Ok(new PostDto(post.Id, author!, post.Title, post.Body, post.Category, post.CreatedAt, Kind:post.Kind, IsOwner:post.AuthorId==UserId(principal), Version:post.Version));
        });
        group.MapPost("/posts/{id:guid}/replies", async (Guid id, ReplyRequest request, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (!CommunityPolicy.ValidBody(request.Body)) return Results.BadRequest();
            var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == id && !p.Hidden && !p.NeedsReview, token);
            if (post is null || await db.Blocks.AnyAsync(b=>(b.OwnerId==UserId(principal)&&b.BlockedId==post.AuthorId)||(b.BlockedId==UserId(principal)&&b.OwnerId==post.AuthorId),token)) return Results.NotFound();
            var parentId = request.ParentId;
            var depth = 0;
            while (parentId is { } ancestor)
            {
                var parent = await db.Replies.AsNoTracking().SingleOrDefaultAsync(r => r.Id == ancestor && r.PostId == id && !r.Hidden, token);
                if (parent is null || ++depth > 4) return Results.BadRequest();
                parentId = parent.ParentId;
            }
            var reply = new ForumReply { ParentId = request.ParentId, PostId = id, AuthorId = UserId(principal), Body = request.Body,
                Hidden = CommunityPolicy.RequiresReview(request.Body), ReviewState = "Unverified" };
            db.Replies.Add(reply);
            if (!reply.Hidden && post.AuthorId != reply.AuthorId && !await db.Blocks.AnyAsync(b => (b.OwnerId == post.AuthorId && b.BlockedId == reply.AuthorId) || (b.OwnerId == reply.AuthorId && b.BlockedId == post.AuthorId), token))
                db.SocialNotifications.Add(new() { UserId = post.AuthorId, PostId = post.Id, Kind = "NewReply" });
            if(!reply.Hidden) {
                var readers=await db.FollowedPosts.Where(f=>f.PostId==id&&f.UserId!=reply.AuthorId&&f.UserId!=post.AuthorId
                    && !db.Blocks.Any(b=>(b.OwnerId==f.UserId&&b.BlockedId==reply.AuthorId)||(b.BlockedId==f.UserId&&b.OwnerId==reply.AuthorId)))
                    .Select(f=>f.UserId).Take(500).ToArrayAsync(token);
                foreach(var reader in readers)db.SocialNotifications.Add(new(){UserId=reader,PostId=id,Kind="FollowedPostReply"});
            }
            await db.SaveChangesAsync(token);
            return Results.Created($"/api/v1/community/posts/{id}/replies", new { reply.Id, reply.ReviewState, pendingReview = reply.Hidden });
        });
        group.MapGet("/posts/{id:guid}/replies", async (Guid id, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (!await db.Posts.AnyAsync(p => p.Id == id && !p.Hidden && !p.NeedsReview && !db.Blocks.Any(b=>(b.OwnerId==UserId(principal)&&b.BlockedId==p.AuthorId)||(b.BlockedId==UserId(principal)&&b.OwnerId==p.AuthorId)), token)) return Results.NotFound();
            var replies = await (from reply in db.Replies.AsNoTracking() join user in db.Users on reply.AuthorId equals user.Id
                let unavailable=reply.Hidden||db.Blocks.Any(b=>(b.OwnerId==UserId(principal)&&b.BlockedId==reply.AuthorId)||(b.BlockedId==UserId(principal)&&b.OwnerId==reply.AuthorId))
                where reply.PostId == id orderby reply.CreatedAt
                select new ReplyDto(reply.Id, unavailable ? "[indisponível]" : user.UserName!, unavailable ? "Resposta indisponível; discussão preservada." : reply.Body,
                    !unavailable && reply.ReviewState == "TechnicallyReviewed" && db.Reviews.Any(r => r.ReplyId == reply.Id && r.Decision == "Approve" && r.ExpiresAt > DateTime.UtcNow)
                        ? "TechnicallyReviewed" : unavailable ? "Removed" : "Unverified", reply.CreatedAt, reply.ParentId,
                    db.Posts.Any(p => p.Id == id && p.AcceptedReplyId == reply.Id), 0, !unavailable && reply.AuthorId==UserId(principal), reply.Version)).Take(100).ToListAsync(token);
            var tree = new List<ReplyDto>();
            void AddChildren(Guid? parent, int depth)
            {
                foreach (var reply in replies.Where(r => r.ParentId == parent))
                {
                    tree.Add(reply with { Depth = depth });
                    if (depth < 4) AddChildren(reply.Id, depth + 1);
                }
            }
            AddChildren(null, 0);
            return Results.Ok(tree);
        });
        group.MapPost("/posts/{id:guid}/solution/{replyId:guid}", async (Guid id, Guid replyId, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == id && p.AuthorId == UserId(principal), token);
            if (post is null || post.Hidden || post.NeedsReview) return Results.NotFound();
            var acceptedReply=await db.Replies.SingleOrDefaultAsync(r=>r.Id==replyId&&r.PostId==id&&!r.Hidden,token);
            if(acceptedReply is null)return Results.NotFound();
            acceptedReply.Version++;post.Version++;
            post.AcceptedReplyId = replyId;
            var solutionAuthor = await db.Replies.Where(r => r.Id == replyId).Select(r => r.AuthorId).SingleAsync(token);
            if (solutionAuthor != post.AuthorId && !await db.SocialNotifications.AnyAsync(n => n.UserId == solutionAuthor && n.PostId == id && n.Kind == "AcceptedSolution", token))
                db.SocialNotifications.Add(new() { UserId = solutionAuthor, PostId = id, Kind = "AcceptedSolution" });
            await db.SaveChangesAsync(token);
            return Results.NoContent();
        });
        group.MapPost("/reports", async (ReportRequest request, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000 || !await db.Posts.AnyAsync(p => p.Id == request.PostId, token)) return Results.BadRequest();
            db.Reports.Add(new() { PostId = request.PostId, ReporterId = UserId(principal), Reason = request.Reason });
            await db.SaveChangesAsync(token);
            return Results.Accepted();
        });
        group.MapPut("/blocks/{id}", async (string id, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var owner = UserId(principal);
            if (id == owner || !await db.Users.AnyAsync(u => u.Id == id, token)) return Results.BadRequest();
            if (!await db.Blocks.AnyAsync(b => b.OwnerId == owner && b.BlockedId == id, token))
            {
                db.Blocks.Add(new() { OwnerId = owner, BlockedId = id });
                await db.SaveChangesAsync(token);
            }
            return Results.NoContent();
        });
        group.MapDelete("/blocks/{id}", async (string id, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            await db.Blocks.Where(b => b.OwnerId == UserId(principal) && b.BlockedId == id).ExecuteDeleteAsync(token);
            return Results.NoContent();
        });
        group.MapPost("/messages", async (MessageRequest request, ClaimsPrincipal principal, CommunityDb db, IHubContext<NotificationHub> hub, CancellationToken token) =>
        {
            var sender = UserId(principal);
            if (!CommunityPolicy.ValidBody(request.Body) || request.Body.Length > 2000 || request.RecipientId == sender) return Results.BadRequest();
            var recipient = await db.Users.FindAsync([request.RecipientId], token);
            if (recipient is null || !recipient.AcceptDirectMessages || await db.Blocks.AnyAsync(b =>
                (b.OwnerId == sender && b.BlockedId == recipient.Id) || (b.OwnerId == recipient.Id && b.BlockedId == sender), token)) return Results.NotFound();
            if (await db.Messages.CountAsync(m => m.SenderId == sender && m.CreatedAt > DateTime.UtcNow.AddMinutes(-1), token) >= 5) return Results.StatusCode(429);
            var message = new DirectMessage { SenderId = sender, RecipientId = recipient.Id, Body = request.Body };
            db.Messages.Add(message);
            await db.SaveChangesAsync(token);
            await hub.Clients.User(recipient.Id).SendAsync("messageAvailable", new { message.Id }, token);
            return Results.Created("/api/v1/community/messages", new { message.Id, endToEndEncrypted = false });
        });
        group.MapGet("/messages", async (ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
            await db.Messages.AsNoTracking().Where(m => m.SenderId == UserId(principal) || m.RecipientId == UserId(principal))
                .OrderByDescending(m => m.CreatedAt).Take(50).Select(m => new { m.Id, m.SenderId, m.RecipientId, m.Body, m.CreatedAt }).ToListAsync(token));
        group.MapPut("/hardware-sharing", async (HardwareShareRequest request, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            string[] allowed = ["cpu", "gpu", "ramTotal", "os", "storageAggregate", "driverVersions"];
            if (request.Fields is null || request.Fields.Count > allowed.Length || request.Fields.Any(f => !allowed.Contains(f.Key, StringComparer.Ordinal) || !HardwareSharingPolicy.SafeValue(f.Value))) return Results.BadRequest();
            var id = UserId(principal);
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            await db.PublicHardware.Where(f => f.UserId == id).ExecuteDeleteAsync(token);
            foreach (var field in request.Fields) db.PublicHardware.Add(new() { UserId = id, Field = field.Key, Value = field.Value });
            db.Audit.Add(new() { ActorId = id, Action = "hardware-sharing-replaced", TargetId = id });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return Results.NoContent();
        });
        group.MapGet("/profiles/{id}/hardware", async (string id, CommunityDb db, CancellationToken token) =>
            await db.PublicHardware.AsNoTracking().Where(f => f.UserId == id).ToDictionaryAsync(f => f.Field, f => f.Value, token));
        app.MapGet("/api/v1/staff/reports", async (CommunityDb db, CancellationToken token) => await db.Reports.AsNoTracking().OrderByDescending(r => r.CreatedAt).Take(100).ToListAsync(token)).RequireAuthorization("Staff");
        app.MapPost("/api/v1/staff/posts/{id:guid}/hide", async (Guid id, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var post = await db.Posts.FindAsync([id], token);
            if (post is null) return Results.NotFound();
            post.Hidden = true;post.Version++;
            db.Audit.Add(new() { ActorId = UserId(principal), Action = "post-hidden", TargetId = id.ToString() });
            await db.SaveChangesAsync(token);
            return Results.NoContent();
        }).RequireAuthorization("Staff");
        app.MapPost("/api/v1/reviews/{replyId:guid}", async (Guid replyId, ReviewRequest request, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (request.Decision is not "Approve" and not "Revoke" || string.IsNullOrWhiteSpace(request.Evidence) || request.Evidence.Length > 2000
                || string.IsNullOrWhiteSpace(request.Scope) || request.Scope.Length > 500 || !Uri.TryCreate(request.OfficialSource, UriKind.Absolute, out var source)
                || source.Scheme != "https") return Results.BadRequest();
            var reply = await db.Replies.FindAsync([replyId], token);
            if (reply is null || reply.AuthorId == UserId(principal)) return Results.NotFound();
            reply.ReviewState = request.Decision == "Approve" ? "TechnicallyReviewed" : "Unverified";
            reply.Hidden = false;
            db.Reviews.Add(new() { ReplyId = replyId, ReviewerId = UserId(principal), Decision = request.Decision,
                Evidence = request.Evidence, OfficialSource = source.AbsoluteUri, Scope = request.Scope });
            db.Audit.Add(new() { ActorId = UserId(principal), Action = "technical-review-" + request.Decision, TargetId = replyId.ToString() });
            await db.SaveChangesAsync(token);
            return Results.NoContent();
        }).RequireAuthorization("Review");
        app.MapGet("/api/v1/admin/audit", async (CommunityDb db, CancellationToken token) =>
            await db.Audit.AsNoTracking().OrderByDescending(a => a.CreatedAt).Take(100).ToListAsync(token)).RequireAuthorization("Admin");
    }
}

