using Avalonia.Controls;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
public sealed partial class MainWindow
{
    private async Task<Control> Dashboard()
    {
        var controls=_entries.Where(e=>e.Kind=="Control").ToList();var requirements=_entries.Where(e=>e.Kind=="Requirement").ToList();var findings=_entries.Where(e=>e.Kind=="Finding").ToList();var measures=_entries.Where(e=>e.Kind=="Measure").ToList();
        var evidence=new Dictionary<string,Proof>();int without=0;foreach(var control in controls){var proofs=await _workspace.EvidenceAsync(control.Id);foreach(var p in proofs)evidence[p.Id]=p;if(!proofs.Any(p=>DateTimeOffset.TryParse(p.ValidUntil,out var until)&&until>DateTimeOffset.UtcNow))without++;}
        async Task Drill(string kind,string status=""){_page=kind;_statusFilter=status;await Render();}
        Control MetricCard(string label,int value,string hint,string kind)=>Ui.Card(Ui.Stack(Ui.Text(label,13),Ui.Text(value.ToString(),34,true),Ui.Text(hint,12),Button("Details",()=>Drill(kind))));
        var cards=Ui.Row(MetricCard("Anforderungen",requirements.Count,"Anwendbarkeit separat bewerten","Requirement"),MetricCard("Controls",controls.Count,"Entwurf ist keine Umsetzung","Control"),MetricCard("Offene Findings",findings.Count(f=>f.Status!="Geschlossen"),"Aktueller Bearbeitungsstand","Finding"),MetricCard("Nachweise",evidence.Count,"Tatsächliche Dateien im Scope","Evidence"));
        Control Bars(string title,IEnumerable<(string Label,int Count,string Kind,string Status)> values)
        {
            var data=values.ToList();var panel=Ui.Stack(Ui.Text(title,18,true));var max=Math.Max(1,data.Select(x=>x.Count).DefaultIfEmpty().Max());
            foreach(var item in data){var bar=new ProgressBar{Minimum=0,Maximum=max,Value=item.Count,Height=16,Foreground=Ui.Green};panel.Children.Add(Ui.Stack(Ui.Row(Ui.Text(item.Label,13),Ui.Text(item.Count.ToString(),15,true),Button("Ansehen",()=>Drill(item.Kind,item.Status))),bar));}
            if(data.Sum(x=>x.Count)==0)panel.Children.Add(Ui.Notice("Noch keine Daten für diese Auswertung. Keine positive Erfüllungsaussage."));return Ui.Card(panel);
        }
        var statusBars=Bars("Control-Status",Entry.Statuses("Control").Select(s=>(s,controls.Count(c=>c.Status==s),"Control",s)));
        var applicability=Bars("Anwendbarkeit der Anforderungen",Entry.Statuses("Requirement").Select(s=>(s,requirements.Count(c=>c.Status==s),"Requirement",s)));
        var findingBars=Bars("Findings nach Status",Entry.Statuses("Finding").Select(s=>(s,findings.Count(c=>c.Status==s),"Finding",s)));
        var due=measures.Where(m=>m.Status!="Erledigt").ToList();var today=DateOnly.FromDateTime(DateTime.Today);int overdue=due.Count(m=>DateOnly.TryParse(m.DueDate,out var d)&&d<today);
        var management=Ui.Scroll(Ui.Stack(cards,Ui.Notice("Thema, Scope und Suche oben steuern Dashboard und Excel-Export. Stichtag: jetzt. Keine historische Zeitreihe und kein pauschaler Compliance-Score."),Ui.Row(Button("Excel-Bericht exportieren",()=>Export(true),true),Button("Druckansicht",()=>Export(false))),applicability,Ui.Card(Ui.Stack(Ui.Text("Datenqualität",18,true),Ui.Text(_entries.Count(e=>e.Owner=="")+" Datensätze ohne Verantwortliche"),Ui.Text(overdue+" überfällige Maßnahmen")))));
        var proofView=Ui.Scroll(Ui.Stack(Ui.Card(Ui.Stack(Ui.Text("Nachweisabdeckung",18,true),Ui.Text(without+" von "+controls.Count+" Controls ohne aktuell gültige Evidence",24,true),Ui.Text("Ein Nachweis ist nicht automatisch ein positiver Wirksamkeitsnachweis. Historische Audits behalten den Prüfzeitpunkt."))),findingBars,Button("Evidence öffnen",async()=>{_page="Evidence";await Render();})));
        var sourceView=Ui.Scroll(Ui.Stack(Ui.Card(Ui.Stack(Ui.Text("Quellen im gewählten Thema",18,true),Ui.Text(_sources.Count+" passende Quellen"),Ui.Text(_sources.Count(s=>s.State.Contains("prüfen")||s.State.Contains("fehlgeschlagen"))+" Quellen mit technischem/fachlichem Prüfhinweis"),Ui.Text(_sources.Count(s=>s.LastSuccess=="")+" Quellen ohne erfolgreichen technischen Abruf"))),Button("Quellen prüfen",async()=>{_page="Sources";await Render();}),Ui.Notice("Ungeprüfte Quellen sind nicht aktuell bestätigt. Technische Erreichbarkeit ist keine juristische Freigabe.")));
        return new TabControl{ItemsSource=new[]{Ui.Tab("Management",management),Ui.Tab("Umsetzung",Ui.Scroll(Ui.Stack(statusBars))),Ui.Tab("Nachweise & Audits",proofView),Ui.Tab("Quellen & Qualität",sourceView)}};
    }
}
