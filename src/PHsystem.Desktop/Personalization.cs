using System.Collections.ObjectModel;
using System.IO;
namespace PHsystem.Desktop;
public sealed partial class UiPreferences
{
    public IReadOnlyList<ThemeDefinition> SavedThemes => (values.SavedThemes??[]).Take(20).Where(IsValidTheme).ToArray();
    private static bool IsValidTheme(ThemeDefinition? theme) { try { if(theme is null)return false;ThemeManager.Validate(theme);return true; } catch(InvalidDataException){return false;} }
    private string? background,surface,elevated,border,secondaryAccent,textColor,secondaryText;
    public string CustomBackground { get=>background??CurrentTheme.Background;set=>background=value; }
    public string CustomSurface { get=>surface??CurrentTheme.Surface;set=>surface=value; }
    public string CustomElevated { get=>elevated??CurrentTheme.Elevated;set=>elevated=value; }
    public string CustomBorder { get=>border??CurrentTheme.Border;set=>border=value; }
    public string CustomSecondaryAccent { get=>secondaryAccent??CurrentTheme.SecondaryAccent;set=>secondaryAccent=value; }
    public string CustomTextColor { get=>textColor??CurrentTheme.Text;set=>textColor=value; }
    public string CustomSecondaryText { get=>secondaryText??CurrentTheme.Secondary;set=>secondaryText=value; }
    public double SidebarSize { get=>CurrentTheme.SidebarWidth;set=>SetTheme(CurrentTheme with { SidebarWidth=Math.Clamp(value,180,320) }); }
    public string[] Densities => ["Comfortable","Compact"];
    public string Density { get=>CurrentTheme.Density;set=>ThemeOperation(()=>SetTheme(CurrentTheme with { Density=value })); }
    private void SaveCustomTheme()
    {
        var theme=ThemeManager.Validate(CurrentTheme with { Name=CustomName,Accent=CustomAccent,Background=CustomBackground,Surface=CustomSurface,Elevated=CustomElevated,
            Border=CustomBorder,SecondaryAccent=CustomSecondaryAccent,Text=CustomTextColor,Secondary=CustomSecondaryText });
        if(ThemeManager.Presets.Any(p=>p.Name.Equals(theme.Name,StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException("Escolha um nome diferente dos temas predefinidos.");
        values=values with { SavedThemes=SavedThemes.Where(p=>!p.Name.Equals(theme.Name,StringComparison.OrdinalIgnoreCase)).TakeLast(19).Append(theme).ToArray() };
        SetTheme(theme);Changed(nameof(ThemeNames));
    }
    public UiCommand RestoreThemeCommand=>new(()=>SetTheme(ThemeManager.Presets[0]));
    public UiCommand RemoveThemeCommand=>new(()=>ThemeOperation(()=>
    {
        if(!SavedThemes.Any(t=>t.Name==CurrentTheme.Name))throw new InvalidDataException("Este tema não está na biblioteca personalizada.");
        values=values with { SavedThemes=SavedThemes.Where(t=>t.Name!=CurrentTheme.Name).ToArray() };SetTheme(ThemeManager.Presets[0]);Changed(nameof(ThemeNames));
    }));
    public static readonly string[] WidgetCodes=["cpu","memory","storage","history"];
    public string[] WidgetOrder => (values.WidgetOrder??WidgetCodes).Where(WidgetCodes.Contains).Distinct().Concat(WidgetCodes).Distinct().ToArray();
    public bool WidgetVisible(string code)=>!(values.HiddenWidgets??[]).Contains(code);
    public void SetWidgetVisible(string code,bool visible)
    { if(!WidgetCodes.Contains(code))throw new ArgumentException("Widget desconhecido.");values=values with { HiddenWidgets=(values.HiddenWidgets??[]).Where(c=>WidgetCodes.Contains(c)&&c!=code).Concat(visible?[]:new[]{code}).Distinct().ToArray() };Save(); }
    public void MoveWidget(string code,int direction)
    { if(direction is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(direction));var order=WidgetOrder;var i=Array.IndexOf(order,code);if(i<0||i+direction<0||i+direction>=order.Length)return;(order[i],order[i+direction])=(order[i+direction],order[i]);values=values with { WidgetOrder=order };Save(); }
}
public sealed class DashboardWidget(string code,string title,string value,string detail,bool enabled,Action<bool> change) : ObservableObject
{
    private bool enabled=enabled;
    public string Code=>code;public string Title=>title;public string Value=>value;public string Detail=>detail;
    public bool Enabled { get=>enabled;set { if(enabled==value)return;enabled=value;Changed();change(value); } }
}
public sealed partial class MainViewModel
{
    public ObservableCollection<DashboardWidget> DashboardWidgets { get; }=[];
    public UiParameterCommand MoveWidgetUpCommand=>new(value=>Preferences.MoveWidget((string)value!,-1),value=>value is string code&&Array.IndexOf(Preferences.WidgetOrder,code)>0);
    public UiParameterCommand MoveWidgetDownCommand=>new(value=>Preferences.MoveWidget((string)value!,1),value=>value is string code&&Array.IndexOf(Preferences.WidgetOrder,code) is var i&&i>=0&&i<3);
    private void UpdateDashboardWidgets()
    {
        DashboardWidgets.Clear();
        foreach(var code in Preferences.WidgetOrder)
        {
            var (title,value,detail)=code switch { "cpu"=>("PROCESSADOR",CpuModel,CpuDetail),"memory"=>("MEMÓRIA",MemoryTotal,MemoryDetail),"storage"=>("ARMAZENAMENTO",StorageAvailable,StorageDetail),_=>("HISTÓRICO",HistoryCountLabel,"Análises locais; não publicado na comunidade") };
            DashboardWidgets.Add(new(code,title,value,detail,Preferences.WidgetVisible(code),visible=>Preferences.SetWidgetVisible(code,visible)));
        }
        Changed(nameof(SidebarWidth));Changed(nameof(MoveWidgetUpCommand));Changed(nameof(MoveWidgetDownCommand));
    }
}
