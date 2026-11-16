using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PHsystem.Community;
using PHsystem.Server;
using PHsystem.SharedContracts.V1;
using Xunit;

namespace PHsystem.SecurityTests;

public sealed class CommunityApiTests
{
    [Fact]
    public async Task GoogleNativeBrokerTransfersOnlyToThePkceClientAndNeverThroughTheUrl()
    {
        await using var owner=new ApiFactory();await using var factory=owner.WithWebHostBuilder(builder=>
        {
            builder.UseSetting("Authentication:Google:ClientId","isolated-fixture-client");builder.UseSetting("Authentication:Google:ClientSecret","isolated-fixture-not-a-real-secret");builder.UseSetting("Authentication:Google:EnableNativeBroker","true");
            builder.ConfigureServices(services=>services.Configure<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>("Google",options=>options.Backchannel=new HttpClient(new GoogleTransport())));
        });
        using var native=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        using var browser=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        var verifier=Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var proof=Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        await SetCsrf(native);var start=await native.PostAsJsonAsync("/api/v1/auth/native/start",new NativeStartRequest(proof));Assert.Equal(HttpStatusCode.OK,start.StatusCode);
        var flow=(await start.Content.ReadFromJsonAsync<NativeStartResponse>())!;
        var challenge=await browser.GetAsync("/api/v1/auth/google?acceptTerms=true&flow="+flow.FlowId);var query=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
        Assert.DoesNotContain(flow.PollToken,challenge.Headers.Location.OriginalString);Assert.DoesNotContain(verifier,challenge.Headers.Location.OriginalString);
        await browser.GetAsync("/signin-google?code=isolated-code&state="+Uri.EscapeDataString(query["state"].ToString()));
        Assert.Equal(HttpStatusCode.Redirect,(await browser.GetAsync("/api/v1/auth/google/callback")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await browser.GetAsync("/api/v1/account/me")).StatusCode);
        var request=new NativeRedeemRequest(flow.FlowId,flow.PollToken,verifier);
        Assert.Equal(HttpStatusCode.BadRequest,(await native.PostAsJsonAsync("/api/v1/auth/native/redeem",request with {Verifier=new string('Z',43)})).StatusCode);
        var redeem=await native.PostAsJsonAsync("/api/v1/auth/native/redeem",request);Assert.Equal(HttpStatusCode.OK,redeem.StatusCode);Assert.Equal("authenticated",(await redeem.Content.ReadFromJsonAsync<NativeRedeemResponse>())!.Status);
        Assert.Equal(HttpStatusCode.OK,(await native.GetAsync("/api/v1/account/me")).StatusCode);
        await SetCsrf(native);Assert.Equal(HttpStatusCode.BadRequest,(await native.PostAsJsonAsync("/api/v1/auth/native/redeem",request)).StatusCode);
    }
    [Fact]
    public async Task InstallerDownloadHasRealMetadataAndDoesNotRequireDatabase()
    {
        var directory=Path.Combine(Path.GetTempPath(),"ph-release-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            byte[] bytes=[0x4d,0x5a,0,1,2,3];await File.WriteAllBytesAsync(Path.Combine(directory,"phsystem-next-preview-win-x64.exe"),bytes);
            var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            await File.WriteAllTextAsync(Path.Combine(directory,"release.json"),JsonSerializer.Serialize(new ReleaseReceipt(1,"0.3.0-fixture","win-x64",bytes.Length,hash,false,true,"Isolated fixture","Not a production executable")));
            await using var owner=new ApiFactory();await using var factory=owner.WithWebHostBuilder(builder=>{builder.UseSetting("Distribution:Directory",directory);builder.UseSetting("ConnectionStrings:Community","");});
            using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
            var receipt=await client.GetFromJsonAsync<ReleaseReceipt>("/api/v1/releases/current");Assert.Equal(hash,receipt!.Sha256);Assert.False(receipt.Signed);
            var response=await client.GetAsync("/download/windows");Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Equal(bytes,await response.Content.ReadAsByteArrayAsync());Assert.Equal(hash,response.Headers.ETag!.Tag.Trim('"'));
            Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/download/../AGENTS.md")).StatusCode);
        }
        finally{Directory.Delete(directory,true);}
    }
    [Fact]
    public async Task GoogleProtocolUsesPkceAndCreatesOnlyVerifiedProviderIdentityWithoutLocalPassword()
    {
        await using var owner=new ApiFactory();
        await using var factory=owner.WithWebHostBuilder(builder=>
        {
            builder.UseSetting("Authentication:Google:ClientId","isolated-fixture-client");builder.UseSetting("Authentication:Google:ClientSecret","isolated-fixture-not-a-real-secret");
            builder.ConfigureServices(services=>services.Configure<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>("Google",options=>options.Backchannel=new HttpClient(new GoogleTransport())));
        });
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        var challenge=await client.GetAsync("/api/v1/auth/google?acceptTerms=true");Assert.Equal(HttpStatusCode.Redirect,challenge.StatusCode);
        var query=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
        Assert.Equal("S256",query["code_challenge_method"].ToString());Assert.False(string.IsNullOrEmpty(query["code_challenge"]));
        var oauth=await client.GetAsync("/signin-google?code=isolated-code&state="+Uri.EscapeDataString(query["state"].ToString()));
        Assert.Equal(HttpStatusCode.Redirect,oauth.StatusCode);
        var finish=await client.GetAsync("/api/v1/auth/google/callback");Assert.Equal(HttpStatusCode.Redirect,finish.StatusCode);Assert.Equal("/comunidade",finish.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/account/me")).StatusCode);
        using var scope=factory.Services.CreateScope();var users=scope.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>();
        var user=await users.FindByLoginAsync("Google","isolated-google-subject");Assert.NotNull(user);Assert.True(user.EmailConfirmed);Assert.Null(user.PasswordHash);Assert.Empty(await users.GetRolesAsync(user));
    }
    [Fact]
    public async Task MissingGoogleConfigurationDoesNotPretendToAuthenticate()
    {
        await using var factory=new ApiFactory();using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        var providers=await client.GetFromJsonAsync<ExternalProvidersDto>("/api/v1/auth/providers");Assert.False(providers!.Google);Assert.False(providers.NativeGoogle);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.GetAsync("/api/v1/auth/google?acceptTerms=true")).StatusCode);
    }
    [Theory]
    [InlineData(false,false)]
    [InlineData(true,true)]
    public async Task GoogleRejectsUnverifiedEmailAndDoesNotLinkAnExistingAccount(bool verified,bool existing)
    {
        await using var owner=new ApiFactory();await using var factory=owner.WithWebHostBuilder(builder=>
        {
            builder.UseSetting("Authentication:Google:ClientId","isolated-fixture-client");builder.UseSetting("Authentication:Google:ClientSecret","isolated-fixture-not-a-real-secret");
            builder.ConfigureServices(services=>services.Configure<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>("Google",options=>options.Backchannel=new HttpClient(new GoogleTransport(verified))));
        });
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        if(existing){using var scope=factory.Services.CreateScope();var users=scope.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>();Assert.True((await users.CreateAsync(new CommunityUser{UserName="ExistingFixture",Email="google-fixture@example.invalid",EmailConfirmed=true})).Succeeded);}
        var challenge=await client.GetAsync("/api/v1/auth/google?acceptTerms=true");var query=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
        await client.GetAsync("/signin-google?code=isolated-code&state="+Uri.EscapeDataString(query["state"].ToString()));
        var finish=await client.GetAsync("/api/v1/auth/google/callback");Assert.Equal(HttpStatusCode.Redirect,finish.StatusCode);Assert.Contains(existing?"external=account-exists":"external=unverified",finish.Headers.Location!.OriginalString);
        using var verify=factory.Services.CreateScope();var manager=verify.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>();Assert.Null(await manager.FindByLoginAsync("Google","isolated-google-subject"));
    }
    private sealed class GoogleTransport(bool verified=true):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.Host=="oauth2.googleapis.com")
            {var body=await request.Content!.ReadAsStringAsync(token);Assert.Contains("code_verifier=",body);return new(HttpStatusCode.OK){Content=JsonContent.Create(new{access_token="isolated-access-token",token_type="Bearer",expires_in=300})};}
            Assert.Equal("openidconnect.googleapis.com",request.RequestUri.Host);Assert.Equal("Bearer",request.Headers.Authorization?.Scheme);
            return new(HttpStatusCode.OK){Content=JsonContent.Create(new{sub="isolated-google-subject",email="google-fixture@example.invalid",email_verified=verified})};
        }
    }
    [Fact]
    public async Task PublicWebServesRedEclipseAndKeepsApiAuthorization()
    {
        await using var factory=new ApiFactory();
        using var client=factory.CreateClient(new() { BaseAddress=new Uri("https://localhost"), AllowAutoRedirect=false });
        foreach(var route in new[]{"/","/entrar","/cadastro","/comunidade","/download","/privacidade"})
        { var response=await client.GetAsync(route); Assert.Equal(HttpStatusCode.OK,response.StatusCode); Assert.Contains("Red Eclipse",await response.Content.ReadAsStringAsync()); }
        Assert.Contains("--accent:#ff3347",await client.GetStringAsync("/style.css"));
        Assert.Contains("same-origin",await client.GetStringAsync("/app.js"));
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/v1/community/feed")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/AGENTS.md")).StatusCode);
    }
    [Fact]
    public async Task AuthorizationErrorsHaveSanitizedProblemDetailsAndServerGeneratedCorrelation()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "do-not-reflect-client-input");
        var unauthorized = await client.GetAsync("/api/v1/community/messages");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        var body = await unauthorized.Content.ReadFromJsonAsync<JsonElement>();
        var id = Assert.Single(unauthorized.Headers.GetValues("X-Correlation-ID"));
        Assert.Equal(32, id.Length); Assert.All(id, c => Assert.True(Uri.IsHexDigit(c)));
        Assert.Equal(id, body.GetProperty("correlationId").GetString()); Assert.Equal(401, body.GetProperty("status").GetInt32());
        Assert.DoesNotContain("do-not-reflect", await unauthorized.Content.ReadAsStringAsync());
        await RegisterAndLogin(client, factory, "CorrelationUser", "Aa1!" + Guid.NewGuid().ToString("N"));
        var forbidden = await client.GetAsync("/api/v1/admin/audit");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var health = (await client.GetFromJsonAsync<ApiHealthDto>("/health"))!;
        Assert.Equal(12, health.BuildId!.Length); Assert.Equal("current", health.MigrationState);
    }
    [Fact]
    public async Task HealthChecksSchemaAndDatabaseFailuresAreUnavailableWithoutInternalDetails()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var health = (await client.GetFromJsonAsync<ApiHealthDto>("/health"))!;
        Assert.Equal("PHsystem.Next", health.Service); Assert.Equal(1, health.ProtocolVersion);
        Assert.True(health.LoginAvailable); Assert.True(health.RegistrationAvailable);
        await SetCsrf(client);
        factory.BreakDatabase();
        var unhealthy = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unhealthy.StatusCode);
        Assert.False((await unhealthy.Content.ReadFromJsonAsync<ApiHealthDto>())!.LoginAvailable);
        var failure = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest("DatabaseFixture", "Synthetic!123", null));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
        var body = await failure.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Sqlite", body); Assert.DoesNotContain("AspNetUsers", body); Assert.DoesNotContain("Synthetic", body);
    }

    [Fact]
    public async Task HealthDistinguishesLoginFromUnconfiguredRegistrationAndEmailResendIsGeneric()
    {
        await using var owner = new ApiFactory();
        await using var factory = owner.WithWebHostBuilder(builder => builder.UseSetting("Security:CompromisedPasswordHashes", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var health = (await client.GetFromJsonAsync<ApiHealthDto>("/health"))!;
        Assert.True(health.LoginAvailable); Assert.False(health.RegistrationAvailable); Assert.Equal("degraded", health.Status);
        await SetCsrf(client);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/account/resend-confirmation", new ForgotPasswordRequest("missing@example.invalid"))).StatusCode);
    }
    [Fact]
    public async Task SocialFeedPaginatesAndProtectsVotesSavedPostsEditsAndNotifications()
    {
        await using var factory = new ApiFactory();
        using var alice = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var bob = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var alicePassword = "Aa1!" + Guid.NewGuid().ToString("N"); var bobPassword = "Aa1!" + Guid.NewGuid().ToString("N");
        var aliceId = await RegisterAndLogin(alice, factory, "SocialAlice", alicePassword);
        var bobId = await RegisterAndLogin(bob, factory, "SocialBob", bobPassword);
        var posts = Enumerable.Range(0, 25).Select(i => new ForumPost { AuthorId = aliceId, Title = "Question " + i, Body = "Evidence for question " + i,
            Category = "hardware", CreatedAt = DateTime.UtcNow.AddMinutes(-i) }).ToArray();
        using (var scope = factory.Services.CreateScope())
        { var db = scope.ServiceProvider.GetRequiredService<CommunityDb>(); db.Posts.AddRange(posts); await db.SaveChangesAsync(); }
        var first = (await bob.GetFromJsonAsync<FeedPage>("/api/v1/community/feed?page=1"))!;
        var second = (await bob.GetFromJsonAsync<FeedPage>("/api/v1/community/feed?page=2"))!;
        Assert.Equal(20, first.Items.Count); Assert.True(first.HasMore); Assert.Equal(5, second.Items.Count); Assert.False(second.HasMore);
        Assert.Empty(first.Items.Select(p => p.Id).Intersect(second.Items.Select(p => p.Id)));
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.GetAsync("/api/v1/community/feed?page=0")).StatusCode);
        var post = first.Items[0];
        Assert.Equal(HttpStatusCode.NoContent, (await bob.PutAsJsonAsync($"/api/v1/community/posts/{post.Id}/vote", new VoteRequest(1))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await bob.PutAsJsonAsync($"/api/v1/community/posts/{post.Id}/vote", new VoteRequest(1))).StatusCode);
        Assert.Equal(1, (await bob.GetFromJsonAsync<FeedPage>("/api/v1/community/feed?sort=relevant"))!.Items[0].Score);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PutAsJsonAsync($"/api/v1/community/posts/{post.Id}/vote", new VoteRequest(1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.PutAsJsonAsync($"/api/v1/community/posts/{post.Id}/vote", new VoteRequest(9))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await bob.PutAsJsonAsync($"/api/v1/community/posts/{post.Id}/saved", new SelectionRequest(true))).StatusCode);
        Assert.Single((await bob.GetFromJsonAsync<FeedPage>("/api/v1/community/feed?sort=saved"))!.Items);
        Assert.Empty((await alice.GetFromJsonAsync<FeedPage>("/api/v1/community/feed?sort=saved"))!.Items);
        Assert.Equal(HttpStatusCode.NoContent, (await bob.PutAsJsonAsync("/api/v1/community/categories/hardware/follow", new SelectionRequest(true))).StatusCode);
        Assert.True((await bob.GetFromJsonAsync<FeedPage>("/api/v1/community/feed?sort=followed"))!.HasMore);
        Assert.Empty((await alice.GetFromJsonAsync<FeedPage>("/api/v1/community/feed?sort=followed"))!.Items);
        var edit = new PostRequest("Edited question", "Updated evidence", "hardware");
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PutAsJsonAsync($"/api/v1/community/posts/{post.Id}", edit)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PutAsJsonAsync($"/api/v1/community/posts/{post.Id}", edit)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/v1/community/posts/{post.Id}/revisions")).StatusCode);
        Assert.Single((await alice.GetFromJsonAsync<JsonElement[]>($"/api/v1/community/posts/{post.Id}/revisions"))!);
        var replyResponse = await bob.PostAsJsonAsync($"/api/v1/community/posts/{post.Id}/replies", new ReplyRequest("Please check the documented symptoms."));
        Assert.Equal(HttpStatusCode.Created, replyResponse.StatusCode);
        var reply = (await replyResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var notes = (await alice.GetFromJsonAsync<NotificationDto[]>("/api/v1/community/notifications"))!;
        var note = Assert.Single(notes); Assert.Equal("NewReply", note.Kind);
        Assert.Empty((await bob.GetFromJsonAsync<NotificationDto[]>("/api/v1/community/notifications"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PutAsync($"/api/v1/community/notifications/{note.Id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PutAsync($"/api/v1/community/notifications/{note.Id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PostAsync($"/api/v1/community/posts/{post.Id}/solution/{reply}", null)).StatusCode);
        Assert.Equal("AcceptedSolution", Assert.Single((await bob.GetFromJsonAsync<NotificationDto[]>("/api/v1/community/notifications"))!).Kind);
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.PostAsJsonAsync($"/api/v1/community/posts/{first.Items[1].Id}/replies", new ReplyRequest("Different question", reply))).StatusCode);
        var parent = reply;
        for (var depth = 1; depth <= 4; depth++)
        {
            var nested = await bob.PostAsJsonAsync($"/api/v1/community/posts/{post.Id}/replies", new ReplyRequest("Nested evidence " + depth, parent));
            Assert.Equal(HttpStatusCode.Created, nested.StatusCode);
            parent = (await nested.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.PostAsJsonAsync($"/api/v1/community/posts/{post.Id}/replies", new ReplyRequest("Too deep", parent))).StatusCode);
        var thread = (await bob.GetFromJsonAsync<ReplyDto[]>($"/api/v1/community/posts/{post.Id}/replies"))!;
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, thread.Select(r => r.Depth));
        Assert.True(thread[0].Accepted);
        Assert.All(thread, r => Assert.Equal("Unverified", r.ReviewState));
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PutAsJsonAsync("/api/v1/community/hardware-sharing", new HardwareShareRequest(new Dictionary<string, string> { ["cpu"] = @"C:\Users\Private\file.txt" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await bob.PutAsJsonAsync("/api/v1/community/profile", new ProfileRequest("Technical enthusiast"))).StatusCode);
        var profile = (await alice.GetFromJsonAsync<PublicProfileDto>("/api/v1/community/profiles/by-name/SocialBob"))!;
        Assert.Equal(1, profile.AcceptedSolutions); Assert.Empty(profile.Hardware); Assert.Equal("Technical enthusiast", profile.Bio);
        Assert.Equal(HttpStatusCode.NoContent, (await bob.PostAsJsonAsync("/api/v1/account/delete", new PasswordConfirmation(bobPassword))).StatusCode);
        using var finalScope = factory.Services.CreateScope(); var finalDb = finalScope.ServiceProvider.GetRequiredService<CommunityDb>();
        Assert.False(await finalDb.PostVotes.AnyAsync(v => v.UserId == bobId));
        Assert.False(await finalDb.SavedPosts.AnyAsync(v => v.UserId == bobId));
        Assert.False(await finalDb.SocialProfiles.AnyAsync(v => v.UserId == bobId));
        Assert.False(await finalDb.FollowedCategories.AnyAsync(v => v.UserId == bobId));
        Assert.False(await finalDb.SocialNotifications.AnyAsync(v => v.UserId == bobId));
    }
    [Fact]
    public async Task RegistrationRequiresExplicitTermsAcceptance()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        await SetCsrf(client);
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new RegisterRequest("TermsTest", "terms@example.invalid", "ValidPass!123"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>().FindByNameAsync("TermsTest"));
    }
    [Theory]
    [InlineData("Zphzin")]
    [InlineData("zPHZIN")]
    [InlineData("Admin")]
    [InlineData("admіn")]
    [InlineData("user/path")]
    public void ReservedAndConfusableUsernamesAreRejected(string name) => Assert.False(CommunityPolicy.ValidUsername(name));

    [Theory]
    [InlineData("powershell -EncodedCommand abc")]
    [InlineData("Desativar o Defender")]
    [InlineData("https://untrusted.invalid/fix.exe")]
    [InlineData("<script>alert(1)</script>")]
    public void UnsafeContentRequiresHumanReview(string text) => Assert.True(CommunityPolicy.RequiresReview(text));

    [Fact]
    public async Task UnconfiguredCommunityReturnsUnavailableWithoutDatabaseAccess()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/v1/community/posts")).StatusCode);
    }

    [Fact]
    public async Task AnonymousRequestsAndMissingCsrfAreRejected()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/community/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest("test", "", null))).StatusCode);
    }

    [Fact]
    public async Task RegistrationReportsUnavailableWhenPasswordScreeningIsMissing()
    {
        await using var owner = new ApiFactory();
        await using var factory = owner.WithWebHostBuilder(builder => builder.UseSetting("Security:CompromisedPasswordHashes", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        await SetCsrf(client);
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new RegisterRequest("ScreenTest", "screen@example.invalid", "Aa1!" + Guid.NewGuid().ToString("N"), true));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task IdentityForumPrivacyAndSessionRevocationWorkThroughHttp()
    {
        await using var factory = new ApiFactory();
        using var alice = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var bob = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var password = "Aa1!" + Guid.NewGuid().ToString("N");
        var aliceId = await RegisterAndLogin(alice, factory, "AliceTest", password);
        _ = await RegisterAndLogin(bob, factory, "BobTest", password);
        Assert.Equal(HttpStatusCode.Forbidden, (await alice.GetAsync("/api/v1/admin/audit")).StatusCode);
        var postResponse = await alice.PostAsJsonAsync("/api/v1/community/posts", new PostRequest("Minha dúvida", "Preciso entender o diagnóstico.", "hardware"));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var post = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        var postId = post.GetProperty("id").GetGuid();
        var replyResponse = await bob.PostAsJsonAsync($"/api/v1/community/posts/{postId}/replies", new ReplyRequest("Verifique os dados da coleta."));
        var reply = await replyResponse.Content.ReadFromJsonAsync<JsonElement>();
        var replyId = reply.GetProperty("id").GetGuid();
        Assert.Equal("Unverified", reply.GetProperty("reviewState").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsync($"/api/v1/community/posts/{postId}/solution/{replyId}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PostAsync($"/api/v1/community/posts/{postId}/solution/{replyId}", null)).StatusCode);
        var listing = await bob.GetAsync("/api/v1/community/posts?search=dúvida");
        Assert.Equal(HttpStatusCode.OK, listing.StatusCode);
        Assert.Single((await listing.Content.ReadFromJsonAsync<PostDto[]>())!);
        Assert.Single((await bob.GetFromJsonAsync<ReplyDto[]>($"/api/v1/community/posts/{postId}/replies"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsJsonAsync("/api/v1/community/messages", new MessageRequest(aliceId, "Olá"))).StatusCode);
        var privateFields = await bob.GetFromJsonAsync<Dictionary<string, string>>($"/api/v1/community/profiles/{aliceId}/hardware");
        Assert.Empty(privateFields!);
        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PutAsJsonAsync("/api/v1/community/hardware-sharing", new HardwareShareRequest(new Dictionary<string, string> { ["serial"] = "test-identifier" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PutAsJsonAsync("/api/v1/community/hardware-sharing", new HardwareShareRequest(new Dictionary<string, string> { ["ramTotal"] = "16 GiB" }))).StatusCode);
        Assert.Single((await bob.GetFromJsonAsync<Dictionary<string, string>>($"/api/v1/community/profiles/{aliceId}/hardware"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PutAsJsonAsync("/api/v1/community/hardware-sharing", new HardwareShareRequest(new Dictionary<string, string>()))).StatusCode);
        Assert.Empty((await bob.GetFromJsonAsync<Dictionary<string, string>>($"/api/v1/community/profiles/{aliceId}/hardware"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PutAsync("/api/v1/account/direct-messages/true", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await bob.PostAsJsonAsync("/api/v1/community/messages", new MessageRequest(aliceId, "Mensagem de teste"))).StatusCode);
        Assert.Single((await alice.GetFromJsonAsync<JsonElement[]>("/api/v1/community/messages"))!);
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync("/api/v1/account/export")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PostAsync("/api/v1/account/revoke-sessions", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await alice.GetAsync("/api/v1/account/me")).StatusCode);
    }

    private static async Task<string> RegisterAndLogin(HttpClient client, ApiFactory factory, string username, string password)
    {
        await SetCsrf(client);
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new RegisterRequest(username, username + "@example.invalid", password, true));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>();
            var user = await users.FindByNameAsync(username);
            Assert.NotNull(user);
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/account/confirm-email", new ConfirmEmailRequest(user.Id, token))).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest(username, password, null))).StatusCode);
        await SetCsrf(client); // Antiforgery tokens are bound to the authenticated identity.
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/account/me");
        return me.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task MfaIsRequiredForAdminAndTheSameTotpCannotBeReused()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var password = "Aa1!" + Guid.NewGuid().ToString("N");
        _ = await RegisterAndLogin(client, factory, "MfaTest", password);
        var setup = await client.PostAsJsonAsync("/api/v1/account/mfa/setup", new PasswordConfirmation(password));
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        var secret = (await setup.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("secret").GetString()!;
        await SetCsrf(client);
        var code = Totp(secret);
        var enabled = await client.PostAsJsonAsync("/api/v1/account/mfa/enable", new MfaCodeRequest(code));
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded);
            Assert.True((await users.AddToRoleAsync((await users.FindByNameAsync("MfaTest"))!, "Admin")).Succeeded);
        }
        await SetCsrf(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest("MfaTest", password, null))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest("MfaTest", password, code))).StatusCode);
        await SetCsrf(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest("MfaTest", password, code))).StatusCode);
    }

    [Fact]
    public async Task AccountDeletionRemovesPublicFieldsAndRevokesTheSession()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var password = "Aa1!" + Guid.NewGuid().ToString("N");
        var id = await RegisterAndLogin(client, factory, "DeleteTest", password);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/account/delete", new PasswordConfirmation(password))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/account/me")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>().FindByIdAsync(id);
        Assert.NotNull(user);
        Assert.Null(user.Email);
        Assert.Null(user.PasswordHash);
        Assert.StartsWith("deleted_", user.UserName);
    }

    private static string Totp(string secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var key = new List<byte>();
        var accumulator = 0; var bits = 0;
        foreach (var c in secret.TrimEnd('='))
        {
            accumulator = (accumulator << 5) | alphabet.IndexOf(char.ToUpperInvariant(c)); bits += 5;
            if (bits >= 8) { bits -= 8; key.Add((byte)(accumulator >> bits)); }
        }
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var hash = System.Security.Cryptography.HMACSHA1.HashData(key.ToArray(), counter);
        var offset = hash[^1] & 15;
        var value = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7fffffff;
        return (value % 1000000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task ResetTokenIsSingleUseAndAuthenticationRoutesAreRateLimited()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var password = "Aa1!" + Guid.NewGuid().ToString("N");
        var id = await RegisterAndLogin(client, factory, "ResetTest", password);
        string resetToken;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>();
            resetToken = await users.GeneratePasswordResetTokenAsync((await users.FindByIdAsync(id))!);
        }
        var request = new ResetPasswordRequest(id, resetToken, "Aa2!" + Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/account/reset-password", request)).StatusCode);
        await SetCsrf(client); // Reset revokes the previous cookie through its security stamp.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/account/reset-password", request)).StatusCode);
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/account/forgot-password", new ForgotPasswordRequest("missing@example.invalid"))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/account/forgot-password", new ForgotPasswordRequest("missing@example.invalid"))).StatusCode);
    }

    [Fact]
    public async Task TypedPostsAreIdempotentAndStaleAccountRequestsCannotWrite()
    {
        await using var factory=new ApiFactory();
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        var owner=await RegisterAndLogin(client,factory,"TypedAuthor","Aa1!"+Guid.NewGuid().ToString("N"));
        var request=new PostRequest("Pergunta de teste","Conteúdo isolado","hardware","question",ClientRequestId:Guid.NewGuid());
        var first=await client.PostAsJsonAsync("/api/v1/community/posts",request);Assert.Equal(HttpStatusCode.Created,first.StatusCode);
        var created=(await first.Content.ReadFromJsonAsync<PostDto>())!;
        var repeat=await client.PostAsJsonAsync("/api/v1/community/posts",request);Assert.Equal(HttpStatusCode.Created,repeat.StatusCode);
        Assert.Equal(created.Id,(await repeat.Content.ReadFromJsonAsync<PostDto>())!.Id);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/v1/community/posts",request with {Body="Alterado"})).StatusCode);
        client.DefaultRequestHeaders.Add("X-PHsystem-Actor","another-account");
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync("/api/v1/community/posts",request with {ClientRequestId=Guid.NewGuid()})).StatusCode);
        client.DefaultRequestHeaders.Remove("X-PHsystem-Actor");client.DefaultRequestHeaders.Add("X-PHsystem-Actor",owner);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/community/posts/"+created.Id)).StatusCode);
        using var scope=factory.Services.CreateScope();Assert.Equal(1,await scope.ServiceProvider.GetRequiredService<CommunityDb>().Posts.CountAsync());
    }

    private static async Task SetCsrf(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/v1/security/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly string passwordList = Path.GetTempFileName();
        public ApiFactory() => File.WriteAllText(passwordList,
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Guid.NewGuid().ToByteArray())));
        public void BreakDatabase() => connection.Close();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Community", "Host=127.0.0.1;Database=test_only");
            builder.UseSetting("Security:CompromisedPasswordHashes", passwordList);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<CommunityDb>>();
                services.RemoveAll<IDbContextOptionsConfiguration<CommunityDb>>();
                connection.Open();
                services.AddDbContext<CommunityDb>(options => options.UseSqlite(connection));
                services.RemoveAll<IAccountEmail>();
                services.AddSingleton<IAccountEmail, TestEmail>();
            });
        }
        protected override Microsoft.Extensions.Hosting.IHost CreateHost(Microsoft.Extensions.Hosting.IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<CommunityDb>().Database.EnsureCreated();
            return host;
        }
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await connection.DisposeAsync();
            File.Delete(passwordList);
        }
    }
    private sealed class TestEmail : IAccountEmail
    {
        public bool Configured => true;
        public Task SendAsync(string email, string subject, string body, CancellationToken token) => Task.CompletedTask;
    }
}
