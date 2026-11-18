using System.Net;
using System.Security.Cryptography;
using static D3.Grc.Core.Database;
namespace D3.Grc.Core;
public sealed record SourceResult(string Id,bool Success,string Hash,string Detail);
public static class SourceUpdates
{
    private static readonly HashSet<string> Hosts=new(StringComparer.OrdinalIgnoreCase){"eur-lex.europa.eu","www.gesetze-im-internet.de","www.bsi.bund.de","www.iso.org","learn.microsoft.com","spdx.dev","cyclonedx.org"};
    public static bool Allowed(Uri u)=>u.Scheme=="https"&&u.Port==443&&u.UserInfo==""&&Hosts.Contains(u.IdnHost);
    public static async Task<SourceResult> ProbeAsync(Source s)
    {
        try{
            if(s.Url=="")throw new IOException("Interne Quelle: kein Onlineabruf.");var uri=new Uri(s.Url);using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));using var handler=new HttpClientHandler{AllowAutoRedirect=false,AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate};using var client=new HttpClient(handler);client.DefaultRequestHeaders.UserAgent.ParseAdd("D3GRC/0.6");
            for(int i=0;i<=3;i++){if(!Allowed(uri))throw new IOException("Quelle oder Weiterleitung nicht freigegeben.");using var response=await client.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,timeout.Token);if((int)response.StatusCode is >=300 and <=399){var target=response.Headers.Location??throw new IOException("Weiterleitung ohne Ziel.");uri=target.IsAbsoluteUri?target:new Uri(uri,target);continue;}response.EnsureSuccessStatusCode();
                var type=response.Content.Headers.ContentType?.MediaType??"";if(!new[]{"text/html","text/plain","application/xhtml+xml","application/json"}.Contains(type))throw new IOException("Quelltyp nicht unterstützt.");
                await using var input=await response.Content.ReadAsStreamAsync(timeout.Token);using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[32768];int total=0,n;while((n=await input.ReadAsync(buffer,timeout.Token))>0){total+=n;if(total>4*1024*1024)throw new IOException("Quelle größer als 4 MiB.");hash.AppendData(buffer,0,n);}if(total<64)throw new IOException("Inhalt unplausibel kurz.");return new(s.Id,true,Convert.ToHexString(hash.GetHashAndReset()),$"HTTP {(int)response.StatusCode}; {total} Byte; technischer Check, keine Rechtsprüfung.");}
            throw new IOException("Zu viele Weiterleitungen.");
        }catch(Exception ex) when(ex is HttpRequestException or IOException or OperationCanceledException or UriFormatException){return new(s.Id,false,"",ex.Message);}
    }
}
public sealed partial class Workspace
{
    public async Task StoreSourceResultAsync(SourceResult result)=>await Change("SourceChecked",result.Id,result.Detail,(c,t)=>{
        var now=DateTimeOffset.UtcNow.ToString("O");var accepted=Scalar(c,t,"SELECT AcceptedHash FROM Sources WHERE Id=$p0",result.Id);var state=!result.Success?"Prüfung fehlgeschlagen":accepted==""?"Erststand – fachlich prüfen":accepted==result.Hash?"Technisch unverändert":"Änderung – fachlich prüfen";
        Exec(c,t,"UPDATE Sources SET LastAttempt=$p0,CheckState=$p1 WHERE Id=$p2",now,state,result.Id);if(result.Success)Exec(c,t,"UPDATE Sources SET CandidateHash=$p0,LastSuccess=$p1 WHERE Id=$p2",result.Hash,now,result.Id);
        Exec(c,t,"INSERT INTO SourceChecks(SourceId,CheckedUtc,State,Hash,Detail) VALUES($p0,$p1,$p2,$p3,$p4)",result.Id,now,state,result.Hash,result.Detail);return true;
    });
    public async Task AcceptSourceAsync(string id,string note)=>await Change("SourceAccepted",id,note,(c,t)=>{
        Reason(note);var hash=Scalar(c,t,"SELECT CandidateHash FROM Sources WHERE Id=$p0",id);var state=Scalar(c,t,"SELECT CheckState FROM Sources WHERE Id=$p0",id);if(hash==""||state.Contains("fehlgeschlagen"))throw new ArgumentException("Zuerst erfolgreich abrufen.");
        var now=DateTimeOffset.UtcNow.ToString("O");Exec(c,t,"INSERT INTO SourceVersions VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6)",Guid.NewGuid().ToString("N"),id,"Bestätigter technischer Referenzstand",Scalar(c,t,"SELECT LastSuccess FROM Sources WHERE Id=$p0",id),now,hash,note);Exec(c,t,"UPDATE Sources SET AcceptedHash=CandidateHash,CheckState='Referenzstand bestätigt' WHERE Id=$p0",id);return true;
    });
    public async Task SaveSourceNoteAsync(string id,string note)=>await Change("SourceNote",id,note,(c,t)=>{Reason(note);Exec(c,t,"UPDATE Sources SET CustomSummary=$p0 WHERE Id=$p1",note,id);return true;});
    public Task<List<string>> SourceHistoryAsync(string id)=>Read(c=>Query(c,null,"SELECT CheckedUtc,State,Detail FROM SourceChecks WHERE SourceId=$p0 ORDER BY Id DESC LIMIT 100",r=>$"{r.GetString(0)} | {r.GetString(1)} | {r.GetString(2)}",id));
}
