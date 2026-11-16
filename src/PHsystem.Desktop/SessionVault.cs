using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PHsystem.Desktop;

public sealed record StoredSession(string Origin, string CookieHeader, DateTimeOffset ExpiresAt);

/// <summary>Opt-in persistence. DPAPI binds the encrypted cookie to the Windows user.</summary>
public sealed class SessionVault(string file)
{
    private static readonly byte[] Purpose = Encoding.UTF8.GetBytes("PHsystem.Next.Session.v1");
    public void Save(StoredSession session)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(session);
        try
        {
            var encrypted = ProtectedData.Protect(bytes, Purpose, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
            var temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temporary, encrypted); File.Move(temporary, file, true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    public StoredSession? Load()
    {
        if (!File.Exists(file)) return null;
        try
        {
            if (new FileInfo(file).Length > 32768) { Clear(); return null; }
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(file), Purpose, DataProtectionScope.CurrentUser);
            try
            {
                var session = JsonSerializer.Deserialize<StoredSession>(bytes);
                if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow) { Clear(); return null; }
                return session;
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        { return null; }
    }
    public void Clear() { if (File.Exists(file)) File.Delete(file); }
}
