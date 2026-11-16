namespace PHsystem.Desktop;

public enum AppPage { Home, Diagnostics, Cleanup, Storage, Drivers, Core, Game, History, Community, Privacy, Hardware, Security, Performance, Settings }

public sealed class NavigationService
{
    public AppPage Current { get; private set; }
    public event EventHandler? Changed;
    public void Navigate(AppPage page)
    {
        if (!Enum.IsDefined(page)) throw new ArgumentOutOfRangeException(nameof(page));
        if (Current == page) return;
        Current = page; Changed?.Invoke(this, EventArgs.Empty);
    }
}
