using System.Globalization;
using Avalonia.Controls;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task<Control> RequirementTraceAsync(string recordId)
    {
        var trace = await _workspace.RequirementTraceAsync(recordId);
        var title = string.IsNullOrEmpty(trace.Code) ? trace.Title : $"{trace.Code} · {trace.Title}";
        var source = string.IsNullOrEmpty(trace.Locator) ? trace.SourceTitle : $"{trace.SourceTitle} · {trace.Locator}";
        var panel = Ui.Stack(Ui.Text(title, 20, true), Ui.Text($"{trace.Status} · {source}"));
        if (trace.Controls.Count == 0)
        {
            panel.Children.Add(Ui.Notice("Keine bestätigte Control-Zuordnung. Unter Mappings kann eine Zuordnung mit Abdeckung und Begründung erfasst werden."));
            return panel;
        }

        panel.Children.Add(Ui.Notice("Der Status folgt Erhebungs- und Ablaufdatum beim Öffnen. Beim Abruf wird der Dateihash geprüft. Die Wirksamkeit wird in der Auditprüfung bewertet."));
        foreach (var control in trace.Controls)
            panel.Children.Add(TraceControlCard(control));
        return panel;
    }

    private static Control TraceControlCard(RequirementControlTrace control)
    {
        var title = string.IsNullOrEmpty(control.Code) ? control.Title : $"{control.Code} · {control.Title}";
        var scope = string.IsNullOrEmpty(control.Scope) ? "Zuordnung ohne angegebenen Geltungsbereich" : $"Geltungsbereich der Zuordnung · {control.Scope}";
        var details = Ui.Stack(
            Ui.Text(title, 17, true),
            Ui.Text($"Controlstatus {control.Status} · Abdeckung {control.Coverage}"),
            Ui.Text(scope),
            Ui.Text($"Quelle der Zuordnung · {control.SourceTitle}"),
            Ui.Text("Begründung der Zuordnung", 13, true),
            Ui.Text(control.Reason),
            Ui.Text("Zugeordnete Nachweise", 15, true));

        if (control.Evidence.Count == 0)
        {
            details.Children.Add(Ui.Text("Keine Nachweise zugeordnet. Unter Evidence können Dateien importiert oder vorhandene Nachweise zugeordnet werden."));
            return Ui.Card(details);
        }

        var current = control.Evidence.Count(evidence => evidence.Status == TraceEvidenceStatus.Current);
        var expired = control.Evidence.Count(evidence => evidence.Status == TraceEvidenceStatus.Expired);
        var pending = control.Evidence.Count(evidence => evidence.Status == TraceEvidenceStatus.NotYetCurrent);
        var invalid = control.Evidence.Count - current - expired - pending;
        var counts = $"{current} zeitlich gültig · {expired} abgelaufen";
        if (pending > 0)
            counts += $" · {pending} noch nicht gültig";
        if (invalid > 0)
            counts += $" · {invalid} mit ungültigen Datumsangaben";
        details.Children.Add(Ui.Text(counts, 14, true));

        foreach (var proof in control.Evidence)
        {
            var state = proof.Status switch
            {
                TraceEvidenceStatus.Current => "Zeitlich gültig",
                TraceEvidenceStatus.Expired => "Abgelaufen",
                TraceEvidenceStatus.NotYetCurrent => "Noch nicht gültig",
                _ => "Datumsangaben prüfen"
            };
            var expiry = proof.ValidUntil?.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            var expiryText = expiry is null ? "" : $" · gültig bis {expiry}";
            details.Children.Add(Ui.Stack(
                Ui.Text(proof.Title, 14, true),
                Ui.Text($"{proof.FileName} · {state}{expiryText}", 12)));
        }
        return Ui.Card(details);
    }
}
