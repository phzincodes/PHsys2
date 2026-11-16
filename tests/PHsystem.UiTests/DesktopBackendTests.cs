using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PHsystem.Desktop;
using PHsystem.Server;
using PHsystem.SharedContracts.V1;
using Xunit;

namespace PHsystem.UiTests;

public sealed class DesktopBackendTests
{
    [Fact]
    public async Task DesktopGoogleNotConfiguredIsHonestAndCreatesNoAccount()
    {
        using var certificates=new Certificates();await using var factory=new IdentityFactory(certificates.Server);
        using var client=TrustedClient(new SessionVault(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"),"session.bin")),certificates.Root);
        var model=new AuthenticationViewModel(client,new(client,new FixedEndpoint(factory.Address)));
        model.GoogleLoginCommand.Execute(null);await Complete();Assert.Contains("termos",model.Status);
        model.AcceptTerms=true;model.GoogleLoginCommand.Execute(null);await Complete();Assert.Contains("não configurado",model.Status);Assert.False(model.IsGooglePending);
        using var scope=factory.Services.CreateScope();Assert.Empty(await scope.ServiceProvider.GetRequiredService<CommunityDb>().Users.ToListAsync());
        async Task Complete(){var deadline=DateTime.UtcNow.AddSeconds(15);while(!model.IsReady&&DateTime.UtcNow<deadline)await Task.Delay(10);Assert.True(model.IsReady);}
    }
    [Fact]
    public async Task DesktopLikesAreConfirmedPersistedAndDoNotResetTheFeed()
    {
        using var certificates = new Certificates();
        await using var factory = new IdentityFactory(certificates.Server);
        var address = factory.Address;
        var password = "Aa1!" + Guid.NewGuid().ToString("N");
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<CommunityUser>>();
            foreach (var name in new[] { "AuthorFixture", "ReaderFixture" })
                Assert.True((await users.CreateAsync(new CommunityUser { UserName = name, Email = name + "@example.invalid", EmailConfirmed = true }, password)).Succeeded);
        }
        var vaultPath = Path.Combine(Path.GetTempPath(), "PHsystem-like-test", Guid.NewGuid().ToString("N"));
        using var author = TrustedClient(new SessionVault(Path.Combine(vaultPath, "a.bin")), certificates.Root);
        using var reader = TrustedClient(new SessionVault(Path.Combine(vaultPath, "b.bin")), certificates.Root);
        author.Connect(address.AbsoluteUri); reader.Connect(address.AbsoluteUri);
        await author.SendAsync(HttpMethod.Post, "/api/v1/account/login", new LoginRequest("AuthorFixture", password, null));
        await reader.SendAsync(HttpMethod.Post, "/api/v1/account/login", new LoginRequest("ReaderFixture", password, null));
        await reader.GetAsync<System.Text.Json.JsonElement>("/api/v1/account/me");
        await author.SendAsync(HttpMethod.Post, "/api/v1/community/posts", new PostRequest("Question about a component", "Which documented specifications should I check?", "hardware"));
        using var model = new CommunityViewModel(new PHsystem.Diagnostics.SnapshotStore(), reader, new(reader, new FixedEndpoint(address)));
        model.LoadFeedCommand.Execute(null); await Complete();
        var post = Assert.Single(model.Posts); model.SelectedPost = post;
        Assert.True(model.UpvoteCommand.CanExecute(post));
        model.UpvoteCommand.Execute(post); model.UpvoteCommand.Execute(post); await Complete();
        Assert.Equal(1, Assert.Single(model.Posts).Likes); Assert.Equal(1, model.SelectedPost!.MyVote);
        Assert.False(model.IsFeedLoading); Assert.Contains("confirmada", model.Status);
        model.LoadFeedCommand.Execute(null); await Complete();
        Assert.Equal(1, Assert.Single(model.Posts).MyVote);
        model.UpvoteCommand.Execute(model.Posts[0]); await Complete();
        Assert.Equal(0, Assert.Single(model.Posts).Likes); Assert.Equal(0, model.SelectedPost!.MyVote);
        var own = Assert.Single((await author.GetAsync<FeedPage>("/api/v1/community/feed?page=1")).Items);
        Assert.False(model.UpvoteCommand.CanExecute(own));
        await reader.SendAsync(HttpMethod.Post, "/api/v1/account/logout"); reader.ClearSession();
        Assert.Empty(model.Posts); Assert.Null(model.SelectedPost);

        async Task Complete()
        {
            var until = DateTime.UtcNow.AddSeconds(15);
            while (!model.IsReady && DateTime.UtcNow < until) await Task.Delay(10);
            Assert.True(model.IsReady, model.Status);
        }
    }

    [Fact]
    public async Task ActualTlsRejectsUntrustedCertificateBeforeAuthentication()
    {
        using var certificates = new Certificates();
        await using var factory = new IdentityFactory(certificates.Server);
        var address = factory.Address;
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "session.bin");
        using var client = new CommunityClient(new SessionVault(file)); // Unmodified OS TLS validation.
        var service = new ApiConnectionService(client, new FixedEndpoint(address));
        var model = new AuthenticationViewModel(client, service) { Page = AuthenticationPage.Login, Username = "FixtureUser" };
        await model.SubmitAsync("SyntheticPass!123", "");
        Assert.Equal(ApiConnectionState.CertificateError, service.Current.State);
        Assert.False(model.CanLogin); Assert.True(model.IsReady);
        Assert.Equal(ApiConnectionService.UnavailableMessage, model.Status);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<CommunityDb>().Users.ToListAsync());
    }

    [Fact]
    public async Task DesktopAuthenticatesOverTlsWithRealIdentityAndRestoresRevokesAndRecoversSession()
    {
        using var certificates = new Certificates();
        await using var factory = new IdentityFactory(certificates.Server);
        var address = factory.Address;
        var directory = Path.Combine(Path.GetTempPath(), "PHsystem-contract-test", Guid.NewGuid().ToString("N"));
        var vault = new SessionVault(Path.Combine(directory, "session.bin"));
        try
        {
            using var client = TrustedClient(vault, certificates.Root);
            var service = new ApiConnectionService(client, new FixedEndpoint(address));
            var model = new AuthenticationViewModel(client, service)
            { Username = "ContractUser", Email = "contract@example.invalid", AcceptTerms = true, RememberSession = true };
            var authenticated = false; model.Authenticated += (_, _) => authenticated = true;
            await model.RestoreAsync(); if (!model.CanLogin) await client.CheckHealthAsync(CancellationToken.None); Assert.True(model.CanLogin, $"State={service.Current.State}; status={model.Status}; detail={model.ConnectionDetail}"); Assert.True(model.CanCreateAccount, $"Health={service.Current.Health}"); Assert.False(authenticated);
            var password = "Aa1!" + Guid.NewGuid().ToString("N");
            model.Page = AuthenticationPage.Register;
            await model.SubmitAsync(password, password);
            Assert.Equal(AuthenticationPage.ConfirmEmail, model.Page);
            var mail = Assert.Single(factory.Email.Sent);
            var lines = mail.Body.Split('\n');
            model.UserId = lines[0][4..]; model.Token = lines[1]["Token de confirmação: ".Length..];
            await model.SubmitAsync("", ""); Assert.Equal(AuthenticationPage.Login, model.Page);
            await model.SubmitAsync(password, ""); Assert.True(authenticated); Assert.True(File.Exists(Path.Combine(directory, "session.bin")));
            using (var scope = factory.Services.CreateScope())
            {
                var user = await scope.ServiceProvider.GetRequiredService<CommunityDb>().Users.SingleAsync();
                Assert.True(user.EmailConfirmed); Assert.NotEqual(password, user.PasswordHash); Assert.NotNull(user.PasswordHash);
            }
            // Duplicate username/email remain generic and create no additional account.
            await client.SendAsync(HttpMethod.Post, "/api/v1/account/register", new RegisterRequest(model.Username, "other@example.invalid", password, true));
            await client.SendAsync(HttpMethod.Post, "/api/v1/account/register", new RegisterRequest("AnotherName", model.Email, password, true));
            await client.SendAsync(HttpMethod.Post, "/api/v1/account/register", new RegisterRequest("WeakPassword", "weak@example.invalid", "weak", true));
            using (var scope = factory.Services.CreateScope()) Assert.Single(await scope.ServiceProvider.GetRequiredService<CommunityDb>().Users.ToListAsync());
            using var restoredClient = TrustedClient(vault, certificates.Root);
            var restored = new AuthenticationViewModel(restoredClient, new ApiConnectionService(restoredClient, new FixedEndpoint(address)));
            var restoredSession = false; restored.Authenticated += (_, _) => restoredSession = true;
            await restored.RestoreAsync(); Assert.True(restoredSession);
            await restoredClient.SendAsync(HttpMethod.Post, "/api/v1/account/refresh-session");
            Assert.NotNull(restoredClient.SessionExpiresAt);
            // A password/security-stamp revocation invalidates an encrypted cookie as well.
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<CommunityUser>>();
                await users.UpdateSecurityStampAsync((await users.FindByNameAsync(model.Username))!);
            }
            using var revokedClient = TrustedClient(vault, certificates.Root);
            var revoked = new AuthenticationViewModel(revokedClient, new ApiConnectionService(revokedClient, new FixedEndpoint(address)));
            var incorrectlyRestored = false; revoked.Authenticated += (_, _) => incorrectlyRestored = true;
            await revoked.RestoreAsync(); Assert.False(incorrectlyRestored); Assert.False(File.Exists(Path.Combine(directory, "session.bin")));
            model.Page = AuthenticationPage.Recovery; await model.SubmitAsync("", "");
            Assert.Equal(AuthenticationPage.ResetPassword, model.Page);
            var reset = factory.Email.Sent.Last().Body.Split('\n');
            model.UserId = reset[0][4..]; model.Token = reset[1]["Token válido por 30 minutos: ".Length..];
            var newPassword = "Bb2!" + Guid.NewGuid().ToString("N");
            await model.SubmitAsync(newPassword, newPassword); Assert.Equal(AuthenticationPage.Login, model.Page);
            authenticated = false; await model.SubmitAsync("WrongPass!123", ""); Assert.False(authenticated); Assert.True(model.IsReady);
            await model.SubmitAsync(newPassword, ""); Assert.True(authenticated);
            await client.SendAsync(HttpMethod.Post, "/api/v1/account/logout"); client.ClearSession();
            client.Connect(address.AbsoluteUri);
            Assert.False(File.Exists(Path.Combine(directory, "session.bin")));
            await Assert.ThrowsAsync<ApiResponseException>(() => client.GetAsync<System.Text.Json.JsonElement>("/api/v1/account/me"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    private static CommunityClient TrustedClient(SessionVault vault, X509Certificate2 root) => new(vault, cookies =>
    {
        // Trust is scoped ONLY to this test handler. No callback accepts invalid certificates,
        // and no certificate is installed into any Windows store.
        var policy = new X509ChainPolicy { TrustMode = X509ChainTrustMode.CustomRootTrust, RevocationMode = X509RevocationMode.NoCheck };
        policy.CustomTrustStore.Add(root);
        return new SocketsHttpHandler { CookieContainer = cookies, AllowAutoRedirect = false, SslOptions = new SslClientAuthenticationOptions { CertificateChainPolicy = policy } };
    });
    private sealed class Certificates : IDisposable
    {
        private readonly RSA rootKey = RSA.Create(2048);
        private readonly RSA serverKey = RSA.Create(2048);
        public X509Certificate2 Root { get; }
        public X509Certificate2 Server { get; }
        public Certificates()
        {
            var root = new CertificateRequest("CN=PHsystem Isolated Test Root", rootKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            root.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            root.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
            Root = root.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
            var request = new CertificateRequest("CN=localhost", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost"); names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
            using var issued = request.Create(Root, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(12), RandomNumberGenerator.GetBytes(16));
            using var withKey = issued.CopyWithPrivateKey(serverKey);
            Server = X509CertificateLoader.LoadPkcs12(withKey.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
        }
        public void Dispose() { Server.Dispose(); Root.Dispose(); rootKey.Dispose(); serverKey.Dispose(); }
    }
    private sealed class IdentityFactory : WebApplicationFactory<global::Program>
    {
        private readonly SqliteConnection database = new("Data Source=:memory:");
        private readonly string screening = Path.GetTempFileName();
        public Mailbox Email { get; } = new();
        public IdentityFactory(X509Certificate2 certificate)
        {
            File.WriteAllText(screening, Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())));
            UseKestrel(options => options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));
        }
        public Uri Address => new(Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Community", "Host=127.0.0.1;Database=isolated_fixture");
            builder.UseSetting("Security:CompromisedPasswordHashes", screening);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<CommunityDb>>(); services.RemoveAll<IDbContextOptionsConfiguration<CommunityDb>>();
                database.Open(); services.AddDbContext<CommunityDb>(options => options.UseSqlite(database));
                services.RemoveAll<IAccountEmail>(); services.AddSingleton<IAccountEmail>(Email);
            });
        }
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder); using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<CommunityDb>().Database.EnsureCreated(); return host;
        }
        public override async ValueTask DisposeAsync() { await base.DisposeAsync(); await database.DisposeAsync(); File.Delete(screening); }
    }
    private sealed class Mailbox : IAccountEmail
    {
        public bool Configured => true;
        public ConcurrentQueue<(string Subject, string Body)> Sent { get; } = [];
        public Task SendAsync(string email, string subject, string body, CancellationToken token) { Sent.Enqueue((subject, body)); return Task.CompletedTask; }
    }
}
