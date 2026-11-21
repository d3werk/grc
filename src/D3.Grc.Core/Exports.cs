using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;
namespace D3.Grc.Core;
public static class ExcelExport
{
    private static readonly XNamespace S="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace P="http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace C="http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly HashSet<string> NumberHeaders = new(StringComparer.Ordinal)
        { "Anzahl", "Aktuelle Version", "Zuletzt veröffentlichte Version" };
    private static readonly HashSet<string> DateHeaders = new(StringComparer.Ordinal)
        { "Termin", "Gültig bis", "Erhoben", "Prüfdatum", "Nächste Prüfung", "Geändert am (UTC)" };
    private static string Clean(string value)=>new(value.Where(ch=>ch=='\t'||ch=='\n'||ch=='\r'||ch>=32).Take(32767).ToArray());
    private static string Column(int n){var text="";do{text=(char)('A'+n%26)+text;n=n/26-1;}while(n>=0);return text;}
    public static byte[] Build(IReadOnlyList<ExportSheet> sheets)
    {
        if(sheets.Count is <1 or >100||sheets.Any(s=>s.Headers.Length is <1 or >100||s.Rows.Count>100000))throw new ArgumentException("Exportumfang überschritten.");
        using var output=new MemoryStream();using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){
            void Part(string name,XElement xml){var entry=zip.CreateEntry(name,CompressionLevel.Fastest);using var stream=entry.Open();new XDocument(new XDeclaration("1.0","utf-8","yes"),xml).Save(stream);}
            var types=new XElement(C+"Types",new XElement(C+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),new XElement(C+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),new XElement(C+"Override",new XAttribute("PartName","/xl/workbook.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),new XElement(C+"Override",new XAttribute("PartName","/xl/styles.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")));
            Part("_rels/.rels",new XElement(P+"Relationships",new XElement(P+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type",R.NamespaceName+"/officeDocument"),new XAttribute("Target","xl/workbook.xml"))));
            var bookSheets=new XElement(S+"sheets");var relations=new XElement(P+"Relationships");
            for(int i=0;i<sheets.Count;i++){
                var sheet=sheets[i];var index=i+1;var name=new string(sheet.Name.Where(ch=>!"[]:*?/\\".Contains(ch)).Take(25).ToArray());if(name=="")name="Blatt";name=index+" "+name;
                bookSheets.Add(new XElement(S+"sheet",new XAttribute("name",name),new XAttribute("sheetId",index),new XAttribute(R+"id","rId"+index)));
                relations.Add(new XElement(P+"Relationship",new XAttribute("Id","rId"+index),new XAttribute("Type",R.NamespaceName+"/worksheet"),new XAttribute("Target","worksheets/sheet"+index+".xml")));
                types.Add(new XElement(C+"Override",new XAttribute("PartName","/xl/worksheets/sheet"+index+".xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
                var data=new XElement(S+"sheetData");var rows=new[]{sheet.Headers}.Concat(sheet.Rows).ToList();
                for(int row=0;row<rows.Count;row++){
                    var line=new XElement(S+"row",new XAttribute("r",row+1));
                    for(int col=0;col<sheet.Headers.Length;col++){
                        var value=col<rows[row].Length?rows[row][col]:"";var cell=new XElement(S+"c",new XAttribute("r",Column(col)+(row+1)),new XAttribute("s",row==0?1:0));
                        if(row>0&&NumberHeaders.Contains(sheet.Headers[col])&&int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var number))cell.Add(new XElement(S+"v",number));
                        else if(row>0&&DateHeaders.Contains(sheet.Headers[col])&&DateTime.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var date)&&date.Year>=1900){cell.SetAttributeValue("s",2);cell.Add(new XElement(S+"v",date.ToOADate().ToString(CultureInfo.InvariantCulture)));}
                        else{cell.SetAttributeValue("t","inlineStr");cell.Add(new XElement(S+"is",new XElement(S+"t",new XAttribute(XNamespace.Xml+"space","preserve"),Clean(value))));}line.Add(cell);
                    }data.Add(line);
                }
                var end=Column(sheet.Headers.Length-1)+(sheet.Rows.Count+1);Part("xl/worksheets/sheet"+index+".xml",new XElement(S+"worksheet",new XElement(S+"dimension",new XAttribute("ref","A1:"+end)),new XElement(S+"sheetViews",new XElement(S+"sheetView",new XAttribute("workbookViewId",0),new XElement(S+"pane",new XAttribute("ySplit",1),new XAttribute("topLeftCell","A2"),new XAttribute("activePane","bottomLeft"),new XAttribute("state","frozen")))),new XElement(S+"cols",new XElement(S+"col",new XAttribute("min",1),new XAttribute("max",sheet.Headers.Length),new XAttribute("width",28),new XAttribute("customWidth",1))),data,new XElement(S+"autoFilter",new XAttribute("ref","A1:"+end))));
            }
            relations.Add(new XElement(P+"Relationship",new XAttribute("Id","styles"),new XAttribute("Type",R.NamespaceName+"/styles"),new XAttribute("Target","styles.xml")));
            Part("xl/workbook.xml",new XElement(S+"workbook",new XAttribute(XNamespace.Xmlns+"r",R),bookSheets));Part("xl/_rels/workbook.xml.rels",relations);Part("[Content_Types].xml",types);
            Part("xl/styles.xml",XElement.Parse("""
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><color rgb="FFFFFFFF"/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF486E31"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="3"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"><alignment vertical="top" wrapText="1"/></xf><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/><xf numFmtId="14" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>
"""));
        }return output.ToArray();
    }
    public static string Html(string title,IReadOnlyList<ExportSheet> sheets)
    {
        static string E(string s)=>WebUtility.HtmlEncode(s);var b=new StringBuilder("<!doctype html><html lang='de'><meta charset='utf-8'><title>GRC-Bericht</title><style>body{font:12px system-ui;color:#26332d;margin:24px}h1,h2{color:#486e31}table{border-collapse:collapse;width:100%;margin:20px 0}td,th{border:1px solid #ddd;padding:8px;text-align:left;vertical-align:top;white-space:pre-wrap;overflow-wrap:anywhere}thead{display:table-header-group}@page{size:A4 landscape;margin:12mm}@media print{button{display:none}tr{break-inside:avoid}}</style><button onclick='window.print()'>Drucken / PDF</button><h1>");b.Append(E(title)).Append("</h1><p>Klartextbericht · keine Konformitätsbescheinigung</p>");
        foreach(var s in sheets){b.Append("<h2>").Append(E(s.Name)).Append("</h2><table><thead><tr>");foreach(var h in s.Headers)b.Append("<th>").Append(E(h)).Append("</th>");b.Append("</tr></thead><tbody>");foreach(var row in s.Rows){b.Append("<tr>");foreach(var cell in row)b.Append("<td>").Append(E(cell)).Append("</td>");b.Append("</tr>");}b.Append("</tbody></table>");}return b.Append("</html>").ToString();
    }
}
public sealed partial class Workspace
{
    public Task<List<ExportSheet>> ExportSnapshotAsync(string topic,string scope,string search)=>Read(c=>{
        var entries=Query(c,null,"SELECT * FROM Records r WHERE ($p0='' OR EXISTS(SELECT 1 FROM RecordTopics t WHERE t.RecordId=r.Id AND t.TopicId=$p0)) AND ($p1='' OR Scope=$p1)",ReadEntry,topic,scope).Where(e=>search==""||new[]{e.Title,e.Code,e.Body,e.Owner}.Any(s=>s.Contains(search,StringComparison.CurrentCultureIgnoreCase))).ToList();var ids=entries.Select(e=>e.Id).ToHashSet();
        var sheets=new List<ExportSheet>();var meta=new List<string[]>{new[]{"Projekt",Name},new[]{"Thema",topic==""?"Alle Themen":topic},new[]{"Scope",scope==""?"Alle Scopes":scope},new[]{"Suche",search},new[]{"Stichtag UTC",DateTimeOffset.UtcNow.ToString("O")},new[]{"Profil",identity.User!},new[]{"Methode","Aktueller Bestand; keine historische Zeitreihe; keine Konformitätsquote"}};sheets.Add(new("Exportkontext",["Feld","Wert"],meta));
        if (ReadCompanyProfile(c) is { } company)
        {
            meta.Add(["Unternehmensgröße", CompanyPlanning.SizeLabels[company.Size]]);
            meta.Add(["Branche", CompanyPlanning.IndustryLabels[company.Industry]]);
            meta.Add(["Standorte", company.SiteCount.ToString(CultureInfo.InvariantCulture)]);
            meta.Add(["Organisationseinheiten", company.OrganizationalUnitCount.ToString(CultureInfo.InvariantCulture)]);
            meta.Add(["Risikofokus", CompanyPlanning.RiskFocusLabels[company.RiskFocus]]);
            meta.Add(["Gewählte Fachthemen", string.Join(", ", company.Topics.Select(id => CompanyPlanning.TopicLabels[id]))]);
            meta.Add(["Ausgewählte Merkmale", string.Join(", ", CompanyPlanning.FactorLabels
                .Where(factor => company.Factors.HasFlag(factor.Key)).Select(factor => factor.Value))]);

            var planRows = CompanyPlanning.Build(company).Select(module => new[]
            {
                module.Priority == CompanyModulePriority.Key ? "Schlüsselmodul" : "Optionale Ergänzung",
                module.Title,
                module.TopicId.Length == 0 ? "Themenübergreifend" : CompanyPlanning.TopicLabels[module.TopicId],
                module.Why,
                module.NextStep
            }).ToList();
            sheets.Add(new("Umsetzungsplan", ["Priorität", "Modul", "Fachthema", "Begründung", "Nächster Schritt"], planRows));
        }
        sheets.Add(new("Kennzahlen",["Kennzahl","Anzahl"],new(){new[]{"Anforderungen",entries.Count(e=>e.Kind=="Requirement").ToString()},new[]{"Controls",entries.Count(e=>e.Kind=="Control").ToString()},new[]{"Controls als Entwurf",entries.Count(e=>e.Kind=="Control"&&e.Status=="Entwurf").ToString()},new[]{"Offene Findings",entries.Count(e=>e.Kind=="Finding"&&e.Status!="Geschlossen").ToString()},new[]{"Ohne Verantwortung",entries.Count(e=>e.Owner=="").ToString()}}));
        foreach(var kind in Entry.Kinds.Where(kind => kind != "Supplier"))sheets.Add(new(kind,["ID","Code","Titel","Quelle","Fundstelle","Status","Verantwortung","Termin","Scope","Version","Beschreibung"],entries.Where(e=>e.Kind==kind).Select(e=>new[]{e.Id,e.Code,e.Title,e.SourceId,e.Locator,e.Status,e.Owner,e.DueDate,e.Scope,e.Version.ToString(),e.Body}).ToList()));
        var links=Query(c,null,"SELECT l.Id,l.SourceRecordId,l.TargetRecordId,l.Type,l.Reason,l.SourceId,COALESCE(a.Coverage,'Teilweise'),COALESCE(a.Scope,'') FROM Links l LEFT JOIN MappingAssessments a ON a.LinkId=l.Id",r=>Enumerable.Range(0,8).Select(r.GetString).ToArray()).Where(x=>ids.Contains(x[1])||ids.Contains(x[2])).ToList();sheets.Add(new("Mappings",["ID","Ausgangs-ID","Ziel-ID","Typ","Begründung","Quelle","Abdeckung","Scope"],links));
        var proofIds=Query(c,null,"SELECT EvidenceId,ControlId FROM EvidenceLinks",r=>(Evidence:r.GetString(0),Control:r.GetString(1))).Where(x=>ids.Contains(x.Control)).Select(x=>x.Evidence).ToHashSet();
        sheets.Add(new("Evidence",["ID","Titel","Primär-Control-ID","Quelle","Datei","SHA256","Erhoben","Gültig bis","Herkunft"],Proofs(c,null).Where(p=>ids.Contains(p.ControlId)||proofIds.Contains(p.Id)).Select(p=>new[]{p.Id,p.Title,p.ControlId,p.SourceId,p.FileName,p.Hash,p.Collected,p.ValidUntil,p.Origin}).ToList()));
        sheets.Add(new("AuditTests",["ID","Audit-ID","Control-ID","Quelle","Evidence-ID","Ergebnis","Notiz","Prüfzeitpunkt"],Query(c,null,"SELECT * FROM AuditTests",r=>Enumerable.Range(0,8).Select(i=>r.IsDBNull(i)?"":r.GetString(i)).ToArray()).Where(x=>ids.Contains(x[1])).ToList()));
        var scopedProofs=Proofs(c,null).Where(p=>ids.Contains(p.ControlId)||proofIds.Contains(p.Id)).ToList();
        var metrics=sheets.Single(s=>s.Name=="Kennzahlen").Rows;metrics.Add(new[]{"Nachweise",scopedProofs.Count.ToString()});
        var currentControls = entries.Where(entry => entry.Kind == "Control").Select(entry => entry.Id).ToList();
        var controlEvidence = ReadControlEvidence(c, "SELECT value AS Id FROM json_each($p0)",
            System.Text.Json.JsonSerializer.Serialize(currentControls), DateTimeOffset.UtcNow);
        metrics.Add(new[] { "Controls ohne zeitlich aktuellen Nachweis",
            currentControls.Count(id => !controlEvidence[id].Any(proof => proof.Status == TraceEvidenceStatus.Current)).ToString() });
        var today=DateOnly.FromDateTime(DateTime.Today);metrics.Add(new[]{"Überfällige Maßnahmen",entries.Count(e=>e.Kind=="Measure"&&e.Status!="Erledigt"&&DateOnly.TryParse(e.DueDate,out var d)&&d<today).ToString()});
        var suppliers = entries.Where(entry => entry.Kind == "Supplier").ToArray();
        if (suppliers.Length > 0)
            AddSupplierExport(c, sheets, metrics, suppliers, today);
        var documents = ReadDocuments(c, topic, scope, search);
        if (documents.Count > 0)
        {
            AddDocumentExport(c, sheets, metrics, documents);
            meta.Add(["Dokumente", "Registerdaten mit aktuellem Status, aktueller und zuletzt veröffentlichter Version."]);
        }
        foreach(var kind in new[]{"Control","Requirement","Finding"})sheets.Add(new(kind+"Status",["Status","Anzahl"],Entry.Statuses(kind).Select(status=>new[]{status,entries.Count(e=>e.Kind==kind&&e.Status==status).ToString()}).ToList()));
        sheets.Add(new("Glossar",["Begriff","Langform","Definition"],Query(c,null,"SELECT Term,Expanded,CASE WHEN CustomDefinition='' THEN Definition ELSE CustomDefinition END FROM Glossary ORDER BY Term",r=>new[]{r.GetString(0),r.GetString(1),r.GetString(2)})));
        sheets.Add(new("Quellen",["ID","Titel","URL","Referenz","Status","Letzter Erfolg"],Query(c,null,"SELECT Id,Title,Url,Reference,CheckState,LastSuccess FROM Sources s WHERE $p0='' OR EXISTS(SELECT 1 FROM TopicSources t WHERE t.SourceId=s.Id AND t.TopicId=$p0)",r=>Enumerable.Range(0,6).Select(r.GetString).ToArray(),topic)));
        sheets.Add(new("Definitionen",["Begriff","Definition"],new(){new[]{"Entwurf","Vorlage oder noch nicht bestätigte Umsetzung"},new[]{"Geprüft","Vom Anwender bewerteter Status, kein Zertifikat"},new[]{"Evidence","Metadaten tatsächlicher Dateien; Inhalte nicht eingebettet"},new[]{"Externe IDs","Mappings können auf außerhalb des Filters liegende Objekte verweisen"},new[]{"Excel","Texte als Inline-Strings, keine Benutzerformeln, keine externen Verknüpfungen"}}));return sheets;
    });

    private static void AddDocumentExport(SqliteConnection connection, List<ExportSheet> sheets, List<string[]> metrics,
        IReadOnlyList<DocumentRecord> documents)
    {
        var topicLabels = Query(connection, null, "SELECT Id,Title FROM Topics", r => (Id: r.GetString(0), Title: r.GetString(1)))
            .ToDictionary(topic => topic.Id, topic => topic.Title, StringComparer.Ordinal);
        var rows = documents.Select(document => new[]
        {
            document.Id,
            document.Draft.Title,
            DocumentRegister.CategoryLabels[document.Draft.Category],
            topicLabels[document.Draft.TopicId],
            document.Draft.Scope,
            document.Draft.Owner,
            DocumentRegister.StatusLabels[document.Status],
            document.Version.ToString(CultureInfo.InvariantCulture),
            document.PublishedVersion?.ToString(CultureInfo.InvariantCulture) ?? "",
            DateTimeOffset.TryParse(document.UpdatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.None, out var updated)
                ? updated.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : ""
        }).ToList();
        sheets.Add(new("Dokumente",
            ["Dokument-ID", "Titel", "Dokumentart", "Thema", "Geltungsbereich", "Verantwortung", "Status",
                "Aktuelle Version", "Zuletzt veröffentlichte Version", "Geändert am (UTC)"], rows));
        metrics.Add(["Dokumente im Register", documents.Count.ToString(CultureInfo.InvariantCulture)]);
        metrics.Add(["Dokumente in Prüfung", documents.Count(document => document.Status == DocumentStatus.InReview)
            .ToString(CultureInfo.InvariantCulture)]);
    }

    private static void AddSupplierExport(SqliteConnection connection, List<ExportSheet> sheets, List<string[]> metrics,
        IReadOnlyList<Entry> suppliers, DateOnly today)
    {
        var suppliersById = suppliers.ToDictionary(supplier => supplier.Id);
        var details = ReadSupplierDetails(connection, suppliersById.Keys);
        var supplierRows = suppliers.OrderBy(supplier => supplier.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(supplier => supplier.Code).ThenBy(supplier => supplier.Id).Select(supplier =>
            {
                var detail = details.GetValueOrDefault(supplier.Id);
                return new[]
                {
                    supplier.Id, supplier.Code, supplier.Title, detail?.Service ?? "",
                    detail is null ? "" : SupplierRegister.CriticalityLabels[detail.Criticality],
                    supplier.Status, supplier.Owner, supplier.DueDate, supplier.Scope, detail?.AgreementReference ?? "",
                    detail?.RiskRationale ?? "", supplier.SourceId, supplier.Locator, supplier.Body
                };
            }).ToList();
        sheets.Add(new("Lieferanten",
            ["ID", "Code", "Lieferant / Dienstleister", "Leistung", "Kritikalität", "Status", "Verantwortung", "Nächste Prüfung",
                "Geltungsbereich", "Vertragsreferenz", "Risikobegründung", "Quelle", "Fundstelle", "Beschreibung"], supplierRows));

        var reviewRows = ReadSupplierReviews(connection, suppliersById.Keys).Select(review => new[]
        {
            review.Id, review.SupplierId, suppliersById[review.SupplierId].Title,
            review.ReviewedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), review.Reviewer,
            SupplierRegister.ReviewOutcomeLabels[review.Outcome],
            review.NextReview?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", review.Notes
        }).ToList();
        sheets.Add(new("Lieferantenprüfungen",
            ["Prüfungs-ID", "Lieferanten-ID", "Lieferant / Dienstleister", "Prüfdatum", "Geprüft durch",
                "Ergebnis", "Nächste Prüfung", "Prüfnotiz"], reviewRows));

        var currentSuppliers = suppliers.Where(supplier => supplier.Status != "Beendet").ToArray();
        var dueDates = currentSuppliers.Select(supplier =>
            DateOnly.TryParseExact(supplier.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var due)
                ? (DateOnly?)due : null).ToArray();
        metrics.Add(["Lieferanten im Register", suppliers.Count.ToString(CultureInfo.InvariantCulture)]);
        metrics.Add(["Kritisch eingestufte Lieferanten (laufend)", currentSuppliers
            .Count(supplier => details.GetValueOrDefault(supplier.Id)?.Criticality == SupplierCriticality.Critical)
            .ToString(CultureInfo.InvariantCulture)]);
        metrics.Add(["Hoch eingestufte Lieferanten (laufend)", currentSuppliers
            .Count(supplier => details.GetValueOrDefault(supplier.Id)?.Criticality == SupplierCriticality.High)
            .ToString(CultureInfo.InvariantCulture)]);
        metrics.Add(["Überfällige Lieferantenprüfungen", dueDates.Count(due => due is { } date && date < today)
            .ToString(CultureInfo.InvariantCulture)]);
        metrics.Add(["Heute fällige Lieferantenprüfungen", dueDates.Count(due => due == today)
            .ToString(CultureInfo.InvariantCulture)]);
        metrics.Add(["Lieferanten ohne nächsten Prüftermin (laufend)", dueDates.Count(due => due is null)
            .ToString(CultureInfo.InvariantCulture)]);
    }

    public async Task RecordExportAsync(string path,string topic,string scope,string format)=>await Change("ExportCompleted","",Path.GetFileName(path),(c,t)=>{Exec(c,t,"INSERT INTO ExportJobs VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6,'Erfolgreich geschrieben')",Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow.ToString("O"),identity.User,topic,scope,format,Path.GetFileName(path));return true;});
}
