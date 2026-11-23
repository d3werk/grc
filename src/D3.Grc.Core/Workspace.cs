using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;
namespace D3.Grc.Core;
public sealed partial class Workspace:IDisposable
{
    private readonly LocalIdentity identity;
    public Workspace(LocalIdentity identity){this.identity=identity;}
    private readonly SemaphoreSlim _gate=new(1,1);private Cipher? _cipher;private FileStream? _lock,_lease;private string? _dir;
    public string? ProjectPath{get;private set;}public string Name{get;private set;}="";public bool IsOpen=>ProjectPath is not null;public bool IsDirty{get;private set;}
    public string? PreservedRecoveryPath { get; private set; }
    public static string WorkRoot=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"D3GRC","work-v06");
    private string DbPath=>Path.Combine(_dir??throw new InvalidOperationException("Kein Projekt geöffnet."),"project.db");
    private void Demand(){identity.Demand();if(!IsOpen)throw new InvalidOperationException("Projekt öffnen.");}
    private async Task<T> Read<T>(Func<SqliteConnection,T> action)
    {await _gate.WaitAsync();try{Demand();using var c=Open(DbPath);return action(c);}finally{_gate.Release();}}
    private async Task<T> Change<T>(string action,string id,string detail,Func<SqliteConnection,SqliteTransaction,T> change,bool save=true)
    {
        await _gate.WaitAsync();
        try
        {
            Demand();
            T result;
            using (var connection = Open(DbPath))
            using (var transaction = connection.BeginTransaction())
            {
                result = change(connection, transaction);
                Log(connection, transaction, action, id, detail);
                transaction.Commit();
            }
            IsDirty = true;
            try
            {
                await Snapshot(ProjectPath! + ".recovery");
                if (save)
                    await WriteProjectAsync();
            }
            catch (Exception ex)
            {
                throw new WorkspacePersistenceException(ex);
            }
            return result;
        }
        finally { _gate.Release(); }
    }
    private void Prepare(string path)
    {
        if(new FileInfo(path).LinkTarget is not null||new FileInfo(path+".lock").LinkTarget is not null)throw new IOException("Symlink-Projektpfad nicht erlaubt.");
        _lock=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);Files.PrivateDirectory(WorkRoot);_dir=Path.Combine(WorkRoot,Guid.NewGuid().ToString("N"));Files.PrivateDirectory(_dir);
        _lease=new FileStream(Path.Combine(_dir,".active"),FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None);using var f=Files.Create(DbPath);
    }
    public async Task CreateAsync(string path, string name, string password, string? catalog = null,
        CompanyProfile? companyProfile = null)
    {
        identity.Demand();if(password.Length<12||string.IsNullOrWhiteSpace(name)||name.Length>240)throw new ArgumentException("Projektname erforderlich; Passwort mindestens 12 Zeichen.");
        if (companyProfile is not null)
            companyProfile = CompanyPlanning.Validate(companyProfile);
        await _gate.WaitAsync();var wasOpen=IsOpen;try{
            if(wasOpen)throw new InvalidOperationException("Aktuelles Projekt schließen.");path=Path.GetFullPath(path);Prepare(path);if(File.Exists(path)||File.Exists(path+".recovery"))throw new IOException("Zieldatei existiert.");
            File.Copy(catalog??Path.Combine(AppContext.BaseDirectory,"content","catalog-v05.db"),DbPath,true);if(!OperatingSystem.IsWindows())File.SetUnixFileMode(DbPath,UnixFileMode.UserRead|UnixFileMode.UserWrite);
            await Database.MigrateAsync(DbPath);_cipher=new Cipher();await _cipher.InitializeAsync(password);ProjectPath=path;Name=name.Trim();
            using (var connection = Open(DbPath))
            using (var transaction = connection.BeginTransaction())
            {
                Exec(connection, transaction, "UPDATE Metadata SET Value=$p0 WHERE Key='ProjectName'", Name);
                Log(connection, transaction, "ProjectCreated", "", Name);
                if (companyProfile is not null)
                {
                    PersistCompanyProfile(connection, transaction, companyProfile);
                    Log(connection, transaction, "CompanyProfileConfigured", "", JsonSerializer.Serialize(companyProfile));
                }
                transaction.Commit();
            }
            await Snapshot(path);
            IsDirty = false;
        }catch{if(!wasOpen)Cleanup();throw;}finally{_gate.Release();}
    }
    public async Task OpenAsync(string path,string password,bool recovery=false)
    {
        identity.Demand();await _gate.WaitAsync();var wasOpen=IsOpen;try{
            if(wasOpen)throw new InvalidOperationException("Aktuelles Projekt schließen.");path=Path.GetFullPath(path);Prepare(path);
            var sourcePath = recovery ? path + ".recovery" : path;
            var bytes = await Files.ReadAsync(sourcePath, Cipher.Limit + 52);
            _cipher=new Cipher();await _cipher.InitializeAsync(password,Cipher.Salt(bytes));var plain=_cipher.Decrypt(bytes);
            try{await using var output=new FileStream(DbPath,FileMode.Truncate,FileAccess.Write,FileShare.None);await output.WriteAsync(plain);}finally{CryptographicOperations.ZeroMemory(plain);}
            string? backupPrefix;
            using(var c=Open(DbPath)){
                if(Scalar(c,null,"SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'")=="")throw new InvalidDataException("Keine bekannte Migrationshistorie.");
                if(Scalar(c,null,"SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId='20261118120000_BrandWorkspace'")=="")throw new InvalidDataException("Kein unterstützter Datenbankstand.");
                if(!Verify(c))throw new InvalidDataException("Journal inkonsistent.");
                var hasSupplierSchema = Scalar(c, null,
                    "SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId=$p0", SupplierMigrationId) != "";
                var hasDocumentSchema = Scalar(c, null,
                    "SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId=$p0", DocumentMigrationId) != "";
                var hasRiskAssessmentSchema = Scalar(c, null,
                    "SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId=$p0", RiskAssessmentMigrationId) != "";
                backupPrefix = (hasSupplierSchema, hasDocumentSchema, hasRiskAssessmentSchema) switch
                {
                    (false, _, _) => "pre08",
                    (_, false, _) => "pre09",
                    (_, _, false) => "pre010",
                    _ => null
                };
            }
            if (backupPrefix is not null)
                await Files.AtomicAsync(sourcePath + $".{backupPrefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.bak", bytes);
            await Database.MigrateAsync(DbPath);
            if (!recovery && File.Exists(path + ".recovery"))
                await PreserveRecoveryAsync(path);
            ProjectPath = path;
            using (var c = Open(DbPath))
            {
                Name = Scalar(c, null, "SELECT Value FROM Metadata WHERE Key='ProjectName'");
                using var transaction = c.BeginTransaction();
                var version = typeof(Workspace).Assembly.GetName().Version?.ToString(3) ?? "";
                Log(c, transaction, recovery ? "RecoveryOpened" : "ProjectOpened", "", version);
                transaction.Commit();
            }
            IsDirty = true;
            await Snapshot(path + ".recovery");
        }catch{if(!wasOpen)Cleanup();throw;}finally{_gate.Release();}
    }

    public bool IsCurrentProject(string path) => ProjectPath is { } current &&
        string.Equals(Path.GetFullPath(path), current,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    /// <summary>Opens a project before replacing the current working context.</summary>
    /// <returns>Whether the retired working copy needs to be cleaned up separately.</returns>
    public Task<bool> SwitchAsync(string path, string password, bool recovery = false) =>
        TransitionAsync(path, candidate => candidate.OpenAsync(path, password, recovery), create: false);

    /// <summary>Creates the encrypted project and makes it active when creation succeeds.</summary>
    /// <returns>Whether the retired working copy needs to be cleaned up separately.</returns>
    public Task<bool> CreateAndSwitchAsync(string path, string name, string password, CompanyProfile companyProfile) =>
        TransitionAsync(path, candidate => candidate.CreateAsync(path, name, password, companyProfile: companyProfile), create: true);

    private async Task<bool> TransitionAsync(string path, Func<Workspace, Task> initialize, bool create)
    {
        await _gate.WaitAsync();
        try
        {
            identity.Demand();
            if (IsCurrentProject(path))
            {
                if (create)
                    throw new IOException("Bitte einen neuen Dateinamen wählen.");
                return false;
            }

            // Commit the former project's adopted changes before doing anything to the destination.
            if (IsOpen && IsDirty)
            {
                try { await WriteProjectAsync(); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    throw new IOException("Das bisherige Projekt konnte nicht gespeichert werden. Prüfen Sie den Speicherort und versuchen Sie den Mandantenwechsel erneut.", ex);
                }
            }

            using var candidate = new Workspace(identity);
            await initialize(candidate);

            (_cipher, candidate._cipher) = (candidate._cipher, _cipher);
            (_lock, candidate._lock) = (candidate._lock, _lock);
            (_lease, candidate._lease) = (candidate._lease, _lease);
            (_dir, candidate._dir) = (candidate._dir, _dir);
            ProjectPath = candidate.ProjectPath;
            PreservedRecoveryPath = candidate.PreservedRecoveryPath;
            Name = candidate.Name;
            IsDirty = candidate.IsDirty;

            try
            {
                candidate.Cleanup();
                return false;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Cleanup drops the retired lock and key in its finally block.
                return true;
            }
        }
        finally { _gate.Release(); }
    }

    private async Task Snapshot(string path)
    {
        var temp=Path.Combine(_dir!,"snapshot.db");if(File.Exists(temp))File.Delete(temp);
        try{using(var f=Files.Create(temp)){}using(var source=Open(DbPath))using(var target=Open(temp))source.BackupDatabase(target);
            var plain=await Files.ReadAsync(temp,Cipher.Limit);byte[] encrypted;try{encrypted=_cipher!.Encrypt(plain);}finally{CryptographicOperations.ZeroMemory(plain);}await Files.AtomicAsync(path,encrypted);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    private async Task PreserveRecoveryAsync(string projectPath)
    {
        var fileName = $"D3GRC-Recovery-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.d3grc";
        var preserved = Path.Combine(Path.GetDirectoryName(projectPath)!, fileName);
        var encrypted = await Files.ReadAsync(projectPath + ".recovery", Cipher.Limit + 52);
        await Files.AtomicAsync(preserved, encrypted);
        PreservedRecoveryPath = preserved;
    }
    private async Task WriteProjectAsync()
    {
        await Snapshot(ProjectPath!);
        IsDirty = false;
        var recoveryPath = ProjectPath! + ".recovery";
        if (File.Exists(recoveryPath))
            File.Delete(recoveryPath);
    }
    public async Task SaveAsync()
    {
        await _gate.WaitAsync();
        try { Demand(); await WriteProjectAsync(); }
        finally { _gate.Release(); }
    }
    public async Task SaveAsAsync(string path)
    {
        await _gate.WaitAsync();FileStream? next=null;try{Demand();path=Path.GetFullPath(path);if(File.Exists(path)||File.Exists(path+".recovery")||new FileInfo(path+".lock").LinkTarget is not null)throw new IOException("Neuen Dateinamen wählen.");next=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);await Snapshot(path);_lock?.Dispose();_lock=next;next=null;ProjectPath=path;IsDirty=false;}finally{next?.Dispose();_gate.Release();}
    }
    public async Task AutosaveAsync(){await _gate.WaitAsync();try{identity.Demand();if(IsOpen&&IsDirty)await Snapshot(ProjectPath!+".recovery");}finally{_gate.Release();}}
    public async Task CloseAsync(){await _gate.WaitAsync();try{Cleanup();}finally{_gate.Release();}}
    public async Task LogoutAsync(){await _gate.WaitAsync();try{if(IsOpen&&IsDirty)await WriteProjectAsync();Cleanup();identity.Logout();}finally{_gate.Release();}}
    private void Cleanup(){_lease?.Dispose();_lease=null;try{if(_dir is not null&&Directory.Exists(_dir))Directory.Delete(_dir,true);}finally{_dir=null;_cipher?.Dispose();_cipher=null;_lock?.Dispose();_lock=null;ProjectPath=null;PreservedRecoveryPath=null;Name="";IsDirty=false;}}
    public void Dispose(){Cleanup();_gate.Dispose();}
    public static int CleanupOrphans(){if(!Directory.Exists(WorkRoot))return 0;int count=0;foreach(var dir in Directory.GetDirectories(WorkRoot))try{using(var f=new FileStream(Path.Combine(dir,".active"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){}Directory.Delete(dir,true);count++;}catch(IOException){}catch(UnauthorizedAccessException){}return count;}
    private static string Hash(string utc,string actor,string action,string id,string detail,string previous)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[]{utc,actor,action,id,detail,previous}))));
    private void Log(SqliteConnection c,SqliteTransaction t,string action,string id,string detail)
    {var previous=Scalar(c,t,"SELECT Hash FROM Journal ORDER BY Id DESC LIMIT 1");var utc=DateTimeOffset.UtcNow.ToString("O");var actor=identity.User!;Exec(c,t,"INSERT INTO Journal(Utc,Actor,Action,EntityId,Detail,PreviousHash,Hash) VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6)",utc,actor,action,id,detail,previous,Hash(utc,actor,action,id,detail,previous));}
    private static bool Verify(SqliteConnection c)
    {var previous="";foreach(var e in Query(c,null,"SELECT Utc,Actor,Action,EntityId,Detail,PreviousHash,Hash FROM Journal ORDER BY Id",r=>Enumerable.Range(0,7).Select(r.GetString).ToArray())){if(e[5]!=previous||e[6]!=Hash(e[0],e[1],e[2],e[3],e[4],e[5]))return false;previous=e[6];}return true;}
    public Task<bool> VerifyAsync()=>Read(Verify);
    public Task<List<string>> JournalAsync()=>Read(c=>Query(c,null,"SELECT Utc,Actor,Action,EntityId,Detail FROM Journal ORDER BY Id DESC LIMIT 1000",r=>$"{r.GetString(0)} | {r.GetString(1)} | {r.GetString(2)} | {r.GetString(3)} | {r.GetString(4)}"));
    private static Entry ReadEntry(SqliteDataReader r)=>new(){Id=r.GetString(0),Kind=r.GetString(1),Code=r.GetString(2),Title=r.GetString(3),SourceId=r.GetString(4),TemplateId=r.IsDBNull(5)?null:r.GetString(5),Locator=r.GetString(6),Body=r.GetString(7),Owner=r.GetString(8),Status=r.GetString(9),DueDate=r.GetString(10),Scope=r.GetString(11),Version=r.GetInt32(12),IsSeed=r.GetInt32(13)!=0};
    private static Entry Get(SqliteConnection c,SqliteTransaction? t,string id)=>Query(c,t,"SELECT * FROM Records WHERE Id=$p0",ReadEntry,id).SingleOrDefault()??throw new ArgumentException("Datensatz fehlt.");
    private static void RequireKind(Entry e,string kind){if(e.Kind!=kind)throw new ArgumentException("Erwarteter Typ: "+kind);}
    private static void Reason(string reason){if(string.IsNullOrWhiteSpace(reason)||reason.Length>30000)throw new ArgumentException("Begründung erforderlich (maximal 30000 Zeichen).");}
    public Task<List<Choice>> TopicsAsync()=>Read(c=>Query(c,null,"SELECT Id,Title FROM Topics ORDER BY Title",r=>new Choice(r.GetString(0),r.GetString(1))));
    public Task<List<Source>> SourcesAsync(string topic="")=>Read(c=>Query(c,null,"SELECT Id,Title,Url,Reference,Summary,CustomSummary,CheckState,LastSuccess FROM Sources s WHERE $p0='' OR EXISTS(SELECT 1 FROM TopicSources t WHERE t.SourceId=s.Id AND t.TopicId=$p0) ORDER BY Title",r=>new Source(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),r.GetString(7)),topic));
    public Task<List<Entry>> EntriesAsync(string topic="",string kind="",string scope="")=>Read(c=>Query(c,null,"SELECT * FROM Records r WHERE ($p0='' OR EXISTS(SELECT 1 FROM RecordTopics t WHERE t.RecordId=r.Id AND t.TopicId=$p0)) AND ($p1='' OR Kind=$p1) AND ($p2='' OR Scope=$p2) ORDER BY Code,Title",ReadEntry,topic,kind,scope));
    public Task<List<Choice>> RecordTopicsAsync(string id)=>Read(c=>Query(c,null,"SELECT t.Id,t.Title FROM Topics t JOIN RecordTopics r ON r.TopicId=t.Id WHERE r.RecordId=$p0",r=>new Choice(r.GetString(0),r.GetString(1)),id));
    public Task<List<TextTemplate>> TemplatesAsync(string topic="",string kind="")=>Read(c=>Query(c,null,"SELECT Id,Kind,Title,SourceId,Locator,Body,CustomBody FROM Templates x WHERE ($p0='' OR EXISTS(SELECT 1 FROM TopicSources s WHERE s.SourceId=x.SourceId AND s.TopicId=$p0)) AND ($p1='' OR Kind=$p1) ORDER BY Title",r=>new TextTemplate(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6)),topic,kind));
    public Task<List<Choice>> MappingSuggestionsAsync(string topic)=>Read(c=>Query(c,null,"SELECT m.RequirementId||'|'||m.ControlId,a.Title||' → '||b.Title||char(10)||m.Reason FROM MappingSuggestions m JOIN Templates a ON a.Id=m.RequirementId JOIN Templates b ON b.Id=m.ControlId WHERE $p0='' OR EXISTS(SELECT 1 FROM TopicSources s WHERE s.SourceId=a.SourceId AND s.TopicId=$p0)",r=>new Choice(r.GetString(0),r.GetString(1)),topic));
    private static bool SourceFits(SqliteConnection c,SqliteTransaction t,string source,string topic)=>Scalar(c,t,"SELECT SourceId FROM TopicSources WHERE SourceId=$p0 AND TopicId=$p1",source,topic)!="";
    public async Task SaveEntryAsync(Entry e,string topic,string reason,SupplierDetails? supplierDetails=null)
    {
        e.Validate();Reason(reason);if(topic=="")throw new ArgumentException("Zum Speichern ein konkretes Thema wählen.");
        if (e.Kind == "Supplier")
            supplierDetails = SupplierRegister.ValidateDetails(supplierDetails);
        else if (supplierDetails is not null)
            throw new ArgumentException("Dienstleisterdaten gehören zu einem Dienstleister.", nameof(supplierDetails));

        var detail = supplierDetails is null
            ? JsonSerializer.Serialize(new { reason, e })
            : JsonSerializer.Serialize(new { reason, e, supplierDetails });
        await Change("EntrySaved",e.Id,detail, (c,t)=>{
            var old=Query(c,t,"SELECT * FROM Records WHERE Id=$p0",ReadEntry,e.Id).SingleOrDefault();if(old is not null&&(old.Kind!=e.Kind||old.Version!=e.Version))throw new ArgumentException("Veralteter Stand. Neu laden.");
            if(old?.Status is "Abgeschlossen" or "Geschlossen")throw new ArgumentException("Abgeschlossener Datensatz gesperrt.");
            if(e.Status is "Abgeschlossen" or "Geschlossen")throw new ArgumentException("Abschluss nur über geprüften Workflow.");
            if (supplierDetails is not null)
                ValidateSupplierEdit(c, t, e, old);
            if(!SourceFits(c,t,e.SourceId,topic)&&!(old is not null&&old.SourceId==e.SourceId&&Scalar(c,t,"SELECT TopicId FROM RecordTopics WHERE RecordId=$p0 AND TopicId=$p1",e.Id,topic)!=""))throw new ArgumentException("Quelle passt nicht zum Thema. Zusätzlichen Quellenbezug separat begründen.");
            if(e.TemplateId is not null&&Scalar(c,t,"SELECT Kind FROM Templates WHERE Id=$p0",e.TemplateId)!=e.Kind)throw new ArgumentException("Vorlage passt nicht zum Typ.");
            if(e.Kind=="Control"&&e.Status=="Geprüft"){var proof=Proofs(c,t,e.Id).FirstOrDefault(p=>DateTimeOffset.Parse(p.ValidUntil)>DateTimeOffset.UtcNow)??throw new ArgumentException("Gültiger Nachweis erforderlich.");RequireProof(c,t,proof.Id,e.Id,DateTimeOffset.UtcNow);}
            if(old is not null)Log(c,t,"EntryBefore",e.Id,JsonSerializer.Serialize(old));
            Exec(c,t,"INSERT INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,Owner,Status,DueDate,Scope,Version,IsSeed) VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9,$p10,$p11,1,0) ON CONFLICT(Id) DO UPDATE SET Code=excluded.Code,Title=excluded.Title,SourceId=excluded.SourceId,TemplateId=excluded.TemplateId,Locator=excluded.Locator,Body=excluded.Body,Owner=excluded.Owner,Status=excluded.Status,DueDate=excluded.DueDate,Scope=excluded.Scope,Version=Records.Version+1,IsSeed=0",e.Id,e.Kind,e.Code,e.Title,e.SourceId,e.TemplateId,e.Locator,e.Body,e.Owner,e.Status,e.DueDate,e.Scope);
            if (supplierDetails is not null)
                SaveSupplierDetails(c, t, e.Id, supplierDetails);
            Exec(c,t,"INSERT OR IGNORE INTO RecordTopics VALUES($p0,$p1)",e.Id,topic);if(topic!="REVIEW")Exec(c,t,"DELETE FROM RecordTopics WHERE RecordId=$p0 AND TopicId='REVIEW'",e.Id);
            Exec(c,t,"UPDATE RecordSources SET Role='Sekundär' WHERE RecordId=$p0",e.Id);Exec(c,t,"INSERT INTO RecordSources VALUES($p0,$p1,$p2,'Primär') ON CONFLICT(RecordId,SourceId) DO UPDATE SET Locator=excluded.Locator,Role='Primär'",e.Id,e.SourceId,e.Locator);
            if(e.Kind=="Requirement")Exec(c,t,"INSERT INTO Applicability VALUES($p0,$p1,$p2,$p3,$p4) ON CONFLICT(RecordId) DO UPDATE SET Decision=excluded.Decision,Reason=excluded.Reason,Reviewer=excluded.Reviewer,ReviewedUtc=excluded.ReviewedUtc WHERE Applicability.Decision<>excluded.Decision",e.Id,e.Status,reason,identity.User,DateTimeOffset.UtcNow.ToString("O"));return true;
        });
    }
    public async Task AddTopicAsync(string id,string topic,string reason)=>await Change("TopicAdded",id,reason,(c,t)=>{Reason(reason);if(Get(c,t,id).Status is "Abgeschlossen" or "Geschlossen")throw new ArgumentException("Abgeschlossener Datensatz gesperrt.");Exec(c,t,"INSERT OR IGNORE INTO RecordTopics VALUES($p0,$p1)",id,topic);return true;});
    public async Task AddSourceAsync(string id,string source,string locator,string reason)=>await Change("SecondarySourceAdded",id,reason,(c,t)=>{Reason(reason);if(Get(c,t,id).Status is "Abgeschlossen" or "Geschlossen")throw new ArgumentException("Abgeschlossener Datensatz gesperrt.");Exec(c,t,"INSERT INTO RecordSources VALUES($p0,$p1,$p2,'Sekundär') ON CONFLICT(RecordId,SourceId) DO UPDATE SET Locator=excluded.Locator",id,source,locator);return true;});
    public Task<List<Source>> RecordSourceChoicesAsync(string id)=>Read(c=>Query(c,null,"SELECT s.Id,s.Title,s.Url,s.Reference,s.Summary,s.CustomSummary,s.CheckState,s.LastSuccess FROM Sources s JOIN RecordSources r ON r.SourceId=s.Id WHERE r.RecordId=$p0 ORDER BY r.Role,s.Title",r=>new Source(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),r.GetString(7)),id));
    public Task<List<string>> RecordSourcesAsync(string id)=>Read(c=>Query(c,null,"SELECT s.Title,r.Locator,r.Role FROM RecordSources r JOIN Sources s ON s.Id=r.SourceId WHERE r.RecordId=$p0",r=>$"{r.GetString(0)} | {r.GetString(1)} | {r.GetString(2)}",id));
    public async Task DeleteAsync(string id,string reason)=>await Change("EntryDeleted",id,reason,(c,t)=>{
        Reason(reason);
        var e=Get(c,t,id);
        if(e.Kind is "Audit" or "Finding")throw new ArgumentException("Audits/Findings bleiben erhalten.");
        if (e.Kind == "Supplier" && LatestSupplierReviewDate(c, t, id) != "")
            throw new ArgumentException("Dienstleister mit Prüfungen bleiben im Register. Setzen Sie den Status bei Bedarf auf „Beendet“.");
        if (e.Kind == "Risk" && Scalar(c, t, "SELECT Id FROM RiskAssessments WHERE RiskId=$p0 LIMIT 1", id) != "")
            throw new ArgumentException("Risiken mit Bewertungen bleiben im Register, damit ihre Historie erhalten bleibt.");
        if ((e.Kind is "Requirement" or "Control") && Scalar(c, t, "SELECT Id FROM DocumentLinks WHERE RecordId=$p0 LIMIT 1", id) != "")
            throw new ArgumentException("Der Datensatz bleibt erhalten, solange ein Dokument darauf verweist.");
        Log(c,t,"EntryBefore",id,JsonSerializer.Serialize(e));
        Exec(c,t,"DELETE FROM Records WHERE Id=$p0",id);
        return true;
    });
    public async Task SaveTemplateAsync(string id, string body)
    {
        var plain = RichTextContent.Plain(body);
        await Change("TemplateCustomized", id, plain, (c, t) =>
        {
            Reason(plain);
            Exec(c, t, "UPDATE Templates SET CustomBody=$p0 WHERE Id=$p1", body, id);
            return true;
        });
    }
    public Task<List<Choice>> GlossaryAsync()=>Read(c=>Query(c,null,"SELECT Id,Term||' – '||Expanded||char(10)||CASE WHEN CustomDefinition='' THEN Definition ELSE CustomDefinition END FROM Glossary ORDER BY Term",r=>new Choice(r.GetString(0),r.GetString(1))));
    public async Task SaveGlossaryAsync(string id,string text)=>await Change("GlossaryCustomized",id,text,(c,t)=>{Reason(text);Exec(c,t,"UPDATE Glossary SET CustomDefinition=$p0 WHERE Id=$p1",text,id);return true;});
}
