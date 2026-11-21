using System.IO.Compression;
using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class CompanyConfigurationTests
{
    [Fact]
    public void Small_company_starts_with_governance_and_risks()
    {
        var plan = CompanyPlanning.Build(CompanyProfile.Default);

        Assert.Equal(new[] { "governance", "risk" }, plan
            .Where(module => module.Priority == CompanyModulePriority.Key)
            .Select(module => module.Id));
        Assert.Equal(CompanyModulePriority.Optional, plan.Single(module => module.Id == "audit").Priority);
        Assert.Equal(CompanyModulePriority.Optional, plan.Single(module => module.Id == "supplier").Priority);
        Assert.Equal(CompanyModulePriority.Optional, plan.Single(module => module.Id == "documents").Priority);
        Assert.All(plan, module =>
        {
            Assert.False(string.IsNullOrWhiteSpace(module.Why));
            Assert.False(string.IsNullOrWhiteSpace(module.NextStep));
        });
    }

    [Fact]
    public void Finance_enterprise_prioritizes_review_and_cross_scope_work()
    {
        var profile = new CompanyProfile(CompanySize.LargeEnterprise, CompanyIndustry.Finance,
            CompanyFactors.CloudServices, ["PRIVACY", "ISMS"], 9, 15, CompanyRiskFocus.ThirdPartyRisk);

        var plan = CompanyPlanning.Build(profile).ToDictionary(module => module.Id);

        Assert.Equal(CompanyModulePriority.Key, plan["dora"].Priority);
        Assert.Equal("DORA-Betroffenheit prüfen", plan["dora"].Title);
        Assert.Contains("Finanzbranche", plan["dora"].Why);
        Assert.Equal(CompanyModulePriority.Optional, plan["nis2"].Priority);
        foreach (string id in new[] { "cloud", "privacy", "evidence", "audit", "mapping", "supplier", "documents" })
        {
            Assert.Equal(CompanyModulePriority.Key, plan[id].Priority);
        }

        Assert.Equal("CLOUD", plan["risk"].TopicId);
    }

    [Fact]
    public void Document_register_is_a_cross_topic_key_module_for_critical_processes()
    {
        var profile = CompanyProfile.Default with { Factors = CompanyFactors.CriticalProcesses };
        var module = Assert.Single(CompanyPlanning.Build(profile), module => module.Id == "documents");

        Assert.Equal(CompanyModulePriority.Key, module.Priority);
        Assert.Equal("Documents", module.TargetPage);
        Assert.Equal("", module.TopicId);
        Assert.Contains("kritische Geschäftsprozesse", module.Why);
    }

    [Fact]
    public void Topic_selection_and_operating_facts_apply_without_enterprise_size()
    {
        var profile = CompanyProfile.Default with
        {
            Industry = CompanyIndustry.Energy,
            Factors = CompanyFactors.AiSystems | CompanyFactors.CriticalProcesses,
            Topics = ["OSS"],
            SiteCount = 2
        };

        var plan = CompanyPlanning.Build(profile).ToDictionary(module => module.Id);

        Assert.Equal(CompanyModulePriority.Key, plan["nis2"].Priority);
        Assert.Equal(CompanyModulePriority.Key, plan["ai"].Priority);
        Assert.Equal(CompanyModulePriority.Key, plan["bcm"].Priority);
        Assert.Equal(CompanyModulePriority.Key, plan["audit"].Priority);
        Assert.Contains("ausgewählt", plan["oss"].Why);
    }

    [Fact]
    public void Profile_validation_rejects_unknown_values_and_normalizes_topics()
    {
        Assert.Throws<ArgumentException>(() => CompanyPlanning.Validate(CompanyProfile.Default with { SiteCount = 0 }));
        Assert.Throws<ArgumentException>(() => CompanyPlanning.Validate(CompanyProfile.Default with { Topics = ["REVIEW"] }));
        Assert.Throws<ArgumentException>(() => CompanyPlanning.Validate(CompanyProfile.Default with { Factors = (CompanyFactors)256 }));

        var validated = CompanyPlanning.Validate(CompanyProfile.Default with { Topics = ["CLOUD", "OSS", "CLOUD"] });
        Assert.Equal(new[] { "CLOUD", "OSS" }, validated.Topics);
    }

    [Fact]
    public void External_suppliers_enable_register_for_a_small_company()
    {
        var profile = CompanyProfile.Default with { Factors = CompanyFactors.ExternalSuppliers };
        var module = Assert.Single(CompanyPlanning.Build(profile), module => module.Id == "supplier");
        Assert.Equal(CompanyModulePriority.Key, module.Priority);
        Assert.Equal("Supplier", module.TargetPage);
        Assert.Contains("externe Partner", module.Why);
    }

    [Fact]
    public async Task Company_profile_is_optional_and_persists_with_the_project()
    {
        const string password = "Synthetic company test password";
        string directory = Directory.CreateTempSubdirectory("d3-company-").FullName;
        try
        {
            var identity = new LocalIdentity(Path.Combine(directory, "identity"));
            await identity.RegisterAsync("tester", password);
            Assert.True(await identity.LoginAsync("tester", password));
            using var workspace = new Workspace(identity);
            string path = Path.Combine(directory, "company.d3grc");
            await workspace.CreateAsync(path, "Company fixture", password);
            Assert.Null(await workspace.GetCompanyProfileAsync());
            Assert.Equal(19, (await workspace.ExportSnapshotAsync("", "", "")).Count);

            var profile = new CompanyProfile(CompanySize.LargeEnterprise, CompanyIndustry.Health,
                CompanyFactors.PersonalData | CompanyFactors.CloudServices, ["PRIVACY", "CLOUD"],
                13, 5, CompanyRiskFocus.DataProtection);
            await workspace.SaveCompanyProfileAsync(profile);
            await workspace.CloseAsync();
            await workspace.OpenAsync(path, password);

            var loaded = Assert.IsType<CompanyProfile>(await workspace.GetCompanyProfileAsync());
            Assert.Equal(profile.Topics, loaded.Topics);
            Assert.Equal(profile, loaded with { Topics = profile.Topics });
            Assert.Contains(await workspace.JournalAsync(), row => row.Contains("CompanyProfileSaved"));

            var exported = await workspace.ExportSnapshotAsync("OSS", "", "");
            var context = Assert.Single(exported, sheet => sheet.Name == "Exportkontext").Rows;
            Assert.Contains(context, row => row[0] == "Unternehmensgröße" && row[1] == "Großunternehmen");
            Assert.Contains(context, row => row[0] == "Standorte" && row[1] == "13");
            var plan = Assert.Single(exported, sheet => sheet.Name == "Umsetzungsplan").Rows;
            Assert.Contains(plan, row => row[0] == "Schlüsselmodul" && row[1] == "Datenschutz");
            Assert.Contains(plan, row => row[0] == "Optionale Ergänzung");
            Assert.Contains("<h2>Umsetzungsplan</h2>", ExcelExport.Html(workspace.Name, exported));
            using var xlsx = new ZipArchive(new MemoryStream(ExcelExport.Build(exported)));
            using var workbook = new StreamReader(xlsx.GetEntry("xl/workbook.xml")!.Open());
            Assert.Contains("Umsetzungsplan", await workbook.ReadToEndAsync());

            await workspace.CloseAsync();
            string initialized = Path.Combine(directory, "initialized.d3grc");
            await workspace.CreateAsync(initialized, "Initialized company fixture", password, companyProfile: profile);
            await workspace.CloseAsync();
            await workspace.OpenAsync(initialized, password);

            var fromCreation = Assert.IsType<CompanyProfile>(await workspace.GetCompanyProfileAsync());
            Assert.Equal(profile.Topics, fromCreation.Topics);
            Assert.Equal(profile, fromCreation with { Topics = profile.Topics });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
