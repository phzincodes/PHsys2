using System.Windows;
using System.Windows.Controls;

namespace PHsystem.Desktop.Views;

public partial class AuthenticationView : UserControl
{
    private void LayoutChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 820;
        BrandPanel.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        BrandColumn.Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
    }
    private AuthenticationViewModel Model => (AuthenticationViewModel)DataContext;
    public AuthenticationView() { InitializeComponent(); IsVisibleChanged += (_, _) => { if (!IsVisible) ClearPasswords(); }; }
    private void Navigate(AuthenticationPage page)
    {
        ClearPasswords(); Model.Page = page;
        LegalText.Text = page switch
        {
            AuthenticationPage.Terms => "Versão de desenvolvimento 2026-11-15. Recursos experimentais têm limitações descritas no aplicativo. Ações de limpeza exigem revisão e consentimento. Conteúdo da comunidade não é executado no computador. Não publique dados pessoais de terceiros. Textos sujeitos a revisão jurídica antes do lançamento.",
            AuthenticationPage.Privacy => "Diagnósticos são armazenados localmente. Criar conta envia apenas os dados do formulário à API escolhida. Hardware permanece privado até seleção e publicação explícitas por campo. A comunidade recebe conteúdo que você publicar. Exportação, exclusão de conta e retirada do compartilhamento estão nas configurações. Sem analytics por padrão. Política sujeita a revisão jurídica.",
            _ => ""
        };
    }
    private void CreateAccount(object sender, RoutedEventArgs e) => Navigate(AuthenticationPage.Register);
    private void OpenLogin(object sender, RoutedEventArgs e) => Navigate(AuthenticationPage.Login);
    private void OpenConfirmation(object sender, RoutedEventArgs e) => Navigate(AuthenticationPage.ConfirmEmail);
    private void OpenRecovery(object sender, RoutedEventArgs e) => Navigate(AuthenticationPage.Recovery);
    private void OpenTerms(object sender, RoutedEventArgs e) => Navigate(AuthenticationPage.Terms);
    private void OpenPrivacy(object sender, RoutedEventArgs e) => Navigate(AuthenticationPage.Privacy);
    private void Back(object sender, RoutedEventArgs e) => Navigate(AuthenticationPage.Welcome);
    private void PasswordChanged(object sender, RoutedEventArgs e) { if (DataContext is AuthenticationViewModel model) model.ValidatePassword(RegisterPassword.Password); }
    private async void Submit(object sender, RoutedEventArgs e)
    {
        try
        {
            var password = Model.Page switch { AuthenticationPage.Register => RegisterPassword.Password, AuthenticationPage.ResetPassword => ResetPassword.Password, _ => LoginPassword.Password };
            var confirmation = Model.Page == AuthenticationPage.ResetPassword ? ResetConfirmation.Password : RegisterConfirmation.Password;
            await Model.SubmitAsync(password, confirmation);
        }
        finally { ClearPasswords(); }
    }
    private void ClearPasswords() { LoginPassword.Clear(); RegisterPassword.Clear(); RegisterConfirmation.Clear(); ResetPassword.Clear(); ResetConfirmation.Clear(); }
    private void ViewUnloaded(object sender, RoutedEventArgs e) => ClearPasswords();
}
