using Microsoft.Extensions.FileProviders;
namespace PHsystem.Server;
public static class PublicWeb
{
    public static void MapPublicWeb(this WebApplication app)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!Directory.Exists(root)) return;
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(root) });
        app.MapGet("/publicacao/{id:guid}", () => Results.File(Path.Combine(root, "index.html"), "text/html; charset=utf-8"));
        app.MapGet("/u/{name}", () => Results.File(Path.Combine(root, "index.html"), "text/html; charset=utf-8"));
        foreach (var route in new[] { "/", "/recursos", "/diagnosticos", "/download", "/comunidade", "/perguntas", "/tutoriais", "/perfil", "/configuracoes", "/entrar", "/cadastro", "/recuperar", "/notificacoes", "/atualizacoes", "/suporte", "/privacidade", "/termos", "/ranking", "/resolvidos", "/confirmar", "/redefinir" })
            app.MapGet(route, () => Results.File(Path.Combine(root, "index.html"), "text/html; charset=utf-8"));
    }
}
