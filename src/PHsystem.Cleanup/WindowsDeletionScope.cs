using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace PHsystem.Cleanup;

/// <summary>Pins directory ancestors against rename and deletes an exclusively opened, revalidated file by handle.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsDeletionScope : IDisposable
{
    private readonly List<SafeFileHandle> directories = [];
    private readonly string root;
    private WindowsDeletionScope(string root) => this.root = root;

    public static WindowsDeletionScope Open(string root)
    {
        var scope = new WindowsDeletionScope(root);
        try
        {
            var ancestors = new Stack<string>();
            for (string? path = root; path is not null; path = Path.GetDirectoryName(path)) ancestors.Push(path);
            foreach (var path in ancestors)
            {
                var handle = CreateFile(path, 0x80, 1, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
                if (handle.IsInvalid) { handle.Dispose(); throw new IOException("Não foi possível fixar o diretório de temporários."); }
                scope.directories.Add(handle);
                if (!GetFileInformationByHandle(handle, out var info) || (info.Attributes & 0x400) != 0)
                    throw new IOException("Reparse point ou diretório inacessível.");
            }
            return scope;
        }
        catch { scope.Dispose(); throw; }
    }

    public bool Delete(CleanupCandidate candidate)
    {
        if (Path.GetFileName(candidate.Name) != candidate.Name || candidate.Name.Contains(':')) return false;
        using var handle = CreateFile(Path.Combine(root, candidate.Name), 0x10080, 0, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
        if (handle.IsInvalid || !GetFileInformationByHandle(handle, out var info)) return false;
        const uint forbidden = 0x400 | 0x1 | 0x4 | 0x10; // link, readonly, system, directory
        var size = ((long)info.SizeHigh << 32) | info.SizeLow;
        var written = DateTime.FromFileTimeUtc(((long)info.WriteTime.High << 32) | info.WriteTime.Low);
        if ((info.Attributes & forbidden) != 0 || info.Links != 1 || size != candidate.Bytes || written != candidate.LastWriteUtc
            || (candidate.HasFileId && candidate.FileId != ((ulong)info.IndexHigh << 32 | info.IndexLow)))
            return false;
        var disposition = new Disposition { Delete = true };
        return SetFileInformationByHandle(handle, 4, ref disposition, (uint)Marshal.SizeOf<Disposition>());
    }

    internal CleanupProbe Probe(CleanupCandidate candidate)
    {
        if (Path.GetFileName(candidate.Name) != candidate.Name || candidate.Name.Contains(':')) return new(candidate, CleanupSkipReason.OutsidePolicy);
        using var handle = CreateFile(Path.Combine(root, candidate.Name), 0x80000080, 0, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            var code = Marshal.GetLastWin32Error();
            return new(candidate, code is 32 or 33 ? CleanupSkipReason.InUse : code == 5 ? CleanupSkipReason.PermissionDenied : CleanupSkipReason.ReadError);
        }
        if (!GetFileInformationByHandle(handle, out var info)) return new(candidate, CleanupSkipReason.ReadError);
        if ((info.Attributes & 0x400) != 0) return new(candidate, CleanupSkipReason.ReparsePoint);
        if (info.Links != 1) return new(candidate, CleanupSkipReason.HardLink);
        if ((info.Attributes & (0x1 | 0x4 | 0x10)) != 0) return new(candidate, CleanupSkipReason.Protected);
        var size = ((long)info.SizeHigh << 32) | info.SizeLow;
        var written = DateTime.FromFileTimeUtc(((long)info.WriteTime.High << 32) | info.WriteTime.Low);
        if (size != candidate.Bytes || written != candidate.LastWriteUtc) return new(candidate, CleanupSkipReason.OutsidePolicy);
        return new(candidate with { HasFileId = true, FileId = (ulong)info.IndexHigh << 32 | info.IndexLow }, null);
    }

    public async Task<string?> HashAsync(CleanupCandidate candidate, CancellationToken token)
    {
        if (Path.GetFileName(candidate.Name) != candidate.Name || candidate.Name.Contains(':')) return null;
        using var handle = CreateFile(Path.Combine(root, candidate.Name), 0x80000080, 1, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
        if (handle.IsInvalid || !GetFileInformationByHandle(handle, out var info)
            || (info.Attributes & (0x400 | 0x4 | 0x10)) != 0 || info.Links != 1) return null;
        var size = ((long)info.SizeHigh << 32) | info.SizeLow;
        var written = DateTime.FromFileTimeUtc(((long)info.WriteTime.High << 32) | info.WriteTime.Low);
        if (size != candidate.Bytes || written != candidate.LastWriteUtc) return null;
        await using var stream = new FileStream(handle, FileAccess.Read);
        return Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, token));
    }

    public void Dispose()
    {
        foreach (var handle in directories.AsEnumerable().Reverse()) handle.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)] private struct FileTime { public uint Low, High; }
    [StructLayout(LayoutKind.Sequential)] private struct Information
    {
        public uint Attributes;
        public FileTime CreationTime, AccessTime, WriteTime;
        public uint VolumeSerial, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Disposition { [MarshalAs(UnmanagedType.Bool)] public bool Delete; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out Information information);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int infoClass, ref Disposition info, uint size);
}
