using System.IO;
using System.Windows.Media;
using PHsystem.Core;

namespace PHsystem.Desktop;

public enum InterfaceSound { Click, Navigation, Panel, ScanStart, ScanComplete, Confirm, Error, Notification, Reply }

/// <summary>Local original PCM assets, one player per cue, serialized playback and throttled interactions.</summary>
public sealed class InterfaceAudio : IDisposable
{
    private readonly Dictionary<InterfaceSound, MediaPlayer> players = [];
    private readonly UiPreferences preferences;
    private DateTimeOffset lastPlayed;
    public string Status { get; private set; } = "Sons locais carregados.";
    public InterfaceAudio(IAssetProvider assets, UiPreferences preferences, string cacheDirectory)
    {
        this.preferences = preferences;
        preferences.Updated += PreferencesUpdated;
        foreach (var sound in Enum.GetValues<InterfaceSound>())
        {
            var file = sound switch { InterfaceSound.ScanStart => "scan-start", InterfaceSound.ScanComplete => "success", _ => sound.ToString().ToLowerInvariant() };
            var asset = assets.Get("sounds/" + file + ".wav");
            if (asset.State != AssetState.Available || asset.Bytes.IsDefaultOrEmpty) { Status = "Alguns sons locais estão ausentes; a interface permanece disponível."; continue; }
            try
            {
                Directory.CreateDirectory(cacheDirectory);
                var path = Path.Combine(cacheDirectory, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(asset.Bytes.AsSpan())) + ".wav");
                if (!File.Exists(path)) File.WriteAllBytes(path, asset.Bytes.ToArray());
                var player = new MediaPlayer();
                player.MediaFailed += (_, _) => Status = "Um som não pôde ser reproduzido neste equipamento.";
                player.Open(new Uri(path)); players.Add(sound, player);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
            { Status = "Áudio indisponível neste ambiente; a interface permanece disponível."; }
        }
    }
    public static bool CanPlay(UiPreferences preferences, InterfaceSound sound) => preferences.SoundEnabled && !preferences.SilentMode && preferences.Volume > 0
        && (sound is InterfaceSound.Notification or InterfaceSound.Reply ? preferences.NotificationVolume > 0 : preferences.InterfaceSounds);
    public void Play(InterfaceSound sound)
    {
        if (!CanPlay(preferences, sound) || !players.TryGetValue(sound, out var player)) return;
        var now = DateTimeOffset.UtcNow;
        if (now - lastPlayed < TimeSpan.FromMilliseconds(100)) return;
        lastPlayed = now;
        foreach (var other in players.Values) other.Stop();
        player.Volume = preferences.Volume * (sound is InterfaceSound.Notification or InterfaceSound.Reply ? preferences.NotificationVolume : 1);
        player.Position = TimeSpan.Zero; player.Play();
    }
    private void PreferencesUpdated(object? sender, EventArgs e) { if (!preferences.SoundEnabled || preferences.SilentMode) foreach (var player in players.Values) player.Stop(); }
    public void Dispose() { preferences.Updated -= PreferencesUpdated; foreach (var player in players.Values) player.Close(); players.Clear(); }
}
