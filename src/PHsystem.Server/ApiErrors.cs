using Microsoft.AspNetCore.Mvc;

namespace PHsystem.Server;

public static class ApiErrors
{
    public static void UseApiErrors(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var id = Guid.NewGuid().ToString("N");
            context.Items["PHsystem.CorrelationId"] = id;
            context.Response.Headers["X-Correlation-ID"] = id;
            context.Response.OnStarting(() => { context.Response.Headers["X-Correlation-ID"] = id; return Task.CompletedTask; });
            using var scope = app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id });
            await next(context);
        });
        app.UseStatusCodePages(async status =>
        {
            var context = status.HttpContext;
            var code = context.Response.StatusCode;
            var title = code switch { 400 => "Revise os dados enviados.", 401 => "Autenticação necessária ou sessão expirada.", 403 => "Você não tem permissão para esta ação.",
                404 => "Conteúdo não encontrado ou indisponível.", 409 => "Conflito: atualize o conteúdo e tente novamente.", 422 => "Os dados não atendem às regras desta operação.",
                429 => "Limite de requisições. Aguarde antes de tentar novamente.", _ => "Não foi possível concluir a solicitação." };
            var problem = new ProblemDetails { Status = code, Title = title, Type = $"urn:phsystem:http:{code}" };
            problem.Extensions["correlationId"] = context.Response.Headers["X-Correlation-ID"].ToString();
            await Results.Problem(problem).ExecuteAsync(context);
        });
    }
}
