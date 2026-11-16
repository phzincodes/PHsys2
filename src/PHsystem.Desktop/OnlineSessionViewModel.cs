namespace PHsystem.Desktop;

public sealed partial class MainViewModel
{
    private string onlineUsername = "";
    public bool IsAuthenticated => onlineUsername.Length > 0;
    public bool IsOfflineUser => !IsAuthenticated;
    public string AccountLabel => IsAuthenticated ? onlineUsername : "Entrar na comunidade";
    public event EventHandler? OnlineAccessRequested;
    public UiCommand OpenAccountCommand => openAccountCommand ??= new(() =>
    {
        if (IsAuthenticated) { SocialTabIndex = 1; CommunityTabIndex = 0; SelectedPageIndex = 8; }
        else OnlineAccessRequested?.Invoke(this, EventArgs.Empty);
    });
    private UiCommand? openAccountCommand;
    internal void SetOnlineIdentity(string? username)
    {
        onlineUsername = username ?? "";
        Changed(nameof(IsAuthenticated)); Changed(nameof(IsOfflineUser)); Changed(nameof(AccountLabel));
    }
}
