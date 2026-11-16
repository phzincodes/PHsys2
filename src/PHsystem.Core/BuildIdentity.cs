using System.Reflection;

namespace PHsystem.Core;

public sealed record BuildIdentity(string Version, string BuildId, string SourceCommit)
{
    public static BuildIdentity From(Assembly assembly)
    {
        var information = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "desenvolvimento";
        var parts = information.Split('+', 2);
        var commit = parts.Length == 2 ? new string(parts[1].TakeWhile(Uri.IsHexDigit).Take(12).ToArray()) : "";
        // MVID identifies the actual compiled module, including builds with uncommitted source changes.
        return new(parts[0], assembly.ManifestModule.ModuleVersionId.ToString("N")[..12], commit.Length > 0 ? commit : "não informado");
    }
}
