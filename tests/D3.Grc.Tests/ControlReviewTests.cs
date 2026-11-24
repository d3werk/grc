using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class ControlReviewTests
{
    [Fact]
    public async Task Review_links_risk_and_original_proof_and_advances_the_schedule_after_reopen()
    {
        using var directory = new TemporaryProject();
        const string password = "Synthetic control review fixture password";
        var identity = new LocalIdentity(Path.Combine(directory.Path, "identity"));
        await identity.RegisterAsync("review-fixture", password);
        Assert.True(await identity.LoginAsync("review-fixture", password));
        using var workspace = new Workspace(identity);
        var path = Path.Combine(directory.Path, "review.d3grc");
        await workspace.CreateAsync(path, "Kontrollprüfung", password);

        const string scope = "IT-Service";
        var control = new Entry { Kind = "Control", Title = "Wiederanlauf nachweisen", Scope = scope };
        var risk = new Entry { Kind = "Risk", Title = "Verlust produktiver Systeme", Scope = scope };
        var other = new Entry { Kind = "Control", Title = "Separater Prozess", Scope = "Personal" };
        foreach (var entry in new[] { control, risk, other })
            await workspace.SaveEntryAsync(entry, "ISMS", "Testdatensatz für die Kontrollprüfung");
        await workspace.LinkAsync(risk.Id, control.Id, "Risiko → Control", "INTERNAL", "Teilweise", scope,
            "Geprüfter Wiederanlauf reduziert die Ausfallwirkung.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var plan = await workspace.ConfigureControlReviewAsync(control.Id, 1, 0, "IT-Betrieb", 3, today.AddDays(-1),
            "Verantwortung für Wiederanlauftests festgelegt.");
        Assert.Equal((1, 0), await workspace.ControlReminderCountsAsync(today));
        var item = Assert.Single(await workspace.ControlReviewItemsAsync(scope, DateTimeOffset.Now));
        Assert.Equal("IT-Betrieb", item.Control.Owner);
        Assert.Equal(risk.Id, Assert.Single(item.RiskIds));
        Assert.Equal(plan, item.Schedule);
        await Assert.ThrowsAsync<ArgumentException>(async () => await workspace.CompleteControlReviewAsync(control.Id,
            item.Control.Version, plan.Version, today, "Wirksam", "Wiederanlauf geprüft.", null));

        await workspace.ImportEvidenceAsync(other.Id, "INTERNAL", "Fremder Testbericht", "Fixture", "other.txt", [9],
            DateTimeOffset.UtcNow.AddDays(1));
        var wrongProof = Assert.Single(await workspace.EvidenceAsync(other.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.CompleteControlReviewAsync(control.Id,
            item.Control.Version, plan.Version, today, "Wirksam", "Wiederanlauf geprüft.", wrongProof.Id));
        await workspace.ImportEvidenceAsync(control.Id, "INTERNAL", "Wiederanlaufprotokoll", "Fixture", "test.txt", [1, 2, 3],
            DateTimeOffset.UtcNow.AddDays(1));
        var proof = Assert.Single(await workspace.EvidenceAsync(control.Id));
        var next = await workspace.CompleteControlReviewAsync(control.Id, item.Control.Version, plan.Version, today,
            "Wirksam", "Wiederanlaufzeit anhand des Protokolls geprüft.", proof.Id);
        Assert.Equal(today.AddMonths(3), next.NextReviewOn);
        Assert.Equal((0, 0), await workspace.ControlReminderCountsAsync(today));
        Assert.Equal((0, 1), await workspace.ControlReminderCountsAsync(today.AddMonths(3)));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.CompleteControlReviewAsync(control.Id,
            item.Control.Version, plan.Version, today, "Wirksam", "Der alte Prüfplan wurde bereits abgeschlossen.", proof.Id));

        await workspace.CloseAsync();
        await workspace.OpenAsync(path, password);
        var revision = Assert.Single(await workspace.ControlReviewsAsync(control.Id));
        Assert.Equal("IT-Betrieb", revision.Owner);
        Assert.Equal(identity.User, revision.Reviewer);
        Assert.Equal(item.Control.Version, revision.ControlVersion);
        Assert.Equal(proof.Id, revision.EvidenceId);
        Assert.Equal(proof.Hash, revision.EvidenceHash);
        Assert.True(await workspace.VerifyAsync());
    }

    private sealed class TemporaryProject : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("d3-control-review-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
