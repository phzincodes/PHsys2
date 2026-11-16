using System.Text.Json;
using PHsystem.Core;
using PHsystem.LocalData;
using Xunit;

namespace PHsystem.UnitTests;

public sealed class AssetProviderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "PHsystem-asset-tests", Guid.NewGuid().ToString("N"));

    public AssetProviderTests() => Directory.CreateDirectory(root);

    [Fact]
    public void MissingAssetReturnsConfiguredFallbackAndCanBeRetried()
    {
        var provider = CreateProvider();
        var missing = provider.Get("icon.svg");
        Assert.Equal(AssetState.Missing, missing.State);
        Assert.Equal("neutro", missing.Fallback);
        Assert.Empty(missing.Bytes);
        File.WriteAllText(Path.Combine(root, "icon.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        Assert.Equal(AssetState.Available, provider.Get("icon.svg").State);
    }

    [Fact]
    public void CacheDetectsChangesAndPreviouslyReturnedBytesStayImmutable()
    {
        var provider = CreateProvider();
        var file = Path.Combine(root, "icon.svg");
        const string original = "<svg xmlns=\"http://www.w3.org/2000/svg\"/>";
        File.WriteAllText(file, original);
        var first = provider.Get("icon.svg");
        Assert.Same(first, provider.Get("ICON.SVG"));
        var changed = original + " ";
        File.WriteAllText(file, changed);
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(changed), provider.Get("icon.svg").Bytes.ToArray());
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(original), first.Bytes.ToArray());
        provider.ClearCache();
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(changed), provider.Get("icon.svg").Bytes.ToArray());
    }

    [Theory]
    [InlineData("../icon.svg")]
    [InlineData("C:/icon.svg")]
    [InlineData("icons\\icon.svg")]
    [InlineData("icon.svg:secret")]
    [InlineData("https://example.com/icon.svg")]
    [InlineData("unknown.png")]
    [InlineData("/icon.svg")]
    public void UncataloguedOrUnsafeRequestsAreRejected(string path)
    {
        var result = CreateProvider().Get(path);
        Assert.Equal(AssetState.Rejected, result.State);
        Assert.Empty(result.Bytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(129)]
    public void EmptyOrOversizedFilesAreRejected(int length)
    {
        var provider = CreateProvider();
        File.WriteAllBytes(Path.Combine(root, "icon.svg"), new byte[length]);
        Assert.Equal(AssetState.Rejected, provider.Get("icon.svg").State);
    }

    [Theory]
    [InlineData("../escape.png", 4)]
    [InlineData("icon.exe", 4)]
    [InlineData("icon.svg", 0)]
    [InlineData("icon.svg", 4194305)]
    [InlineData("CON.png", 4)]
    [InlineData("COM1/icon.svg", 4)]
    public void InvalidCatalogEntriesFailBeforeLoading(string path, int maxBytes)
    {
        WriteManifest(path, maxBytes);
        Assert.Throws<InvalidDataException>(() => new LocalAssetProvider(root));
    }

    [Fact]
    public void UnsupportedCatalogVersionIsRejected()
    {
        File.WriteAllText(Path.Combine(root, "assets-manifest.json"), "{\"version\":2,\"assets\":[]}");
        Assert.Throws<InvalidDataException>(() => new LocalAssetProvider(root));
    }

    [Fact]
    public void DuplicateCatalogPathsAreRejectedIgnoringCase()
    {
        File.WriteAllText(Path.Combine(root, "assets-manifest.json"), """
            {"version":1,"assets":[
              {"path":"icon.svg","fallback":"neutro","maxBytes":4},
              {"path":"ICON.SVG","fallback":"neutro","maxBytes":4}
            ]}
            """);
        Assert.Throws<InvalidDataException>(() => new LocalAssetProvider(root));
    }

    private LocalAssetProvider CreateProvider()
    {
        WriteManifest("icon.svg", 128);
        return new(root);
    }

    [Fact]
    public void CacheEvictsWhenTotalBytesExceedBudget()
    {
        var assets = Enumerable.Range(0, 5).Select(i => new
        {
            path = $"icon{i}.svg", fallback = "neutro", maxBytes = 4 * 1024 * 1024
        }).ToArray();
        File.WriteAllText(Path.Combine(root, "assets-manifest.json"),
            JsonSerializer.Serialize(new { version = 1, assets }));
        foreach (var asset in assets)
        {
            const string opening = "<svg xmlns=\"http://www.w3.org/2000/svg\">";
            const string closing = "</svg>";
            File.WriteAllText(Path.Combine(root, asset.path), opening + new string(' ', asset.maxBytes - opening.Length - closing.Length) + closing);
        }
        var provider = new LocalAssetProvider(root);
        var first = provider.Get(assets[0].path);
        Assert.Equal(AssetState.Available, first.State);
        foreach (var asset in assets.Skip(1))
            Assert.Equal(AssetState.Available, provider.Get(asset.path).State);
        Assert.NotSame(first, provider.Get(assets[0].path));
    }

    private void WriteManifest(string path, int maxBytes) => File.WriteAllText(
        Path.Combine(root, "assets-manifest.json"),
        JsonSerializer.Serialize(new { version = 1, assets = new[] { new { path, required = false, fallback = "neutro", maxBytes } } }));

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"/>")]
    [InlineData("<!DOCTYPE svg SYSTEM \"file:///private\"><svg xmlns=\"http://www.w3.org/2000/svg\"/>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><image href=\"https://external.invalid\"/></svg>")]
    public void ActiveOrExternalSvgContentIsRejected(string content)
    {
        var provider = CreateProvider();
        File.WriteAllText(Path.Combine(root, "icon.svg"), content);
        Assert.Equal(AssetState.Rejected, provider.Get("icon.svg").State);
    }

    [Fact]
    public void MissingConfiguredCatalogFallsBackToInstalledCatalog()
    {
        WriteManifest("icon.svg", 128);
        File.WriteAllText(Path.Combine(root, "icon.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        var provider = new CatalogAssetProvider(root, Path.Combine(root, "absent"));
        Assert.Equal(AssetState.Available, provider.Get("icon.svg").State);
    }
}

