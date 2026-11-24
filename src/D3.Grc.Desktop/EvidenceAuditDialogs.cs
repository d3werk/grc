using Avalonia;
using Avalonia.Controls;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task ShowEvidenceIntegrityAsync(Proof selected)
    {
        var review = await _workspace.CheckEvidenceIntegrityAsync(selected.Id);
        var fileResult = review.FileHashMatches ? "Datei und gespeicherter SHA-256-Wert stimmen überein." :
            "Datei und gespeicherter SHA-256-Wert weichen voneinander ab.";
        var journalResult = review.ImportHashMatches switch
        {
            true => "Der Importwert stimmt mit der Datei überein. Die lokale Journalkette ist konsistent.",
            false => "Der Importwert oder die lokale Journalkette konnte nicht bestätigt werden.",
            null => "Für diesen Nachweis liegt kein einzeln protokollierter Importwert vor."
        };
        var integrity = Ui.Card(Ui.Stack(Ui.Heading("Integritätsprüfung", 16, 2), Ui.Text(fileResult), Ui.Secondary(journalResult),
            Ui.Text($"Gespeicherter SHA-256-Wert\n{review.Proof.Hash}"),
            Ui.Text($"Neu berechneter SHA-256-Wert\n{review.ComputedSha256}")));
        integrity.Background = review.FileHashMatches && review.ImportHashMatches != false ? Ui.SoftGreen : Ui.SoftAmber;
        var links = Ui.Stack(Ui.Heading("Verwendet in", 16, 2),
            Ui.Text($"{review.Controls.Count} Controls · {review.AuditRevisions} Audit-Prüfstände · " +
                $"{review.ControlChecks} Kontrollprüfungen · {review.FindingClosures} abgeschlossene Feststellungen"));
        foreach (var control in review.Controls)
            links.Children.Add(Ui.Secondary(control.Title));
        await ShowAuditReadOnlyDialogAsync("Nachweis prüfen", review.Proof.Title,
            Ui.Stack(integrity, Ui.Secondary($"{review.Proof.FileName} · Erhoben {EvidenceDateDisplay(review.Proof.Collected)} · Gültig bis {EvidenceDateDisplay(review.Proof.ValidUntil)}"),
                Ui.Secondary(review.Proof.Origin), Ui.Card(links)));
    }

    private async Task ShowAuditSnapshotHistoryAsync(Entry audit, AuditCoverage item)
    {
        var snapshots = await _workspace.AuditTestSnapshotsAsync(audit.Id, item.ControlId);
        var details = Ui.Stack();
        foreach (var snapshot in snapshots)
        {
            var row = Ui.Stack(Ui.Heading(snapshot.Outcome, 16, 2), Ui.Secondary(EvidenceDateDisplay(snapshot.TestedUtc)),
                Ui.Secondary($"{snapshot.ControlCode} {snapshot.ControlTitle} · Control-Fassung {snapshot.ControlVersion?.ToString() ?? "unbekannt"} · Quelle {snapshot.SourceId}"),
                Ui.Text(snapshot.Notes));
            if (snapshot.IsLegacy)
                row.Children.Add(Ui.Secondary("Beim Upgrade übernommener Prüfstand."));
            if (snapshot.EvidenceSha256 is { } hash)
                row.Children.Add(Ui.Secondary($"SHA-256 des zugeordneten Nachweises\n{hash}"));
            details.Children.Add(Ui.Card(row));
        }
        if (snapshots.Count == 0)
            details.Children.Add(Ui.Notice("Für dieses Control liegen noch keine archivierten Prüfstände vor."));
        await ShowAuditReadOnlyDialogAsync("Prüfverlauf", $"{audit.Title} · bis zu 100 neueste Prüfstände", details);
    }

    private async Task ExportAuditEvidenceArchiveAsync(Entry audit)
    {
        if (!await Ui.Confirm(this, "Gespeicherte Prüfstände mit den zugeordneten Originaldateien als unverschlüsseltes ZIP exportieren?", "Dossier exportieren"))
            return;
        var path = await SavePath("D3-GRC-Pruefdossier.zip", "zip");
        if (path is null)
            return;
        var archive = await _workspace.CreateAuditEvidenceArchiveAsync(audit.Id);
        await Files.AtomicAsync(path, archive.Content);
        await _workspace.RecordExportAsync(path, _topicId, audit.Scope, "Prüfdossier ZIP");
        _status.Text = $"Prüfdossier gespeichert · {archive.Revisions} Prüfstände · {archive.Files} Dateien";
    }

    private async Task ShowAuditReadOnlyDialogAsync(string title, string subtitle, Control contents)
    {
        var window = new Window { Title = title, Width = 720, Height = 590, MinWidth = 490, MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var close = new Button { Content = "Schließen", IsCancel = true, IsDefault = true };
        close.Click += (_, _) => window.Close();
        window.Content = new Border { Padding = new Thickness(22), Child = Ui.Fill(
            Ui.Stack(Ui.Heading(title, 22, 1), Ui.Secondary(subtitle)), Ui.Scroll(contents), close) };
        await window.ShowDialog(this);
    }

    private static string EvidenceDateDisplay(string iso) => DateTimeOffset.TryParse(iso, out var value) ? value.ToLocalTime().ToString("g") : iso;
}
