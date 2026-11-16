using System.IO;
using PHsystem.Desktop;
using PHsystem.Diagnostics;
using PHsystem.SharedContracts.V1;
using Xunit;
namespace PHsystem.UiTests;
public sealed class SessionPrivacyTests
{
    [Fact]
    public void EndingSessionClearsSocialDataWithoutErasingLocalDiagnostics()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "session.bin");
        using var client = new CommunityClient(new SessionVault(file));
        var store = new SnapshotStore();
        var snapshot = new PHsystem.Core.DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "fixture", []); store.Set(snapshot);
        using var model = new CommunityViewModel(store, client);
        model.Posts.Add(new(Guid.NewGuid(), "fixture", "Title", "Body", "hardware", DateTimeOffset.UtcNow));
        model.Notifications.Add(new(Guid.NewGuid(), Guid.NewGuid(), "NewReply", false, DateTimeOffset.UtcNow));
        model.ShareFields.Add(new("cpu", "private fixture")); model.Messages.Add("private message fixture"); model.PostBody = "private draft";
        client.ClearSession();
        Assert.Empty(model.Posts); Assert.Empty(model.Notifications); Assert.Empty(model.ShareFields); Assert.Empty(model.Messages);
        Assert.Equal("", model.PostBody); Assert.False(model.FeedLoaded); Assert.Null(model.Profile);
        Assert.Same(snapshot, store.Current);
    }
}
