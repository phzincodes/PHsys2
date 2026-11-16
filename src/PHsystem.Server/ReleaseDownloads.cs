using System.Text.Json;
using System.Text.RegularExpressions;
namespace PHsystem.Server;
public sealed record ReleaseReceipt(int SchemaVersion,string Version,string Architecture,long Bytes,string Sha256,bool Signed,bool Preview,string Requirements,string Notes);
public static class ReleaseDownloads
{
    public static void MapReleaseDownloads(this WebApplication app)
    {
        (string Path,ReleaseReceipt Receipt)? Read()
        {
            var directory=app.Configuration["Distribution:Directory"];if(string.IsNullOrWhiteSpace(directory))return null;
            try
            {
                var metadata=Path.Combine(directory,"release.json");var binary=Path.Combine(directory,"phsystem-next-preview-win-x64.exe");
                if(!File.Exists(metadata)||!File.Exists(binary)||new FileInfo(metadata).Length>8192)return null;
                var receipt=JsonSerializer.Deserialize<ReleaseReceipt>(File.ReadAllText(metadata),new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if(receipt is null||receipt.SchemaVersion!=1||receipt.Version is null||!Regex.IsMatch(receipt.Version,"^[0-9A-Za-z.+-]{1,32}$")||receipt.Architecture!="win-x64"
                    ||receipt.Bytes<=0||receipt.Bytes!=new FileInfo(binary).Length||receipt.Sha256 is null||!Regex.IsMatch(receipt.Sha256,"^[a-fA-F0-9]{64}$"))return null;
                return(binary,receipt);
            }
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException){return null;}
        }
        app.MapGet("/api/v1/releases/current",(HttpContext context)=>
        {context.Response.Headers.CacheControl="no-store";return Read() is { } release?Results.Ok(release.Receipt):Results.NotFound();});
        app.MapGet("/download/windows",(HttpContext context)=>
        {
            if(Read() is not { } release)return Results.NotFound();
            context.Response.Headers.ETag='"'+release.Receipt.Sha256+'"';context.Response.Headers.CacheControl="public, max-age=0, must-revalidate";
            return Results.File(release.Path,"application/octet-stream","PHsystem-Next-"+release.Receipt.Version+"-win-x64.exe",enableRangeProcessing:true);
        });
    }
}
