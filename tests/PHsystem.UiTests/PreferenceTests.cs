using System.IO;
using PHsystem.Desktop;
using Xunit;

namespace PHsystem.UiTests;

public sealed class PreferenceTests
{
    [Fact]
    public void PreferencesPersistAndSilentModeSuppressesEveryCue()
    {
        var directory = Path.Combine(Path.GetTempPath(), "PHsystem-preferences", Guid.NewGuid().ToString("N"));
        try
        {
            var file = Path.Combine(directory, "preferences.json");
            var preferences = new UiPreferences(file) { AnimationsEnabled = false, SoundEnabled = true, Volume = 0.4, NotificationVolume = 0.3 };
            var loaded = new UiPreferences(file);
            Assert.False(loaded.AnimationsEnabled);
            Assert.Equal(0.4, loaded.Volume);
            Assert.All(Enum.GetValues<InterfaceSound>(), sound => Assert.True(InterfaceAudio.CanPlay(loaded, sound)));
            loaded.SilentMode = true;
            Assert.All(Enum.GetValues<InterfaceSound>(), sound => Assert.False(InterfaceAudio.CanPlay(loaded, sound)));
            loaded.SilentMode = false; loaded.InterfaceSounds = false;
            Assert.False(InterfaceAudio.CanPlay(loaded, InterfaceSound.Click));
            Assert.True(InterfaceAudio.CanPlay(loaded, InterfaceSound.Reply));
            loaded.Volume = double.NaN; Assert.Equal(0.4, loaded.Volume);
            loaded.Volume = 2; Assert.Equal(1, loaded.Volume);
            loaded.SoundEnabled = false;
            Assert.All(Enum.GetValues<InterfaceSound>(), sound => Assert.False(InterfaceAudio.CanPlay(loaded, sound)));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
