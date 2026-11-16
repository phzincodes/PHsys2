using PHsystem.Desktop;
using Xunit;
namespace PHsystem.UiTests;
public sealed class PrivateDraftTests
{
 [Fact] public void DraftsAreEncryptedAndIsolatedByAccount()
 {
  var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ph-draft-test-"+Guid.NewGuid().ToString("N"));
  try {
   var store=new PrivateDraftStore(path);var draft=new PrivatePostDraft("account-a","Private title","Private body","hardware","question",Guid.NewGuid());
   store.Save(draft);Assert.Equal(draft,store.Load("account-a"));Assert.Null(store.Load("account-b"));
   var file=System.IO.Directory.GetFiles(path).Single();Assert.DoesNotContain("Private body",System.Text.Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(file)));
   System.IO.File.WriteAllBytes(file,[1,2,3]);Assert.Throws<InvalidOperationException>(()=>store.Load("account-a"));
   store.Delete("account-a");Assert.Null(store.Load("account-a"));
  } finally {if(System.IO.Directory.Exists(path))System.IO.Directory.Delete(path,true);}
 }
}
