namespace D3.Grc.Core;
public sealed record Choice(string Id,string Title){public override string ToString()=>Title;}
public sealed record Source(string Id,string Title,string Url,string Reference,string Summary,string Note,string State,string LastSuccess)
{
    internal const string CustomIdPrefix = "CUSTOM-";

    public bool IsCustom => IsCustomId(Id);
    internal static bool IsCustomId(string id) => id.StartsWith(CustomIdPrefix, StringComparison.Ordinal);
    public override string ToString() => Title;
}
public sealed record TextTemplate(string Id,string Kind,string Title,string SourceId,string Locator,string Body,string CustomBody){public string Effective=>CustomBody==""?Body:CustomBody;public override string ToString()=>Title;}
public sealed class Entry
{
    public string Id{get;set;}=Guid.NewGuid().ToString("N");public string Kind{get;set;}="Control";public string Code{get;set;}="";public string Title{get;set;}="";public string SourceId{get;set;}="INTERNAL";public string? TemplateId{get;set;}
    public string Locator{get;set;}="";public string Body{get;set;}="";public string Owner{get;set;}="";public string Status{get;set;}="Entwurf";public string DueDate{get;set;}="";public string Scope{get;set;}="";public int Version{get;set;}=1;public bool IsSeed{get;set;}
    public override string ToString()=>$"{Code} · {Title} | {Status}";
    public static readonly string[] Kinds=["Requirement","Control","Risk","Audit","Finding","Measure","AiSystem","OssComponent","Supplier"];
    public static string[] Statuses(string kind)=>kind switch{"Audit"=>["Entwurf","Geplant","In Prüfung","Abgeschlossen"],"Finding"=>["Entwurf","Offen","In Bearbeitung","Wirksamkeitsprüfung","Geschlossen"],"Control"=>["Entwurf","Geplant","Umgesetzt","Geprüft"],"Requirement"=>["Entwurf","Anwendbar","Nicht anwendbar"],"Supplier"=>["Entwurf","In Prüfung","Freigegeben","Maßnahmen offen","Abgelehnt","Beendet"],_=>["Entwurf","Offen","In Bearbeitung","Erledigt"]};
    public void Validate(){if(!Kinds.Contains(Kind)||string.IsNullOrWhiteSpace(Title)||Title.Length>500||Body.Length>30000||Code.Length>100||Owner.Length>240||Scope.Length>1000||Locator.Length>1000||!Statuses(Kind).Contains(Status))throw new ArgumentException("Datensatz ungültig oder Feldlänge überschritten.");if(DueDate!=""&&!DateOnly.TryParseExact(DueDate,"yyyy-MM-dd",out _))throw new ArgumentException("Termin: JJJJ-MM-TT.");}
}
public sealed record Mapping(string Id,string From,string To,string Type,string Reason,string SourceId,string Coverage,string Scope,string Approval){public override string ToString()=>$"{Type} | {Coverage} | {Approval} | {Reason}";}
public sealed record Proof(string Id,string Title,string ControlId,string SourceId,string FileName,string Hash,string Collected,string ValidUntil,string Origin){public override string ToString()=>$"{Title} | {FileName} | {ValidUntil[..Math.Min(10,ValidUntil.Length)]}";}
public sealed record AuditTest(string Id,string AuditId,string ControlId,string SourceId,string? EvidenceId,string Outcome,string Notes,string TestedUtc){public override string ToString()=>$"{Outcome} | {Notes}";}
public sealed record Metric(string Label,int Value,string Kind,string? Status=null);
public sealed record ExportSheet(string Name,string[] Headers,List<string[]> Rows);
