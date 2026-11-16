using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
namespace PHsystem.Desktop.Components;
/// <summary>A headerless TabControl needs a content peer: the standard tab peer
/// searches for rendered TabItem headers, which our navigation intentionally omits.</summary>
public sealed class AccessiblePageHost : TabControl
{
    protected override AutomationPeer OnCreateAutomationPeer()=>new ContentPeer(this);
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {base.OnSelectionChanged(e);UIElementAutomationPeer.FromElement(this)?.InvalidatePeer();}
    private sealed class ContentPeer(AccessiblePageHost host):FrameworkElementAutomationPeer(host)
    {
        protected override string GetClassNameCore()=>nameof(AccessiblePageHost);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Pane;
        protected override List<AutomationPeer>? GetChildrenCore()
        {
            if(host.SelectedContent is UIElement element && UIElementAutomationPeer.CreatePeerForElement(element) is { } peer)return [peer];
            return base.GetChildrenCore();
        }
    }
}
