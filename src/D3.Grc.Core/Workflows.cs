using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;
namespace D3.Grc.Core;
public sealed partial class Workspace
{
    public static readonly Dictionary<string, (string From, string To)> LinkTypes = new()
    {
        ["Anforderung → Control"] = ("Requirement", "Control"),
        ["Risiko → Control"] = ("Risk", "Control"),
        ["Audit → Control"] = ("Audit", "Control"),
        ["Finding → Audit"] = ("Finding", "Audit"),
        ["Finding → Control"] = ("Finding", "Control"),
        ["Maßnahme → Finding"] = ("Measure", "Finding"),
        ["Maßnahme → Risiko"] = ("Measure", "Risk"),
        ["KI-System → Control"] = ("AiSystem", "Control"),
        ["OSS-Komponente → Control"] = ("OssComponent", "Control"),
        ["Dienstleister → Control"] = ("Supplier", "Control"),
        ["Dienstleister → Risiko"] = ("Supplier", "Risk"),
        ["Maßnahme → Dienstleister"] = ("Measure", "Supplier")
    };
    public Task<List<Mapping>> MappingsAsync()=>Read(c=>Query(c,null,"SELECT l.*,COALESCE(a.Coverage,'Teilweise'),COALESCE(a.Scope,''),COALESCE(a.Approval,'Entwurf') FROM Links l LEFT JOIN MappingAssessments a ON a.LinkId=l.Id",r=>new Mapping(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),r.GetString(7),r.GetString(8))));
    public async Task LinkAsync(string from,string to,string type,string source,string coverage,string scope,string reason)=>await Change("MappingCreated",from,JsonSerializer.Serialize(new{to,type,source,coverage,scope,reason}),(c,t)=>{
        Reason(reason);if(!LinkTypes.TryGetValue(type,out var pair)||!new[]{"Teilweise","Vollständig","Unterstützend"}.Contains(coverage))throw new ArgumentException("Beziehung/Abdeckung ungültig.");var a=Get(c,t,from);var b=Get(c,t,to);RequireKind(a,pair.From);RequireKind(b,pair.To);
        if(a.Status is "Abgeschlossen" or "Geschlossen"||b.Status is "Abgeschlossen" or "Geschlossen")throw new ArgumentException("Abgeschlossene Datensätze sind gesperrt.");
        var sourceAllowed=Scalar(c,t,"SELECT SourceId FROM RecordSources WHERE RecordId=$p0 AND SourceId=$p1",from,source)!=""||source==a.SourceId;if(!sourceAllowed)throw new ArgumentException("Quellenbezug zuerst am Ausgangsobjekt begründet ergänzen.");
        var id=Guid.NewGuid().ToString("N");Exec(c,t,"INSERT INTO Links VALUES($p0,$p1,$p2,$p3,$p4,$p5)",id,from,to,type,reason,source);Exec(c,t,"INSERT INTO MappingAssessments VALUES($p0,$p1,$p2,'Bestätigt')",id,coverage,scope);return true;
    });
    public async Task UnlinkAsync(string id)=>await Change("MappingRemoved",id,"Benutzerbestätigung",(c,t)=>{
        var link=Query(c,t,"SELECT SourceRecordId,TargetRecordId,Type FROM Links WHERE Id=$p0",r=>(From:r.GetString(0),To:r.GetString(1),Type:r.GetString(2)),id).Single();
        if(Get(c,t,link.From).Status is "Abgeschlossen" or "Geschlossen"||Get(c,t,link.To).Status is "Abgeschlossen" or "Geschlossen")throw new ArgumentException("Abgeschlossene Zuordnung gesperrt.");
        if(link.Type=="Audit → Control"&&Scalar(c,t,"SELECT Id FROM AuditTests WHERE AuditId=$p0 AND ControlId=$p1",link.From,link.To)!="")throw new ArgumentException("Bereits geprüften Scope nicht entfernen.");Exec(c,t,"DELETE FROM Links WHERE Id=$p0",id);return true;
    });
    private static List<Proof> Proofs(SqliteConnection c,SqliteTransaction? t,string control="")=>Query(c,t,"SELECT Id,Title,ControlId,SourceId,FileName,Hash,CollectedUtc,ValidUntil,SourceDescription FROM Evidence e WHERE $p0='' OR ControlId=$p0 OR EXISTS(SELECT 1 FROM EvidenceLinks l WHERE l.EvidenceId=e.Id AND l.ControlId=$p0)",r=>new Proof(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),r.GetString(7),r.GetString(8)),control);
    public Task<List<Proof>> EvidenceAsync(string control="")=>Read(c=>Proofs(c,null,control));
    private static void RequireProof(SqliteConnection c,SqliteTransaction t,string id,string control,DateTimeOffset at)
    {
        var p=Proofs(c,t,control).SingleOrDefault(x=>x.Id==id)??throw new ArgumentException("Nachweis passt nicht zum Control.");
        if(!DateTimeOffset.TryParse(p.ValidUntil,out var until)||!DateTimeOffset.TryParse(p.Collected,out var collected)||until<=at||collected>at)throw new ArgumentException("Nachweis war zum Prüfzeitpunkt nicht gültig.");
        using var cmd=Command(c,t,"SELECT Content FROM Evidence WHERE Id=$p0",id);var bytes=(byte[])cmd.ExecuteScalar()!;if(Convert.ToHexString(SHA256.HashData(bytes))!=p.Hash)throw new InvalidDataException("Evidence-Hash stimmt nicht.");
    }
    public async Task ImportEvidenceAsync(string control,string source,string title,string origin,string fileName,byte[] content,DateTimeOffset until)=>await Change("EvidenceImported",control,title,(c,t)=>{
        RequireKind(Get(c,t,control),"Control");Reason(title);Reason(origin);if(content.Length is <1 or >8388608||until<=DateTimeOffset.UtcNow)throw new ArgumentException("Maximal 8 MiB, Gültigkeit muss in der Zukunft liegen.");
        if(Scalar(c,t,"SELECT SourceId FROM RecordSources WHERE RecordId=$p0 AND SourceId=$p1",control,source)=="")throw new ArgumentException("Quelle zuerst am Control zuordnen.");
        if(long.Parse(Scalar(c,t,"SELECT COALESCE(SUM(length(Content)),0) FROM Evidence"))+content.Length>96*1024*1024)throw new IOException("Evidence-Gesamtlimit 96 MiB.");
        Exec(c,t,"INSERT INTO Evidence VALUES($p0,$p1,$p2,$p3,NULL,$p4,$p5,$p6,$p7,$p8,$p9)",Guid.NewGuid().ToString("N"),title,control,source,origin,Path.GetFileName(fileName),Convert.ToHexString(SHA256.HashData(content)),content,DateTimeOffset.UtcNow.ToString("O"),until.ToString("O"));return true;
    });
    public async Task ReuseEvidenceAsync(string evidence,string control)=>await Change("EvidenceReused",evidence,control,(c,t)=>{RequireKind(Get(c,t,control),"Control");var p=Proofs(c,t).Single(x=>x.Id==evidence);RequireProof(c,t,evidence,p.ControlId,DateTimeOffset.UtcNow);if(p.ControlId==control)throw new ArgumentException("Bereits zugeordnet.");Exec(c,t,"INSERT INTO EvidenceLinks VALUES($p0,$p1)",evidence,control);return true;});
    public Task<byte[]> EvidenceBytesAsync(string id)=>Read(c=>{var p=Proofs(c,null).Single(x=>x.Id==id);using var cmd=Command(c,null,"SELECT Content FROM Evidence WHERE Id=$p0",id);var bytes=(byte[])cmd.ExecuteScalar()!;if(Convert.ToHexString(SHA256.HashData(bytes))!=p.Hash)throw new InvalidDataException("Evidence beschädigt.");return bytes;});
    public Task<List<AuditTest>> TestsAsync(string audit)=>Read(c=>Query(c,null,"SELECT * FROM AuditTests WHERE AuditId=$p0",r=>new AuditTest(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.IsDBNull(4)?null:r.GetString(4),r.GetString(5),r.GetString(6),r.GetString(7)),audit));
    public async Task TestAsync(string audit,string control,string source,string? evidence,string outcome,string notes)=>await Change("AuditTestSaved",audit,JsonSerializer.Serialize(new{control,source,evidence,outcome,notes}),(c,t)=>{
        Reason(notes);var a=Get(c,t,audit);RequireKind(a,"Audit");var b=Get(c,t,control);RequireKind(b,"Control");if(a.Status!="In Prüfung")throw new ArgumentException("Audit muss In Prüfung sein.");
        if(Scalar(c,t,"SELECT Id FROM Links WHERE SourceRecordId=$p0 AND TargetRecordId=$p1 AND Type='Audit → Control'",audit,control)=="")throw new ArgumentException("Control liegt nicht im Audit-Scope.");
        if(!new[]{"Bestanden","Abweichung","Nicht anwendbar"}.Contains(outcome))throw new ArgumentException("Prüfergebnis auswählen.");
        if(Scalar(c,t,"SELECT SourceId FROM RecordSources WHERE RecordId=$p0 AND SourceId=$p1",control,source)=="")throw new ArgumentException("Prüfquelle zuerst am Control zuordnen.");
        var now=DateTimeOffset.UtcNow;if(evidence is not null)RequireProof(c,t,evidence,control,now);else if(outcome=="Bestanden")throw new ArgumentException("Bestanden benötigt Evidence.");
        var old=Scalar(c,t,"SELECT Id FROM AuditTests WHERE AuditId=$p0 AND ControlId=$p1",audit,control);
        Exec(c,t,"INSERT INTO AuditTests VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7) ON CONFLICT(AuditId,ControlId) DO UPDATE SET SourceId=excluded.SourceId,EvidenceId=excluded.EvidenceId,Outcome=excluded.Outcome,Notes=excluded.Notes,TestedUtc=excluded.TestedUtc",old==""?Guid.NewGuid().ToString("N"):old,audit,control,source,evidence,outcome,notes,now.ToString("O"));
        var refs=Query(c,t,"SELECT SourceId,Locator,Role FROM RecordSources WHERE RecordId=$p0",r=>new[]{r.GetString(0),r.GetString(1),r.GetString(2)},control);
        Exec(c,t,"INSERT INTO AuditCriteria VALUES($p0,$p1,$p2,$p3) ON CONFLICT(AuditId,ControlId) DO UPDATE SET ControlVersion=excluded.ControlVersion,CriterionJson=excluded.CriterionJson",audit,control,b.Version,JsonSerializer.Serialize(new{control=b,sources=refs}));return true;
    });
    public async Task CompleteAuditAsync(string audit,string reason)=>await Change("AuditCompleted",audit,reason,(c,t)=>{
        Reason(reason);var a=Get(c,t,audit);RequireKind(a,"Audit");if(a.Status!="In Prüfung")throw new ArgumentException("Audit muss In Prüfung sein.");
        var scope=Query(c,t,"SELECT TargetRecordId FROM Links WHERE SourceRecordId=$p0 AND Type='Audit → Control'",r=>r.GetString(0),audit);if(scope.Count==0)throw new ArgumentException("Scope leer.");
        foreach(var control in scope){var test=Query(c,t,"SELECT Outcome,EvidenceId,TestedUtc FROM AuditTests WHERE AuditId=$p0 AND ControlId=$p1",r=>(Outcome:r.GetString(0),Evidence:r.IsDBNull(1)?null:r.GetString(1),At:r.GetString(2)),audit,control).SingleOrDefault();if(test.Outcome is null)throw new ArgumentException("Scope nicht vollständig geprüft.");
            if(test.Outcome=="Bestanden")RequireProof(c,t,test.Evidence!,control,DateTimeOffset.Parse(test.At));
            if(test.Outcome=="Abweichung"&&Scalar(c,t,"SELECT a.Id FROM Links a JOIN Links b ON a.SourceRecordId=b.SourceRecordId WHERE a.Type='Finding → Audit' AND a.TargetRecordId=$p0 AND b.Type='Finding → Control' AND b.TargetRecordId=$p1",audit,control)=="")throw new ArgumentException("Abweichung benötigt Finding mit Audit- und Control-Bezug.");}
        Exec(c,t,"UPDATE Records SET Status='Abgeschlossen',Version=Version+1 WHERE Id=$p0",audit);return true;
    });
    public async Task CloseFindingAsync(string finding,string evidence,string review)=>await Change("FindingClosed",finding,JsonSerializer.Serialize(new{ evidence, review }),(c,t)=>{
        Reason(review);var f=Get(c,t,finding);RequireKind(f,"Finding");if(f.Status!="Wirksamkeitsprüfung")throw new ArgumentException("Finding muss Wirksamkeitsprüfung sein.");
        var controls=Query(c,t,"SELECT TargetRecordId FROM Links WHERE SourceRecordId=$p0 AND Type='Finding → Control'",r=>r.GetString(0),finding);if(controls.Count==0)throw new ArgumentException("Finding hat keinen Control-Bezug.");foreach(var control in controls)RequireProof(c,t,evidence,control,DateTimeOffset.UtcNow);Exec(c,t,"UPDATE Records SET Status='Geschlossen',Version=Version+1 WHERE Id=$p0",finding);return true;
    });
}
