using System.Collections.Immutable;
using System.Text.Json;
using PHsystem.Core;

namespace PHsystem.Community;

public sealed record CommunityDraft(string Title, string Body, ImmutableArray<string> Tags);

public static class CommunityDraftBuilder
{
    // Only aggregate states: no facts, raw errors, paths or machine identifiers.
    public static CommunityDraft Create(DiagnosticSnapshot? snapshot)
    {
        if (snapshot is null) throw new InvalidOperationException("Execute um diagnóstico antes de preparar um resumo.");
        var summary = string.Join(Environment.NewLine, snapshot.Categories.Select(c => $"- {c.Category}: {c.State}"));
        return new("Ajuda com diagnóstico local", $"Resumo revisável (nenhum envio automático):{Environment.NewLine}{summary}", ["diagnóstico"]);
    }
    public static string Export(CommunityDraft draft) => JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = true });
}
