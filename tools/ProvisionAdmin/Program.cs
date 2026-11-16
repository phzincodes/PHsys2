using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PHsystem.Server;

if (args.Length != 1 || args[0] != "--provision-owner" || Console.IsInputRedirected)
{
    Console.Error.WriteLine("Use interativamente: dotnet run --project tools/ProvisionAdmin -- --provision-owner. Requer autoridade local sobre o banco. Nunca execute em produção sem revisão.");
    return 2;
}
var builder = Host.CreateApplicationBuilder();
var connection = builder.Configuration["ConnectionStrings:Community"];
if (string.IsNullOrWhiteSpace(connection)) { Console.Error.WriteLine("Configure ConnectionStrings__Community por canal local seguro."); return 2; }
builder.Services.AddDbContext<CommunityDb>(options => options.UseNpgsql(connection));
builder.Services.AddLogging();
builder.Services.AddIdentityCore<CommunityUser>(options => { options.Password.RequiredLength = 16; options.User.RequireUniqueEmail = true; })
    .AddRoles<IdentityRole>().AddEntityFrameworkStores<CommunityDb>().AddDefaultTokenProviders()
    .AddPasswordValidator<CompromisedPasswordValidator>();
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600000);
using var host = builder.Build();
using var scope = host.Services.CreateScope();
var users = scope.ServiceProvider.GetRequiredService<UserManager<CommunityUser>>();
var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var db = scope.ServiceProvider.GetRequiredService<CommunityDb>();
if ((await db.Database.GetPendingMigrationsAsync()).Any()) { Console.Error.WriteLine("Aplique e revise as migrações antes do provisionamento."); return 2; }
if (await users.FindByNameAsync("Zphzin") is not null)
{
    Console.WriteLine("Conta proprietária já existe. Nenhuma credencial ou papel foi alterado. Recuperação exige processo separado com prova de autoridade.");
    return 0;
}
Console.Write("Email do proprietário: ");
var email = Console.ReadLine();
if (!System.Net.Mail.MailAddress.TryCreate(email, out _)) { Console.Error.WriteLine("Email inválido."); return 2; }
Console.Write("Nova senha forte (mínimo 16 caracteres; entrada oculta): ");
var password = ReadSecret();
Console.Write("Repita a nova senha (entrada oculta): ");
var confirmation = ReadSecret();
if (password != confirmation || password.Length is < 16 or > 128) { Console.Error.WriteLine("As senhas não coincidem ou têm tamanho inválido."); return 2; }
await using var transaction = await db.Database.BeginTransactionAsync();
var owner = new CommunityUser { UserName = "Zphzin", Email = email, EmailConfirmed = true, AcceptDirectMessages = false };
var created = await users.CreateAsync(owner, password);
password = ""; confirmation = "";
if (!created.Succeeded) { Console.Error.WriteLine("Provisionamento recusado: " + string.Join(", ", created.Errors.Select(e => e.Code))); return 2; }
await users.ResetAuthenticatorKeyAsync(owner);
Console.WriteLine("Cadastre manualmente no autenticador: emissor PHsystem Next; conta Zphzin.");
Console.WriteLine("Segredo TOTP exibido somente nesta sessão local: " + await users.GetAuthenticatorKeyAsync(owner));
Console.Write("Código atual do autenticador (entrada oculta): ");
var code = ReadSecret();
if (!await users.VerifyTwoFactorTokenAsync(owner, TokenOptions.DefaultAuthenticatorProvider, code))
{ Console.Error.WriteLine("MFA não confirmado. Transação será revertida; nenhum administrador liberado."); return 2; }
if (!(await users.SetTwoFactorEnabledAsync(owner, true)).Succeeded) return 2;
foreach (var role in PHsystem.Community.CommunityPolicy.Roles)
    if (!await roles.RoleExistsAsync(role) && !(await roles.CreateAsync(new IdentityRole(role))).Succeeded) return 2;
if (!(await users.AddToRoleAsync(owner, "Admin")).Succeeded) return 2;
db.Audit.Add(new() { ActorId = owner.Id, Action = "owner-provisioned-with-mfa", TargetId = owner.Id });
await db.SaveChangesAsync();
await transaction.CommitAsync();
Console.WriteLine("Proprietário criado com MFA. Nenhum segredo foi gravado em arquivo ou repositório pela ferramenta.");
return 0;

static string ReadSecret()
{
    var chars = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return new string(chars.ToArray()); }
        if (key.Key == ConsoleKey.Backspace) { if (chars.Count > 0) chars.RemoveAt(chars.Count - 1); continue; }
        if (!char.IsControl(key.KeyChar) && chars.Count < 128) chars.Add(key.KeyChar);
    }
}
