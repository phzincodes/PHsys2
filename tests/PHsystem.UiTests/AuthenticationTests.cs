using PHsystem.Desktop;
using Xunit;

namespace PHsystem.UiTests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task MissingBackendNeverAuthenticates()
    {
        using var client = new CommunityClient();
        var model = new AuthenticationViewModel(client, new ApiConnectionService(client,new ApiEndpointConfiguration("missing-test-config.json","missing-test-discovery.json",false))) { Page = AuthenticationPage.Login, Username = "FixtureUser" };
        var authenticated = false;
        model.Authenticated += (_, _) => authenticated = true;
        await model.SubmitAsync("ValidPass!123", "");
        Assert.False(authenticated);
        Assert.Equal(ApiConnectionService.UnavailableMessage, model.Status);
        Assert.True(model.IsReady);
    }

    [Fact]
    public async Task RegistrationValidatesConfirmationAndConsentBeforeNetwork()
    {
        using var client = new CommunityClient();
        var model = new AuthenticationViewModel(client) { Page = AuthenticationPage.Register };
        await model.SubmitAsync("ValidPass!123", "different");
        Assert.Contains("não coincidem", model.Status);
        await model.SubmitAsync("ValidPass!123", "ValidPass!123");
        Assert.Contains("termos", model.Status);
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void LongPasswordsAreValidatedWithoutTruncation(int length, bool valid)
    {
        var password = "Aa1!" + new string('x', length - 4);
        Assert.Equal(valid, AuthenticationViewModel.PasswordValid(password));
        Assert.Equal(length, password.Length);
        Assert.False(AuthenticationViewModel.PasswordValid("alllowercase!123"));
    }
}
