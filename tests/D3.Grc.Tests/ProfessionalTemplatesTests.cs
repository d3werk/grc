using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class ProfessionalTemplatesTests
{
    [Fact]
    public async Task Explicit_topics_filter_professional_templates_and_encrypted_reopen_keeps_edits()
    {
        const string password = "Synthetic template test passphrase";
        const string process = "D3-PRO-PROCESS-PROFILE";
        const string audit = "D3-PRO-AUDIT-WORKPAPER";
        const string legacy = "D3-DOC-SUPPLIER";
        var temporary = Directory.CreateTempSubdirectory("grc-professional-templates-");
        try
        {
            var identity = new LocalIdentity(Path.Combine(temporary.FullName, "identity"));
            await identity.RegisterAsync("template-review", password);
            Assert.True(await identity.LoginAsync("template-review", password));
            using var workspace = new Workspace(identity);
            var path = Path.Combine(temporary.FullName, "project.d3grc");
            await workspace.CreateAsync(path, "Template project", password);

            var processTemplates = await workspace.TemplatesAsync("PROCESS", "Procedure");
            var baseline = Assert.Single(processTemplates, template => template.Id == process);
            Assert.Contains("## Prozessprofil", baseline.Effective, StringComparison.Ordinal);
            Assert.DoesNotContain(processTemplates, template => template.Id == audit);
            Assert.Contains(await workspace.TemplatesAsync("PROCESS"), template => template.Id == audit);
            Assert.Contains(await workspace.TemplatesAsync("ISMS"), template => template.Id == audit);

            var privacy = await workspace.TemplatesAsync("PRIVACY");
            Assert.DoesNotContain(privacy, template => template.Id is process or audit);
            Assert.Contains(privacy, template => template.Id == legacy);

            await workspace.SaveTemplateAsync(process, "## Genehmigter Prozess\nDieses Projekt verwendet einen eigenen Ablauf.");
            await workspace.SaveTemplateAsync(legacy, "## Lieferantenrichtlinie\nIndividuelle Fassung des Projekts.");
            await workspace.CloseAsync();
            await workspace.OpenAsync(path, password);

            var edited = Assert.Single(await workspace.TemplatesAsync("PROCESS"), template => template.Id == process);
            Assert.Equal(baseline.Body, edited.Body);
            Assert.Contains("eigenen Ablauf", edited.Effective, StringComparison.Ordinal);
            var editedLegacy = Assert.Single(await workspace.TemplatesAsync("PRIVACY"), template => template.Id == legacy);
            Assert.Contains("Individuelle Fassung", editedLegacy.Effective, StringComparison.Ordinal);
        }
        finally
        {
            temporary.Delete(recursive: true);
        }
    }
}
