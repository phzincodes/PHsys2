using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PHsystem.Community;
namespace PHsystem.Desktop;
public sealed record PrivatePostDraft(string OwnerId,string Title,string Body,string Category,string Kind,Guid RequestId);
public sealed class PrivateDraftStore(string directory)
{
    private static byte[] Purpose(string owner)=>SHA256.HashData(Encoding.UTF8.GetBytes("PHsystem.Draft.v1:"+owner));
    private string FileFor(string owner)
    {if(string.IsNullOrWhiteSpace(owner)||owner.Length>128)throw new InvalidOperationException("Entre para usar rascunhos privados.");return Path.Combine(directory,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(owner)))+".draft");}
    public void Save(PrivatePostDraft draft)
    {
        if(draft.Title.Length>160||draft.Body.Length>8000||!CommunityPolicy.Categories.Contains(draft.Category)||!CommunityPolicy.PostKinds.Contains(draft.Kind)||draft.RequestId==Guid.Empty)throw new InvalidOperationException("Revise o tamanho e os campos do rascunho.");
        var file=FileFor(draft.OwnerId);var bytes=JsonSerializer.SerializeToUtf8Bytes(draft);
        try
        {
            var encrypted=ProtectedData.Protect(bytes,Purpose(draft.OwnerId),DataProtectionScope.CurrentUser);Directory.CreateDirectory(directory);
            var temporary=file+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllBytes(temporary,encrypted);File.Move(temporary,file,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        catch(CryptographicException){throw new InvalidOperationException("Não foi possível proteger o rascunho neste Windows.");}
        finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public PrivatePostDraft? Load(string owner)
    {
        var file=FileFor(owner);if(!File.Exists(file))return null;if(new FileInfo(file).Length>98304)throw new InvalidOperationException("Rascunho inválido.");
        byte[]? plain=null;
        try
        {
            plain=ProtectedData.Unprotect(File.ReadAllBytes(file),Purpose(owner),DataProtectionScope.CurrentUser);
            var draft=JsonSerializer.Deserialize<PrivatePostDraft>(plain);
            if(draft is null||draft.OwnerId!=owner||draft.Title is null||draft.Body is null||draft.Title.Length>160||draft.Body.Length>8000||draft.RequestId==Guid.Empty||!CommunityPolicy.Categories.Contains(draft.Category)||!CommunityPolicy.PostKinds.Contains(draft.Kind))throw new InvalidOperationException("Rascunho inválido.");
            return draft;
        }
        catch(Exception ex)when(ex is CryptographicException or JsonException){throw new InvalidOperationException("O rascunho não pôde ser validado para esta conta.");}
        finally{if(plain is not null)CryptographicOperations.ZeroMemory(plain);}
    }
    public void Delete(string owner){var file=FileFor(owner);if(File.Exists(file))File.Delete(file);}
}
