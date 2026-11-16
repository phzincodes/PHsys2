namespace PHsystem.Server;

public sealed class PostVote { public Guid PostId { get; set; } public string UserId { get; set; } = ""; public int Value { get; set; } }
public sealed class SavedPost { public Guid PostId { get; set; } public string UserId { get; set; } = ""; }
public sealed class FollowedCategory { public string UserId { get; set; } = ""; public string Category { get; set; } = ""; }
public sealed class PostRevision
{
    public string Kind { get; set; } = "discussion";
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid PostId { get; set; }
    public string AuthorId { get; set; } = ""; public string Title { get; set; } = ""; public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class SocialProfile { public bool PublicVisible { get; set; } public string Accent { get; set; } = "#FF3347";  public string UserId { get; set; } = ""; public string Bio { get; set; } = ""; }
public sealed class SocialNotification
{
    public Guid Id { get; set; } = Guid.NewGuid(); public string UserId { get; set; } = "";
    public Guid PostId { get; set; } public string Kind { get; set; } = "";
    public bool Read { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FollowedPost { public Guid PostId {get;set;} public string UserId {get;set;}=""; }
public sealed class ReplyRevision { public Guid Id {get;set;}=Guid.NewGuid();public Guid ReplyId{get;set;}public string AuthorId{get;set;}="";public string Body{get;set;}="";public DateTime CreatedAt{get;set;}=DateTime.UtcNow; }
public sealed class PublicHealthShare {public string UserId{get;set;}="";public string Mode{get;set;}="none";public int? Score{get;set;}public string Band{get;set;}="unassessed";public int? Lower{get;set;}public int? Upper{get;set;}public int Coverage{get;set;}public DateTime CapturedAt{get;set;}public bool IncludeRanking{get;set;} }
