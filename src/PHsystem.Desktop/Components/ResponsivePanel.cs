using System.Windows;
using System.Windows.Controls;
namespace PHsystem.Desktop.Components;
// Equal columns that wrap to the available width; every row uses its tallest
// measured child. No screen resolution or per-card position is hardcoded.
public sealed class ResponsivePanel : Panel
{
    public double MinimumColumnWidth { get; set; } = 300;
    public double Gap { get; set; } = 16;
    private UIElement[] VisibleChildren => InternalChildren.Cast<UIElement>().Where(c=>c.Visibility!=Visibility.Collapsed).ToArray();
    private int Columns(double width) => double.IsFinite(width) ? Math.Max(1,(int)((width+Gap)/(MinimumColumnWidth+Gap))) : 1;
    protected override Size MeasureOverride(Size available)
    {
        var width=double.IsFinite(available.Width)?available.Width:MinimumColumnWidth;
        var columns=Columns(width); var cell=Math.Max(0,(width-Gap*(columns-1))/columns); double height=0,row=0; var index=0;
        foreach(UIElement child in VisibleChildren) { child.Measure(new Size(cell,double.PositiveInfinity)); row=Math.Max(row,child.DesiredSize.Height); if(++index%columns==0){height+=row+Gap;row=0;} }
        if(index%columns!=0)height+=row+Gap;
        return new Size(width,Math.Max(0,height-Gap));
    }
    protected override Size ArrangeOverride(Size size)
    {
        var columns=Columns(size.Width); var cell=Math.Max(0,(size.Width-Gap*(columns-1))/columns); double y=0;
        var children=VisibleChildren;
        for(var start=0;start<children.Length;start+=columns)
        { var count=Math.Min(columns,children.Length-start); double row=0;for(var i=0;i<count;i++)row=Math.Max(row,children[start+i].DesiredSize.Height);
          for(var i=0;i<count;i++)children[start+i].Arrange(new Rect(i*(cell+Gap),y,cell,row)); y+=row+Gap; }
        return size;
    }
}
