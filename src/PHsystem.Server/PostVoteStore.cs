using Microsoft.EntityFrameworkCore;

namespace PHsystem.Server;

public static class PostVoteStore
{
    public static Task<int> SetAsync(CommunityDb db, Guid postId, string userId, int value, CancellationToken token = default)
    {
        if (value is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(value));
        if (value == 0) return db.PostVotes.Where(v => v.PostId == postId && v.UserId == userId).ExecuteDeleteAsync(token);
        // One parameterized statement: visibility, blocking, self-vote policy and uniqueness.
        return db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PostVotes" ("PostId", "UserId", "Value")
            SELECT p."Id", {userId}, {value} FROM "Posts" p
            WHERE p."Id" = {postId} AND NOT p."Hidden" AND NOT p."NeedsReview" AND p."AuthorId" <> {userId}
            AND NOT EXISTS (SELECT 1 FROM "Blocks" b WHERE (b."OwnerId" = {userId} AND b."BlockedId" = p."AuthorId") OR (b."BlockedId" = {userId} AND b."OwnerId" = p."AuthorId"))
            ON CONFLICT ("PostId", "UserId") DO UPDATE SET "Value" = excluded."Value"
            """, token);
    }
}
