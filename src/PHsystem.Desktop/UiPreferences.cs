using System.IO;
using System.Text.Json;

namespace PHsystem.Desktop;

public sealed partial class UiPreferences : ObservableObject
{
    private readonly string? file;
    private PreferenceValues values = new();
    private string status = "Preferências armazenadas somente neste computador.";
    public event EventHandler? Updated;
    public UiPreferences(string? file = null)
    {
        this.file = file;
        try { if (file is not null && File.Exists(file) && new FileInfo(file).Length < 16384) values = JsonSerializer.Deserialize<PreferenceValues>(File.ReadAllText(file)) ?? new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { status = "Não foi possível ler as preferências; valores padrão aplicados."; }
        values = values with { Volume = Math.Clamp(values.Volume, 0, 1), NotificationVolume = Math.Clamp(values.NotificationVolume, 0, 1) };
    }
    public string Status => status;
    public bool AnimationsEnabled { get => values.AnimationsEnabled; set { values = values with { AnimationsEnabled = value }; Save(); Changed(); } }
    public bool SoundEnabled { get => values.SoundEnabled; set { values = values with { SoundEnabled = value }; Save(); Changed(); } }
    public bool InterfaceSounds { get => values.InterfaceSounds; set { values = values with { InterfaceSounds = value }; Save(); Changed(); } }
    public bool SilentMode { get => values.SilentMode; set { values = values with { SilentMode = value }; Save(); Changed(); } }
    public double Volume { get => values.Volume; set { if (!double.IsFinite(value)) return; values = values with { Volume = Math.Clamp(value, 0, 1) }; Save(); Changed(); } }
    public double NotificationVolume { get => values.NotificationVolume; set { if (!double.IsFinite(value)) return; values = values with { NotificationVolume = Math.Clamp(value, 0, 1) }; Save(); Changed(); } }
    private void Save()
    {
        if (file is not null)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
                File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(values));
                File.Move(file + ".tmp", file, true);
                status = "Preferências salvas neste computador.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { status = "Preferências aplicadas nesta sessão; não foi possível salvar no disco."; }
        }
        Changed(nameof(Status)); Updated?.Invoke(this, EventArgs.Empty);
    }
    public sealed record PreferenceValues(bool AnimationsEnabled = true, bool SoundEnabled = false, bool InterfaceSounds = true, bool SilentMode = false, double Volume = 0.25, double NotificationVolume = 0.2, ThemeDefinition? Theme = null, ThemeDefinition[]? SavedThemes = null, string[]? WidgetOrder = null, string[]? HiddenWidgets = null);
}
