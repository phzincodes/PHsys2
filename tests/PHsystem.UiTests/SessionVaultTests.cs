using System.IO;
using System.Text;
using PHsystem.Desktop;
using Xunit;

namespace PHsystem.UiTests;

public sealed class SessionVaultTests
{
    [Fact]
    public void SessionRoundTripsUnderDpapiAndRejectsTamperingAndExpiration()
    {
        var directory = Path.Combine(Path.GetTempPath(), "PHsystem-session-test", Guid.NewGuid().ToString("N"));
        var file = Path.Combine(directory, "session.bin");
        var vault = new SessionVault(file);
        try
        {
            var session = new StoredSession("https://example.invalid/", "__Host-PHsystem=test-cookie-not-a-secret", DateTimeOffset.UtcNow.AddMinutes(1));
            vault.Save(session);
            Assert.Equal(session, vault.Load());
            Assert.DoesNotContain("test-cookie-not-a-secret", Encoding.UTF8.GetString(File.ReadAllBytes(file)));
            var bytes = File.ReadAllBytes(file); bytes[^1] ^= 0xff; File.WriteAllBytes(file, bytes);
            Assert.Null(vault.Load());
            vault.Save(session with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) });
            Assert.Null(vault.Load());
            Assert.False(File.Exists(file));
            vault.Save(session); vault.Clear(); Assert.Null(vault.Load());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
