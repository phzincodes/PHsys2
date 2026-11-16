using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PHsystem.Community;
using PHsystem.SharedContracts.V1;

namespace PHsystem.Server;

public static class SocialEndpoints
{
    private static string UserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public static void MapSocialEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/community").RequireAuthorization();
        group.MapGet("/feed", async (int? page, string? search, string? category, string? sort, string? kind, bool? solved, bool? unanswered, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var number = page ?? 1; var mode = sort ?? "recent"; var id = UserId(principal);
            if (number is < 1 or > 1000 || search?.Length > 100 || category?.Length > 32 || mode is not ("recent" or "relevant" or "saved" or "followed" or "mine") || (!string.IsNullOrEmpty(kind) && !CommunityPolicy.PostKinds.Contains(kind))) return Results.BadRequest();
            if (!string.IsNullOrEmpty(category) && !CommunityPolicy.Categories.Contains(category)) return Results.BadRequest();
            var query = db.Posts.AsNoTracking().Where(p => !p.Hidden && (!p.NeedsReview || (mode == "mine" && p.AuthorId == id)));
            query = query.Where(p => !db.Blocks.Any(b => (b.OwnerId == id && b.BlockedId == p.AuthorId) || (b.BlockedId == id && b.OwnerId == p.AuthorId)));
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Title.Contains(search) || p.Body.Contains(search));
            if (!string.IsNullOrEmpty(category)) query = query.Where(p => p.Category == category);
            if (!string.IsNullOrEmpty(kind)) query=query.Where(p=>p.Kind==kind);
            if (solved is true) query=query.Where(p=>p.AcceptedReplyId!=null);
            if (unanswered is true) query=query.Where(p=>!db.Replies.Any(r=>r.PostId==p.Id&&!r.Hidden));
            if (mode == "saved") query = query.Where(p => db.SavedPosts.Any(s => s.PostId == p.Id && s.UserId == id));
            if (mode == "followed") query = query.Where(p => db.FollowedCategories.Any(f => f.UserId == id && f.Category == p.Category) || db.FollowedPosts.Any(f=>f.UserId==id&&f.PostId==p.Id));
            if (mode == "mine") query = query.Where(p => p.AuthorId == id);
            var ordered = mode == "relevant" ? query.OrderByDescending(p => db.PostVotes.Where(v => v.PostId == p.Id).Sum(v => (int?)v.Value) ?? 0).ThenByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
                : query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id);
            var rows = await (from post in ordered.Skip((number - 1) * 20).Take(21)
                join author in db.Users on post.AuthorId equals author.Id
                select new PostDto(post.Id, author.UserName!, post.Title, post.Body, post.Category, post.CreatedAt,
                    db.PostVotes.Where(v => v.PostId == post.Id).Sum(v => (int?)v.Value) ?? 0,
                    db.Replies.Count(r => r.PostId == post.Id && !r.Hidden), db.SavedPosts.Any(s => s.PostId == post.Id && s.UserId == id),
                    db.PostVotes.Where(v => v.PostId == post.Id && v.UserId == id).Select(v => v.Value).FirstOrDefault(), post.AuthorId == id, post.AcceptedReplyId != null,
                    db.PostVotes.Count(v => v.PostId == post.Id && v.Value == 1),post.Kind,db.FollowedPosts.Any(f=>f.PostId==post.Id&&f.UserId==id),post.Version)).ToListAsync(token);
            return Results.Ok(new FeedPage(rows.Take(20).ToArray(), rows.Count > 20, number + 1));
        });
        group.MapPut("/posts/{postId:guid}/vote", async (Guid postId, VoteRequest request, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (request.Value is < -1 or > 1) return Results.BadRequest();
            var id = UserId(principal);
            var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId && !p.Hidden && !p.NeedsReview, token);
            if (post is null) return Results.NotFound();
            if (post.AuthorId == id) return Results.BadRequest();
            if (await Blocked(db, id, post.AuthorId, token)) return Results.NotFound();
            var changed = await PostVoteStore.SetAsync(db, postId, id, request.Value, token);
            if (request.Value != 0 && changed == 0) return Results.NotFound();
            return Results.NoContent();
        });
        group.MapGet("/posts/{postId:guid}/reaction", async (Guid postId, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var id = UserId(principal);
            var state = await db.Posts.AsNoTracking().Where(p => p.Id == postId && !p.Hidden && !p.NeedsReview
                && !db.Blocks.Any(b => (b.OwnerId == id && b.BlockedId == p.AuthorId) || (b.BlockedId == id && b.OwnerId == p.AuthorId)))
                .Select(p => new PostReactionDto(p.Id, db.PostVotes.Where(v => v.PostId == p.Id).Sum(v => (int?)v.Value) ?? 0,
                    db.PostVotes.Count(v => v.PostId == p.Id && v.Value == 1),
                    db.PostVotes.Where(v => v.PostId == p.Id && v.UserId == id).Select(v => v.Value).FirstOrDefault(),
                    db.SavedPosts.Any(v => v.PostId == p.Id && v.UserId == id),
                    db.Replies.Count(r => r.PostId == p.Id && !r.Hidden), p.AcceptedReplyId != null, db.FollowedPosts.Any(f=>f.PostId==p.Id&&f.UserId==id))).SingleOrDefaultAsync(token);
            return state is null ? Results.NotFound() : Results.Ok(state);
        });
        group.MapPut("/posts/{postId:guid}/saved", async (Guid postId, SelectionRequest request, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var id = UserId(principal);
            var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId && !p.Hidden && !p.NeedsReview, token);
            if (post is null || await Blocked(db, id, post.AuthorId, token)) return Results.NotFound();
            if (request.Selected) await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"SavedPosts\" (\"PostId\", \"UserId\") VALUES ({postId}, {id}) ON CONFLICT (\"PostId\", \"UserId\") DO NOTHING", token);
            else await db.SavedPosts.Where(v => v.PostId == postId && v.UserId == id).ExecuteDeleteAsync(token);
            return Results.NoContent();
        });
        group.MapPut("/categories/{category}/follow", async (string category, SelectionRequest request, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (!CommunityPolicy.Categories.Contains(category)) return Results.BadRequest();
            var id = UserId(principal);
            if (request.Selected) await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"FollowedCategories\" (\"UserId\", \"Category\") VALUES ({id}, {category}) ON CONFLICT (\"UserId\", \"Category\") DO NOTHING", token);
            else await db.FollowedCategories.Where(v => v.UserId == id && v.Category == category).ExecuteDeleteAsync(token);
            return Results.NoContent();
        });
        group.MapGet("/following", async (ClaimsPrincipal principal, CommunityDb db, CancellationToken token) => Results.Ok(await db.FollowedCategories.Where(f => f.UserId == UserId(principal)).Select(f => f.Category).ToArrayAsync(token)));
        group.MapPut("/posts/{postId:guid}", async (Guid postId, PostRequest request, ClaimsPrincipal principal, CommunityDb db, IAuthorizationService authorization, CancellationToken token) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 160 || !CommunityPolicy.ValidBody(request.Body) || !CommunityPolicy.Categories.Contains(request.Category) || !CommunityPolicy.PostKinds.Contains(request.Kind)) return Results.BadRequest();
            if(request.Kind=="announcement" && !(await authorization.AuthorizeAsync(principal,"Admin")).Succeeded)return Results.Forbid();
            var id = UserId(principal); var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId && p.AuthorId == id && !p.Hidden, token);
            if (post is null) return Results.NotFound();
            if(request.ExpectedVersion is { } expected && expected!=post.Version)return Results.Conflict();
            db.PostRevisions.Add(new() { Kind=post.Kind,PostId = postId, AuthorId = id, Title = post.Title, Body = post.Body });
            post.Kind=request.Kind;post.Version++;
            post.Title = request.Title.Trim(); post.Body = request.Body; post.Category = request.Category;
            post.NeedsReview = CommunityPolicy.RequiresReview(post.Title + " " + post.Body); post.AcceptedReplyId = null;
            foreach (var reply in await db.Replies.Where(r => r.PostId == postId && r.ReviewState == "TechnicallyReviewed").ToListAsync(token)) reply.ReviewState = "Unverified";
            db.Audit.Add(new() { ActorId = id, Action = "post-edited-review-invalidated", TargetId = postId.ToString() });
            await db.SaveChangesAsync(token); return Results.NoContent();
        });
        group.MapDelete("/posts/{postId:guid}", async (Guid postId, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var id = UserId(principal); var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId && p.AuthorId == id && !p.Hidden, token);
            if (post is null) return Results.NotFound();
            post.Hidden = true;post.Version++; db.Audit.Add(new() { ActorId = id, Action = "own-post-hidden", TargetId = postId.ToString() });
            await db.SaveChangesAsync(token); return Results.NoContent();
        });
        group.MapGet("/posts/{postId:guid}/revisions", async (Guid postId, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (!await db.Posts.AnyAsync(p => p.Id == postId && p.AuthorId == UserId(principal), token)) return Results.NotFound();
            return Results.Ok(await db.PostRevisions.AsNoTracking().Where(r => r.PostId == postId).OrderByDescending(r => r.CreatedAt).Take(20).Select(r => new { r.Title, r.Body, r.CreatedAt }).ToListAsync(token));
        });
        group.MapPut("/profile", async (ProfileRequest request, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            if (request.Bio is null || request.Bio.Length > 300 || request.Bio.Contains('<') || request.Bio.Contains('>') || CommunityPolicy.RequiresReview(request.Bio)) return Results.BadRequest();
            var id = UserId(principal); var profile = await db.SocialProfiles.FindAsync([id], token);
            if (profile is null) db.SocialProfiles.Add(new() { UserId = id, Bio = request.Bio }); else profile.Bio = request.Bio;
            await db.SaveChangesAsync(token); return Results.NoContent();
        });
        group.MapGet("/profiles/by-name/{name}", async (string name, ClaimsPrincipal principal, UserManager<CommunityUser> users, CommunityDb db, CancellationToken token) =>
        {
            var profile=await PublicProfiles.Project(name,false,UserId(principal),users,db,token);
            return profile is null?Results.NotFound():Results.Ok(profile);
        });
        group.MapGet("/notifications", async (ClaimsPrincipal principal, CommunityDb db, CancellationToken token) => Results.Ok(await db.SocialNotifications.Where(n => n.UserId == UserId(principal)).OrderByDescending(n => n.CreatedAt).Take(50).Select(n => new NotificationDto(n.Id, n.PostId, n.Kind, n.Read, n.CreatedAt)).ToArrayAsync(token)));
        group.MapPut("/notifications/{id:guid}/read", async (Guid id, ClaimsPrincipal principal, CommunityDb db, CancellationToken token) =>
        {
            var notification = await db.SocialNotifications.SingleOrDefaultAsync(n => n.Id == id && n.UserId == UserId(principal), token);
            if (notification is null) return Results.NotFound(); notification.Read = true; await db.SaveChangesAsync(token); return Results.NoContent();
        });
    }
    private static Task<bool> Blocked(CommunityDb db, string one, string two, CancellationToken token) => db.Blocks.AnyAsync(b => (b.OwnerId == one && b.BlockedId == two) || (b.OwnerId == two && b.BlockedId == one), token);
}
