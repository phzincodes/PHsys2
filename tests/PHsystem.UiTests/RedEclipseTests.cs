using System.IO;
using PHsystem.Desktop;
using Xunit;
namespace PHsystem.UiTests;
public sealed class RedEclipseTests
{
    [Fact]
    public void HeaderlessNavigationExposesItsSelectedPageToAccessibilityClients()
    {
        Exception? failure=null;var thread=new Thread(()=>{System.Windows.Window? window=null;try
        {
            var host=new PHsystem.Desktop.Components.AccessiblePageHost();var button=new System.Windows.Controls.Button{Content="Analyze fixture"};
            System.Windows.Automation.AutomationProperties.SetAutomationId(button,"AccessibleAnalyzeFixture");
            host.Items.Add(new System.Windows.Controls.TabItem{Header="Hidden header",Content=new System.Windows.Controls.UserControl{Content=button}});host.SelectedIndex=0;
            window=new System.Windows.Window{Content=host,Width=400,Height=200,ShowInTaskbar=false};
            window.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary{Source=new Uri("/PHsystem.Desktop;component/Theme.xaml",UriKind.Relative)});
            host.Style=(System.Windows.Style)window.FindResource("PageHost");window.Show();window.UpdateLayout();
            var peer=System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(host)!;
            Assert.True(Find(peer));
            bool Find(System.Windows.Automation.Peers.AutomationPeer p)=>p.GetAutomationId()=="AccessibleAnalyzeFixture"||(p.GetChildren()?.Any(Find)??false);
        }catch(Exception ex){failure=ex;}finally{window?.Close();}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();Assert.Null(failure);
    }
    [Fact]
    public void ThemeLibraryAndDashboardOrderPersistWithoutHidingProtectedHealth()
    {
        var directory=Path.Combine(Path.GetTempPath(),"ph-personalization-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"preferences.json");var p=new UiPreferences(path);
            p.CustomName="Library one";p.ApplyCustomThemeCommand.Execute(null);Assert.Single(p.SavedThemes);
            p.CustomName="Library two";p.CustomAccent="#BA8BFF";p.ApplyCustomThemeCommand.Execute(null);Assert.Equal(2,p.SavedThemes.Count);
            p.ThemeName="Library one";Assert.Equal("Library one",p.CurrentTheme.Name);
            p.SetWidgetVisible("cpu",false);p.MoveWidget("history",-1);p.SidebarSize=280;p.Density="Compact";
            var restored=new UiPreferences(path);Assert.False(restored.WidgetVisible("cpu"));Assert.Equal("history",restored.WidgetOrder[2]);Assert.Equal(280,restored.SidebarSize);Assert.Equal("Compact",restored.Density);
            Assert.Throws<ArgumentException>(()=>restored.SetWidgetVisible("health",false));
        }finally{Directory.Delete(directory,true);}
    }
    [Fact]
    public void ReducedReportNeverIncludesRawFactsOrIdentity()
    {
        var snapshot=new PHsystem.Core.DiagnosticSnapshot(Guid.NewGuid(),DateTimeOffset.UtcNow,"fixture",[new("Sistema",PHsystem.Core.CollectionState.Partial,DateTimeOffset.UtcNow.AddMinutes(5),"fixture",.9,[],System.Collections.Immutable.ImmutableDictionary<string,string>.Empty.Add("Hostname","private-host-fixture").Add("Path",@"C:\Users\PrivateFixture"))]);
        var html=SafeDiagnosticReport.Create(snapshot);Assert.DoesNotContain("private-host-fixture",html);Assert.DoesNotContain("PrivateFixture",html);Assert.Contains("autodeclarado",html);
    }
    [Fact]
    public void FeedSelectorDisplaysTheLabelInsteadOfTheRecordName()
    {
        Exception? failure=null;var thread=new Thread(()=>{System.Windows.Window? window=null;try{
            var combo=new System.Windows.Controls.ComboBox { ItemsSource=new[]{new FeedOption("recent","Recentes")},DisplayMemberPath="Label",SelectedIndex=0 };
            window=new System.Windows.Window { Content=combo,Width=400,Height=120,ShowInTaskbar=false };
            window.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source=new Uri("/PHsystem.Desktop;component/Theme.xaml",UriKind.Relative) });
            window.Show();window.UpdateLayout();
            var texts=new List<string>();Collect(combo);
            Assert.Contains("Recentes",texts);Assert.DoesNotContain(texts,text=>text.Contains("FeedOption"));
            void Collect(System.Windows.DependencyObject parent){if(parent is System.Windows.Controls.TextBlock text)texts.Add(text.Text);for(var i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);i++)Collect(System.Windows.Media.VisualTreeHelper.GetChild(parent,i));}
        }catch(Exception ex){failure=ex;}finally{window?.Close();}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();Assert.Null(failure);
    }
    [Fact]
    public void ResponsiveCardsStayInsideLogicalViewportsWithoutOverlap()
    {
        Exception? failure=null;
        var thread=new Thread(()=>{try {
            foreach(var pixels in new[]{1280,1366,1920,2560,3840}) foreach(var dpi in new[]{1d,1.25,1.5,2})
            {
                var panel=new PHsystem.Desktop.Components.ResponsivePanel();
                for(var i=0;i<4;i++)panel.Children.Add(new System.Windows.Controls.Border { Height=80+i*10 });
                var width=Math.Max(200,pixels/dpi-280);
                panel.Measure(new System.Windows.Size(width,double.PositiveInfinity));panel.Arrange(new System.Windows.Rect(0,0,width,panel.DesiredSize.Height));
                for(var i=0;i<panel.Children.Count;i++)
                {
                    var a=System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot((System.Windows.FrameworkElement)panel.Children[i]); Assert.True(a.Right<=width+.01);
                    for(var j=i+1;j<panel.Children.Count;j++){var b=System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot((System.Windows.FrameworkElement)panel.Children[j]);Assert.False(a.IntersectsWith(b));}
                }
            }
        }catch(Exception ex){failure=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();Assert.Null(failure);
    }
    [Fact]
    public void ThemesPersistAndUnsafeImportsAreRejected()
    {
        var directory=Path.Combine(Path.GetTempPath(),"phsystem-theme-fixture",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path=Path.Combine(directory,"preferences.json"); var settings=new UiPreferences(path);
            Assert.Equal("Red Eclipse",settings.ThemeName);
            foreach(var preset in ThemeManager.Presets) Assert.Equal(preset,ThemeManager.Validate(preset));
            settings.ThemeName="Emerald"; Assert.Equal("Emerald",new UiPreferences(path).ThemeName);
            settings.SetTheme(settings.CurrentTheme with { Name="Custom", FontScale=1.2, Radius=4 });
            Assert.Equal(1.2,new UiPreferences(path).FontScale);
            Assert.Throws<InvalidDataException>(()=>settings.SetTheme(settings.CurrentTheme with { Accent="https://example.invalid/code.xaml" }));
            Assert.Throws<InvalidDataException>(()=>settings.SetTheme(settings.CurrentTheme with { Text="#111318" }));
            var file=Path.Combine(directory,"theme.json"); File.WriteAllText(file,"{\"$type\":\"unsafe\",\"Name\":\"Bad\"}"); Assert.Throws<System.Text.Json.JsonException>(()=>ThemeManager.Import(file));
            settings.CustomAccent="not-a-color"; settings.ApplyCustomThemeCommand.Execute(null); Assert.Contains("Não foi possível",settings.Status);
            Assert.Equal("Custom",new UiPreferences(path).CurrentTheme.Name);
        } finally { Directory.Delete(directory,true); }
    }
    [Theory]
    [InlineData("localhost",true)] [InlineData("127.0.0.1",true)] [InlineData("https://example.com/",false)] [InlineData("host & command",false)]
    public void NetworkTargetsAreDataNotCommands(string target,bool valid) => Assert.Equal(valid,NetworkLabViewModel.ValidTarget(target));
    [Fact]
    public void AssistedActionsOnlyAllowFixedOfficialDestinations()
    { Assert.True(MainViewModel.IsOfficialDestination("windowsdefender:")); Assert.False(MainViewModel.IsOfficialDestination("cmd.exe")); Assert.False(MainViewModel.IsOfficialDestination("https://untrusted.example")); }
}
