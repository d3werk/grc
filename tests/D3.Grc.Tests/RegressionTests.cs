using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using D3.Grc.Core;
using Microsoft.EntityFrameworkCore;
using Xunit;
[assembly:CollectionBehavior(DisableTestParallelization=true)]
namespace D3.Grc.Tests;
public sealed class RegressionTests:IDisposable
{
    private const string Password="Correct horse battery staple!";private readonly string _root=Path.Combine(Path.GetTempPath(),"d3-v06-tests-"+Guid.NewGuid());
    private string P(string name){Directory.CreateDirectory(_root);return Path.Combine(_root,name);}
    private async Task<(LocalIdentity Identity,Workspace Workspace)> New(string name)
    {var identity=new LocalIdentity(P(name+"-identity"));await identity.RegisterAsync("tester",Password);Assert.True(await identity.LoginAsync("tester",Password));var w=new Workspace(identity);try{await w.CreateAsync(P(name+".d3grc"),name,Password);return(identity,w);}catch{w.Dispose();throw;}}
    [Fact]public async Task Registration_login_and_logout()
    {var identity=new LocalIdentity(P("identity"));await identity.RegisterAsync("tester",Password);Assert.False(await identity.LoginAsync("tester","incorrect"));Assert.True(await identity.LoginAsync("tester",Password));identity.Logout();Assert.Throws<UnauthorizedAccessException>(identity.Demand);}
    [Fact]public async Task Five_failures_temporarily_lock_profile()
    {var identity=new LocalIdentity(P("lockout"));await identity.RegisterAsync("tester",Password);for(int i=0;i<5;i++)Assert.False(await identity.LoginAsync("tester","wrong"));await Assert.ThrowsAsync<InvalidOperationException>(()=>identity.LoginAsync("tester",Password));}
    [Fact]public async Task Workspace_requires_login()
    {var identity=new LocalIdentity(P("anonymous"));using var w=new Workspace(identity);await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>w.CreateAsync(P("anonymous.d3grc"),"Test",Password));}
    [Fact]public async Task Cipher_roundtrip_and_tamper()
    {using var c=new Cipher();await c.InitializeAsync(Password);var a=c.Encrypt([1,2,3]);var b=c.Encrypt([1,2,3]);Assert.False(a.SequenceEqual(b));Assert.Equal(new byte[]{1,2,3},c.Decrypt(a));a[36]^=1;Assert.ThrowsAny<CryptographicException>(()=>c.Decrypt(a));}
    [Fact]public async Task Wrong_project_password_rejected()
    {using var a=new Cipher();await a.InitializeAsync(Password);var bytes=a.Encrypt([1]);using var b=new Cipher();await b.InitializeAsync("wrong",Cipher.Salt(bytes));Assert.ThrowsAny<CryptographicException>(()=>b.Decrypt(bytes));}
    [Fact]public async Task Migration_preserves_existing_custom_text()
    {
        var path=P("legacy.db");File.Copy(Path.Combine(AppContext.BaseDirectory,"content","catalog-v05.db"),path);using(var c=Database.Open(path)){Database.Exec(c,null,"UPDATE Templates SET CustomBody='Custom legacy text' WHERE Id='REQ-001'");Database.Exec(c,null,"UPDATE Records SET Body='Organization-specific content' WHERE Id='seed-REQ-001'");}
        await Database.MigrateAsync(path);using var check=Database.Open(path);Assert.Equal("Custom legacy text",Database.Scalar(check,null,"SELECT CustomBody FROM Templates WHERE Id='REQ-001'"));Assert.Equal("Organization-specific content",Database.Scalar(check,null,"SELECT Body FROM Records WHERE Id='seed-REQ-001'"));Assert.Equal("PRIVACY",Database.Scalar(check,null,"SELECT TopicId FROM RecordTopics WHERE RecordId='seed-REQ-001'"));
    }
    [Fact]public async Task Migration_is_idempotent_via_history()
    {var path=P("migration.db");File.Copy(Path.Combine(AppContext.BaseDirectory,"content","catalog-v05.db"),path);await Database.MigrateAsync(path);await Database.MigrateAsync(path);using var c=Database.Open(path);Assert.Equal("4",Database.Scalar(c,null,"SELECT COUNT(*) FROM __EFMigrationsHistory"));}
    [Fact]public async Task Source_filter_excludes_unrelated_topics()
    {var pair=await New("sources");using var w=pair.Workspace;var privacy=await w.SourcesAsync("PRIVACY");Assert.Contains(privacy,s=>s.Id=="GDPR");Assert.Contains(privacy,s=>s.Id=="BDSG");Assert.DoesNotContain(privacy,s=>s.Id is "AIACT" or "SPDX" or "BSI2004");}
    [Fact]public async Task Mismatched_primary_source_rejected()
    {var pair=await New("source-mismatch");using var w=pair.Workspace;await Assert.ThrowsAsync<ArgumentException>(()=>w.SaveEntryAsync(new Entry{Kind="Control",Title="Test",SourceId="AIACT"},"PRIVACY","Test"));}
    [Fact]public async Task Save_persists_main_file_and_reopens()
    {var pair=await New("save");using var w=pair.Workspace;var path=w.ProjectPath!;var e=new Entry{Kind="Risk",Title="Persisted test",SourceId="GDPR"};await w.SaveEntryAsync(e,"PRIVACY","Test");Assert.False(w.IsDirty);await w.CloseAsync();await w.OpenAsync(path,Password);Assert.Contains(await w.EntriesAsync("PRIVACY"),x=>x.Id==e.Id);}
    [Fact]public async Task Logout_closes_project_and_denies_access()
    {var pair=await New("logout");using var w=pair.Workspace;await w.LogoutAsync();Assert.False(w.IsOpen);Assert.Null(pair.Identity.User);await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>w.EntriesAsync());}
    [Fact]public async Task Save_as_keeps_original()
    {var pair=await New("save-as");using var w=pair.Workspace;var original=w.ProjectPath!;var copy=P("copy.d3grc");await w.SaveAsAsync(copy);Assert.True(File.Exists(original));Assert.Equal(copy,w.ProjectPath);}
    [Fact]public async Task Second_process_workspace_lock_rejected()
    {var pair=await New("project-lock");using var w=pair.Workspace;using var other=new Workspace(pair.Identity);await Assert.ThrowsAsync<IOException>(()=>other.OpenAsync(w.ProjectPath!,Password));}
    [Fact]public async Task Seed_does_not_fabricate_evidence_or_audits()
    {var pair=await New("seed");using var w=pair.Workspace;Assert.Empty(await w.EvidenceAsync());Assert.Empty(await w.EntriesAsync("","Audit"));Assert.All(await w.EntriesAsync(),e=>Assert.Equal("Entwurf",e.Status));}
    [Fact]public async Task Mapping_type_checked()
    {var pair=await New("mapping");using var w=pair.Workspace;var a=(await w.EntriesAsync("PRIVACY","Control"))[0];var b=(await w.EntriesAsync("PRIVACY","Requirement"))[0];await Assert.ThrowsAsync<ArgumentException>(()=>w.LinkAsync(a.Id,b.Id,"Anforderung → Control",a.SourceId,"Teilweise","","Test"));}
    [Fact]public async Task Audit_requires_scope_and_actual_evidence()
    {
        var pair=await New("audit");using var w=pair.Workspace;var a=new Entry{Kind="Audit",Title="Test audit",SourceId="GDPR",Status="In Prüfung"};await w.SaveEntryAsync(a,"PRIVACY","Test");await Assert.ThrowsAsync<ArgumentException>(()=>w.CompleteAuditAsync(a.Id,"Test"));
        var c=(await w.EntriesAsync("PRIVACY","Control")).First(x=>x.SourceId=="GDPR");await w.LinkAsync(a.Id,c.Id,"Audit → Control","GDPR","Teilweise","","Scope");await Assert.ThrowsAsync<ArgumentException>(()=>w.TestAsync(a.Id,c.Id,"GDPR",null,"Bestanden","Test"));
        await w.ImportEvidenceAsync(c.Id,"GDPR","Synthetic test evidence","Unit test fixture","test.txt",[1,2,3],DateTimeOffset.UtcNow.AddDays(1));var p=(await w.EvidenceAsync(c.Id)).Single();await w.TestAsync(a.Id,c.Id,"GDPR",p.Id,"Bestanden","Actual test fixture evaluated");await w.CompleteAuditAsync(a.Id,"All scope controls tested");Assert.Equal("Abgeschlossen",(await w.EntriesAsync("PRIVACY","Audit")).Single().Status);
    }
    [Fact]public async Task Source_failure_preserves_last_success()
    {var pair=await New("source-update");using var w=pair.Workspace;await w.StoreSourceResultAsync(new("GDPR",true,new string('A',64),"Synthetic test fixture"));var before=(await w.SourcesAsync()).Single(s=>s.Id=="GDPR").LastSuccess;await w.StoreSourceResultAsync(new("GDPR",false,"","Synthetic failure"));Assert.Equal(before,(await w.SourcesAsync()).Single(s=>s.Id=="GDPR").LastSuccess);}
    [Fact]public void Excel_contains_real_workbook_and_no_user_formula()
    {
        var bytes=ExcelExport.Build(new[]{new ExportSheet("Test",["Text","Anzahl"],new(){new[]{"=HYPERLINK(\"https://example.invalid\")","4"}})});using var zip=new ZipArchive(new MemoryStream(bytes));Assert.NotNull(zip.GetEntry("xl/workbook.xml"));using var stream=zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();var xml=XDocument.Load(stream);XNamespace s="http://schemas.openxmlformats.org/spreadsheetml/2006/main";Assert.Empty(xml.Descendants(s+"f"));Assert.Contains(xml.Descendants(s+"t"),t=>t.Value.StartsWith("=HYPERLINK"));Assert.Contains(xml.Descendants(s+"v"),v=>v.Value=="4");
    }
    [Fact]public async Task Export_uses_topic_and_scope()
    {var pair=await New("export");using var w=pair.Workspace;await w.SaveEntryAsync(new Entry{Kind="Risk",Title="Scope A",SourceId="GDPR",Scope="A"},"PRIVACY","Test");await w.SaveEntryAsync(new Entry{Kind="Risk",Title="Scope B",SourceId="GDPR",Scope="B"},"PRIVACY","Test");var sheets=await w.ExportSnapshotAsync("PRIVACY","A","");var risks=sheets.Single(s=>s.Name=="Risk");Assert.Single(risks.Rows);Assert.Equal("Scope A",risks.Rows[0][2]);}
    [Theory][InlineData("https://127.0.0.1/")][InlineData("http://eur-lex.europa.eu/")][InlineData("https://eur-lex.europa.eu.evil.invalid/")]
    public void Untrusted_source_url_rejected(string url)=>Assert.False(SourceUpdates.Allowed(new Uri(url)));
    public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);}
}
