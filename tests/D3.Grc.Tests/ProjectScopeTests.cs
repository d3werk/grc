using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class ProjectScopeTests
{
    [Fact]
    public async Task Project_scope_options_keep_exact_current_assignments_per_project()
    {
        const string password = "Synthetic project scope test password";
        string directory = Directory.CreateTempSubdirectory("d3-scope-").FullName;
        try
        {
            var identity = new LocalIdentity(Path.Combine(directory, "identity"));
            await identity.RegisterAsync("tester", password);
            Assert.True(await identity.LoginAsync("tester", password));
            using var workspace = new Workspace(identity);
            var firstPath = Path.Combine(directory, "first.d3grc");
            await workspace.CreateAsync(firstPath, "Erster Mandant", password);

            var risk = new Entry { Kind = "Risk", Title = "Datenschutzrisiko Nord", SourceId = "GDPR", Scope = "Testbereich Nord" };
            var control = new Entry { Kind = "Control", Title = "Datenschutzkontrolle Nord", SourceId = "GDPR", Scope = risk.Scope };
            var other = new Entry { Kind = "Risk", Title = "Datenschutzrisiko nord", SourceId = "GDPR", Scope = "Testbereich nord" };
            await workspace.SaveEntryAsync(risk, "PRIVACY", "Synthetisches Risiko anlegen");
            await workspace.SaveEntryAsync(control, "PRIVACY", "Synthetische Kontrolle anlegen");
            await workspace.SaveEntryAsync(other, "PRIVACY", "Synthetisches Risiko anlegen");
            await workspace.CreateDocumentAsync(new DocumentDraft("Richtlinie", "PRIVACY", "Testbereich Süd", DocumentCategory.Policy,
                "Datenschutz", "Synthetischer Richtlinientext"), "Synthetischen Entwurf anlegen");
            await workspace.LinkAsync(risk.Id, control.Id, "Risiko → Control", "GDPR", "Teilweise", "Testbereich Ost", "Synthetische Zuordnung");

            var scopes = await workspace.ScopeOptionsAsync();
            Assert.DoesNotContain("", scopes);
            Assert.Single(scopes, name => name == risk.Scope);
            Assert.Contains(other.Scope, scopes);
            Assert.Contains("Testbereich Süd", scopes);
            Assert.Contains("Testbereich Ost", scopes);

            var scoped = await workspace.EntriesAsync("PRIVACY", "Risk", scope: risk.Scope);
            Assert.Contains(scoped, entry => entry.Id == risk.Id);
            Assert.DoesNotContain(scoped, entry => entry.Id == other.Id);
            var projectWide = await workspace.EntriesAsync("PRIVACY", "Risk");
            Assert.Equal(risk.Scope, Assert.Single(projectWide, entry => entry.Id == risk.Id).Scope);
            Assert.Equal(other.Scope, Assert.Single(projectWide, entry => entry.Id == other.Id).Scope);

            await workspace.CloseAsync();
            await workspace.CreateAsync(Path.Combine(directory, "second.d3grc"), "Zweiter Mandant", password);
            Assert.DoesNotContain(await workspace.ScopeOptionsAsync(), scope => scope.StartsWith("Testbereich", StringComparison.Ordinal));
            await workspace.CloseAsync();
            await workspace.OpenAsync(firstPath, password);
            Assert.Equal(scopes, await workspace.ScopeOptionsAsync());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
