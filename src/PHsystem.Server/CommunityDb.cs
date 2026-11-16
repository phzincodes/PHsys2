using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PHsystem.Server;

public sealed class CommunityUser : IdentityUser
{
    public bool AcceptDirectMessages { get; set; }
}

public sealed class CommunityDb(DbContextOptions<CommunityDb> options) : IdentityDbContext<CommunityUser>(options)
{
    public DbSet<FollowedPost> FollowedPosts => Set<FollowedPost>();
    public DbSet<ReplyRevision> ReplyRevisions => Set<ReplyRevision>();
    public DbSet<PublicHealthShare> PublicHealthShares => Set<PublicHealthShare>();
    public DbSet<PostVote> PostVotes => Set<PostVote>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
    public DbSet<FollowedCategory> FollowedCategories => Set<FollowedCategory>();
    public DbSet<PostRevision> PostRevisions => Set<PostRevision>();
    public DbSet<SocialProfile> SocialProfiles => Set<SocialProfile>();
    public DbSet<SocialNotification> SocialNotifications => Set<SocialNotification>();
    public DbSet<ForumPost> Posts => Set<ForumPost>();
    public DbSet<ForumReply> Replies => Set<ForumReply>();
    public DbSet<AbuseReport> Reports => Set<AbuseReport>();
    public DbSet<DirectMessage> Messages => Set<DirectMessage>();
    public DbSet<UserBlock> Blocks => Set<UserBlock>();
    public DbSet<PublicHardwareField> PublicHardware => Set<PublicHardwareField>();
    public DbSet<AuditEntry> Audit => Set<AuditEntry>();
    public DbSet<TechnicalReview> Reviews => Set<TechnicalReview>();
    public DbSet<MfaUse> MfaUses => Set<MfaUse>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<FollowedPost>().HasKey(f=>new{f.PostId,f.UserId});
        builder.Entity<FollowedPost>().HasOne<ForumPost>().WithMany().HasForeignKey(f=>f.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<FollowedPost>().HasOne<CommunityUser>().WithMany().HasForeignKey(f=>f.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ReplyRevision>().HasIndex(r=>new{r.ReplyId,r.CreatedAt});
        builder.Entity<ReplyRevision>().HasOne<ForumReply>().WithMany().HasForeignKey(r=>r.ReplyId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ReplyRevision>().Property(r=>r.Body).HasMaxLength(8000);
        builder.Entity<PublicHealthShare>().HasKey(h=>h.UserId);
        builder.Entity<PublicHealthShare>().HasOne<CommunityUser>().WithMany().HasForeignKey(h=>h.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PublicHealthShare>().HasOne<SocialProfile>().WithMany().HasForeignKey(h=>h.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PublicHealthShare>().Property(h=>h.Mode).HasMaxLength(16);
        builder.Entity<PublicHealthShare>().Property(h=>h.Band).HasMaxLength(16);
        builder.Entity<ForumReply>().Property(r=>r.Version).IsConcurrencyToken().HasDefaultValue(1);
        builder.Entity<ForumPost>().HasIndex(p=>new{p.AuthorId,p.ClientRequestId}).IsUnique().HasFilter("\"ClientRequestId\" IS NOT NULL");
        builder.Entity<ForumPost>().Property(p=>p.Version).IsConcurrencyToken().HasDefaultValue(1);
        builder.Entity<ForumPost>().Property(p=>p.Kind).HasMaxLength(24).HasDefaultValue("discussion");
        builder.Entity<PostRevision>().Property(p=>p.Kind).HasMaxLength(24).HasDefaultValue("discussion");
        builder.Entity<SocialProfile>().Property(p=>p.Accent).HasMaxLength(7).HasDefaultValue("#FF3347");
        builder.Entity<AbuseReport>().Property(r=>r.Resolution).HasMaxLength(16).HasDefaultValue("pending");
        builder.Entity<PostVote>().HasKey(v => new { v.PostId, v.UserId });
        builder.Entity<SavedPost>().HasKey(v => new { v.PostId, v.UserId });
        builder.Entity<FollowedCategory>().HasKey(v => new { v.UserId, v.Category });
        builder.Entity<SocialProfile>().HasKey(v => v.UserId);
        builder.Entity<SocialProfile>().Property(v => v.Bio).HasMaxLength(300);
        builder.Entity<FollowedCategory>().Property(v => v.Category).HasMaxLength(32);
        builder.Entity<SocialNotification>().Property(v => v.Kind).HasMaxLength(32);
        builder.Entity<SocialNotification>().HasIndex(v => new { v.UserId, v.CreatedAt });
        builder.Entity<PostRevision>().HasIndex(v => new { v.PostId, v.CreatedAt });
        builder.Entity<PostVote>().HasOne<ForumPost>().WithMany().HasForeignKey(v => v.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SavedPost>().HasOne<ForumPost>().WithMany().HasForeignKey(v => v.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PostVote>().HasOne<CommunityUser>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SavedPost>().HasOne<CommunityUser>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<FollowedCategory>().HasOne<CommunityUser>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SocialProfile>().HasOne<CommunityUser>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SocialNotification>().HasOne<CommunityUser>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CommunityUser>().HasIndex(u => u.NormalizedEmail).IsUnique();
        builder.Entity<ForumPost>().HasKey(p => p.Id);
        builder.Entity<ForumPost>().Property(p => p.Title).HasMaxLength(160);
        builder.Entity<ForumPost>().Property(p => p.Body).HasMaxLength(8000);
        builder.Entity<ForumPost>().HasOne<CommunityUser>().WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ForumReply>().HasOne<ForumPost>().WithMany().HasForeignKey(p => p.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserBlock>().HasKey(b => new { b.OwnerId, b.BlockedId });
        builder.Entity<PublicHardwareField>().HasKey(f => new { f.UserId, f.Field });
        builder.Entity<AuditEntry>().HasIndex(a => a.CreatedAt);
        builder.Entity<TechnicalReview>().HasOne<ForumReply>().WithMany().HasForeignKey(r => r.ReplyId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ForumReply>().HasOne<ForumReply>().WithMany().HasForeignKey(r => r.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MfaUse>().HasKey(m => new { m.UserId, m.Digest });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.Entries<AuditEntry>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("A auditoria é append-only na aplicação.");
        return base.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ForumPost
{
    public Guid? ClientRequestId {get;set;}
    public int Version {get;set;}=1;
    public string Kind { get; set; } = "discussion";
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AuthorId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Hidden { get; set; }
    public bool NeedsReview { get; set; }
    public Guid? AcceptedReplyId { get; set; }
}
public sealed class ForumReply
{
    public int Version {get;set;}=1;
    public Guid? ParentId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public string AuthorId { get; set; } = "";
    public string Body { get; set; } = "";
    public string ReviewState { get; set; } = "Unverified";
    public bool Hidden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class AbuseReport
{
    public string Resolution { get; set; } = "pending";
    public DateTime? ResolvedAt { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public string ReporterId { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class DirectMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SenderId { get; set; } = "";
    public string RecipientId { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class UserBlock { public string OwnerId { get; set; } = ""; public string BlockedId { get; set; } = ""; }
public sealed class PublicHardwareField { public string UserId { get; set; } = ""; public string Field { get; set; } = ""; public string Value { get; set; } = ""; }
public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ActorId { get; set; } = "";
    public string Action { get; set; } = "";
    public string TargetId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class TechnicalReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReplyId { get; set; }
    public string ReviewerId { get; set; } = "";
    public string Decision { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string OfficialSource { get; set; } = "";
    public string Scope { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(90);
}
public sealed class MfaUse
{
    public string UserId { get; set; } = "";
    public string Digest { get; set; } = "";
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}

