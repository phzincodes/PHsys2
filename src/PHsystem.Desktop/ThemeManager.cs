using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace PHsystem.Desktop;

public sealed record ThemeDefinition(string Name, string Accent, string Background, string Surface, string Elevated,
    string Border, string Text = "#F3F4F6", string Secondary = "#A6ADBA", double FontScale = 1, double Radius = 10, string SecondaryAccent = "#BA8BFF", double SidebarWidth = 218, string Density = "Comfortable");

public static class ThemeManager
{
    public static ThemeDefinition[] Presets { get; } = [
        new("Red Eclipse", "#FF3347", "#07080B", "#111318", "#191C23", "#30343D"),
        new("Midnight", "#5599FF", "#060D18", "#101D30", "#192940", "#35465E"),
        new("Emerald", "#45D6A0", "#070D0C", "#11201B", "#1B2C25", "#324C40"),
        new("Violet", "#BA8BFF", "#0B0810", "#1A1324", "#261D34", "#443550"),
        new("Monochrome", "#D5D9E0", "#08090B", "#15171B", "#23262B", "#454A54"),
        new("High Contrast", "#FFFF00", "#000000", "#000000", "#121212", "#FFFFFF", "#FFFFFF", "#FFFFFF")];
    public static ThemeDefinition Validate(ThemeDefinition theme)
    {
        if (string.IsNullOrWhiteSpace(theme.Name) || theme.Name.Length > 48 || theme.Name.Any(char.IsControl)
            || new[] { theme.Accent, theme.Background, theme.Surface, theme.Elevated, theme.Border, theme.Text, theme.Secondary, theme.SecondaryAccent }.Any(c => c is null || !Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$"))
            || !double.IsFinite(theme.FontScale) || theme.FontScale is < .9 or > 1.5 || !double.IsFinite(theme.Radius) || theme.Radius is < 0 or > 24 || !double.IsFinite(theme.SidebarWidth) || theme.SidebarWidth is < 180 or > 320 || theme.Density is not ("Comfortable" or "Compact"))
            throw new InvalidDataException("Tema inválido. Use somente cores #RRGGBB, fonte 0,9–1,5 e raio 0–24.");
        foreach (var surface in new[] { theme.Background, theme.Surface, theme.Elevated })
            if (Contrast(theme.Text, surface) < 4.5 || Contrast(theme.Secondary, surface) < 4.5) throw new InvalidDataException("Contraste insuficiente: os textos precisam atingir 4,5:1 nas superfícies.");
        return theme;
    }
    private static double Luminance(string color)
    {
        var c = (Color)ColorConverter.ConvertFromString(color);
        double Linear(byte v) { double n = v / 255d; return n <= .04045 ? n / 12.92 : Math.Pow((n + .055) / 1.055, 2.4); }
        return .2126 * Linear(c.R) + .7152 * Linear(c.G) + .0722 * Linear(c.B);
    }
    public static double Contrast(string a, string b) { var x = Luminance(a); var y = Luminance(b); return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05); }
    public static ThemeDefinition Import(string file)
    {
        if (new FileInfo(file).Length > 8192) throw new InvalidDataException("Tema excede 8 KiB.");
        return Validate(JsonSerializer.Deserialize<ThemeDefinition>(File.ReadAllText(file), new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow }) ?? throw new InvalidDataException("Tema vazio."));
    }
    public static void Apply(ThemeDefinition theme)
    {
        Validate(theme); if (Application.Current is null) return;
        var resources = Application.Current.Resources;
        void Brush(string key, string color) => resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        Brush("BackgroundBrush", theme.Background); Brush("SidebarBrush", theme.Surface); Brush("PanelBrush", theme.Elevated);
        Brush("InsetBrush", theme.Background); Brush("HoverBrush", theme.Elevated); Brush("BorderBrush", theme.Border);
        Brush("TextBrush", theme.Text); Brush("SecondaryBrush", theme.Secondary); Brush("MutedBrush", theme.Secondary);
        Brush("AccentBrush", theme.Accent); Brush("AccentMutedBrush", theme.Surface);
        Brush("ActionTextBrush", Contrast(theme.Accent, "#000000") >= Contrast(theme.Accent, "#FFFFFF") ? "#000000" : "#FFFFFF");
        Brush("SecondaryAccentBrush", theme.SecondaryAccent);
        resources["ControlRadius"] = new CornerRadius(theme.Radius / 2);
        resources["ButtonPadding"] = theme.Density == "Compact" ? new Thickness(10,5,10,5) : new Thickness(14,8,14,8);
        resources["BodyFontSize"] = 13 * theme.FontScale; resources["PanelRadius"] = new CornerRadius(theme.Radius);
    }
}

public sealed partial class UiPreferences
{
    public ThemeDefinition CurrentTheme { get { try { return ThemeManager.Validate(values.Theme ?? ThemeManager.Presets[0]); } catch (InvalidDataException) { return ThemeManager.Presets[0]; } } }
    public string[] ThemeNames => ThemeManager.Presets.Concat(SavedThemes).Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Append("Personalizado").ToArray();
    public string ThemeName { get => ThemeManager.Presets.Concat(SavedThemes).Any(t => t == CurrentTheme) ? CurrentTheme.Name : "Personalizado"; set { var theme = ThemeManager.Presets.Concat(SavedThemes).FirstOrDefault(p => p.Name == value); if (theme is not null) SetTheme(theme); } }
    private string? customAccent, customName;
    public string CustomAccent { get => customAccent ?? CurrentTheme.Accent; set => customAccent=value; }
    public string CustomName { get => customName ?? "Meu tema"; set => customName=value; }
    public double FontScale { get => CurrentTheme.FontScale; set => SetTheme(CurrentTheme with { FontScale = Math.Clamp(value, .9, 1.5) }); }
    public double CornerRadius { get => CurrentTheme.Radius; set => SetTheme(CurrentTheme with { Radius = Math.Clamp(value, 0, 24) }); }
    public void SetTheme(ThemeDefinition theme) { values = values with { Theme = ThemeManager.Validate(theme) }; customAccent=background=surface=elevated=border=secondaryAccent=textColor=secondaryText=null; Save(); foreach(var name in new[]{nameof(ThemeName),nameof(FontScale),nameof(CornerRadius),nameof(SidebarSize),nameof(Density),nameof(CustomAccent),nameof(CustomBackground),nameof(CustomSurface),nameof(CustomElevated),nameof(CustomBorder),nameof(CustomSecondaryAccent),nameof(CustomTextColor),nameof(CustomSecondaryText)})Changed(name); }
    public UiCommand ApplyCustomThemeCommand => new(() => ThemeOperation(SaveCustomTheme));
    public UiCommand ExportThemeCommand => new(() => ThemeOperation(() => { var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Tema PHsystem|*.json", FileName = "phsystem-theme.json" }; if (dialog.ShowDialog() == true) File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(CurrentTheme, new JsonSerializerOptions { WriteIndented = true })); }));
    public UiCommand ImportThemeCommand => new(() => ThemeOperation(() => { var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Tema PHsystem|*.json" }; if (dialog.ShowDialog() == true) SetTheme(ThemeManager.Import(dialog.FileName)); }));
    private void ThemeOperation(Action action) { try { action(); } catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException) { status = "Não foi possível aplicar o tema. " + (ex is InvalidDataException ? ex.Message : "Verifique o arquivo e as permissões."); Changed(nameof(Status)); } }
}
