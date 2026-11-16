namespace PHsystem.SharedContracts.V1;

public static class ApiProtocol { public const int Version = 1; }
public sealed record ApiHealthDto(string Service, int ProtocolVersion, string Status, bool LoginAvailable, bool RegistrationAvailable, bool RecoveryAvailable,
    string? ApiVersion = null, string? BuildId = null, string? MigrationState = null);

public sealed record RegisterRequest(string Username, string Email, string Password, bool AcceptTerms = false);
public sealed record LoginRequest(string Username, string Password, string? TwoFactorCode, bool RememberSession = false);
public sealed record ConfirmEmailRequest(string UserId, string Token);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string UserId, string Token, string NewPassword);
public sealed record PostRequest(string Title, string Body, string Category, string Kind = "discussion", int? ExpectedVersion = null, Guid? ClientRequestId = null);
public sealed record ReplyRequest(string Body, Guid? ParentId = null);
public sealed record ReportRequest(Guid PostId, string Reason);
public sealed record MessageRequest(string RecipientId, string Body);
public sealed record HardwareShareRequest(IReadOnlyDictionary<string, string> Fields);
public sealed record PostDto(Guid Id, string Author, string Title, string Body, string Category, DateTimeOffset CreatedAt,
    int Score = 0, int ReplyCount = 0, bool Saved = false, int MyVote = 0, bool IsOwner = false, bool Solved = false, int? Likes = null, string Kind = "discussion", bool Followed = false, int Version = 1);
public sealed record PostReactionDto(Guid Id, int Score, int Likes, int MyVote, bool Saved, int ReplyCount, bool Solved, bool Followed = false);
public sealed record FeedPage(IReadOnlyList<PostDto> Items, bool HasMore, int NextPage);
public sealed record VoteRequest(int Value);
public sealed record SelectionRequest(bool Selected);
public sealed record ProfileRequest(string Bio);
public sealed record PublicProfileDto(string Username, string Bio, int Posts, int Replies, int AcceptedSolutions, IReadOnlyDictionary<string, string> Hardware, int Reputation = 0, bool PublicVisible = false, string Accent = "#FF3347", PublicHealthDto? Health = null);
public sealed record NotificationDto(Guid Id, Guid PostId, string Kind, bool Read, DateTimeOffset CreatedAt);
public sealed record ReplyDto(Guid Id, string Author, string Body, string ReviewState, DateTimeOffset CreatedAt, Guid? ParentId = null, bool Accepted = false, int Depth = 0, bool IsOwner = false, int Version = 1);
public sealed record ReviewRequest(string Decision, string Evidence, string OfficialSource, string Scope);
public sealed record PasswordConfirmation(string Password);
public sealed record MfaCodeRequest(string Code);

public sealed record ExternalProvidersDto(bool Google, bool NativeGoogle);
public sealed record NativeStartRequest(string Challenge);
public sealed record NativeStartResponse(Guid FlowId,string PollToken,DateTimeOffset ExpiresAt);
public sealed record NativeRedeemRequest(Guid FlowId,string PollToken,string Verifier,bool RememberSession=false);
public sealed record NativeRedeemResponse(string Status);

public sealed record PublicProfileSettingsRequest(bool Enabled,string Bio,string Accent);
public sealed record PublicHealthShareRequest(string Mode,int? Score,string Band,int? Lower,int? Upper,int Coverage,DateTimeOffset CapturedAt,bool IncludeRanking=false);
public sealed record PublicHealthDto(string Mode,int? Score,string Band,int? Lower,int? Upper,int Coverage,DateTimeOffset CapturedAt,bool IncludeRanking,string Verification="SelfDeclared", bool Stale=false);
public sealed record HealthRankingDto(string Username,int Score,int Coverage,DateTimeOffset CapturedAt,string Verification="SelfDeclared");
public sealed record AdminStatsDto(int Users,int VisiblePosts,int PendingPosts,int PendingReplies,int OpenReports);
public sealed record AdminUserDto(string Id,string Username,IReadOnlyList<string> Roles,DateTimeOffset? LockedUntil);
public sealed record AdminUsersPage(IReadOnlyList<AdminUserDto> Items,bool HasMore);
public sealed record AdminRoleRequest(string Role,bool Enabled,string Password);
public sealed record AdminSuspensionRequest(int Days,string Password);
public sealed record ResolveReportRequest(string Decision);

public sealed record ReplyEditRequest(string Body,int ExpectedVersion);
public sealed record ReplyRemoveRequest(int ExpectedVersion);
