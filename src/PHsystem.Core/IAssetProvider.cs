using System.Collections.Immutable;

namespace PHsystem.Core;

public enum AssetState { Available, Missing, Rejected }

// Bytes are inert: consumers must explicitly choose a decoder. SVG is never executed.
public sealed record AssetResult(AssetState State, string Fallback, ImmutableArray<byte> Bytes);

public interface IAssetProvider
{
    AssetResult Get(string path);
    void ClearCache();
}
