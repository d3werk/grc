using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public enum CompanySize
{
    SmallMedium = 0,
    LargeEnterprise = 1
}

public enum CompanyIndustry
{
    Other = 0,
    Finance = 1,
    Health = 2,
    Manufacturing = 3,
    Energy = 4,
    Transport = 5,
    DigitalServices = 6,
    PublicSector = 7,
    TradeAndServices = 8,
    EducationResearch = 9
}

[Flags]
public enum CompanyFactors
{
    None = 0,
    PersonalData = 1 << 0,
    Nis2Review = 1 << 1,
    DoraReview = 1 << 2,
    AiSystems = 1 << 3,
    CloudServices = 1 << 4,
    CriticalProcesses = 1 << 5,
    OpenSourceSoftware = 1 << 6,
    ExternalSuppliers = 1 << 7
}

public enum CompanyRiskFocus
{
    Balanced = 0,
    DataProtection = 1,
    InformationSecurity = 2,
    OperationalResilience = 3,
    ThirdPartyRisk = 4,
    SupplyChain = 5,
    AiGovernance = 6
}

public sealed record CompanyProfile(
    CompanySize Size,
    CompanyIndustry Industry,
    CompanyFactors Factors,
    string[] Topics,
    int SiteCount,
    int OrganizationalUnitCount,
    CompanyRiskFocus RiskFocus)
{
    public static CompanyProfile Default => new(
        CompanySize.SmallMedium, CompanyIndustry.Other, CompanyFactors.None,
        ["ISMS"], 1, 1, CompanyRiskFocus.Balanced);
}

public enum CompanyModulePriority { Key, Optional }

public sealed record CompanyModuleRecommendation(
    string Id,
    string Title,
    CompanyModulePriority Priority,
    string Why,
    string NextStep,
    string TopicId,
    string TargetPage);

public static class CompanyPlanning
{
    private const CompanyFactors AllFactors = CompanyFactors.PersonalData | CompanyFactors.Nis2Review |
        CompanyFactors.DoraReview | CompanyFactors.AiSystems | CompanyFactors.CloudServices |
        CompanyFactors.CriticalProcesses | CompanyFactors.OpenSourceSoftware | CompanyFactors.ExternalSuppliers;

    public static IReadOnlyDictionary<CompanySize, string> SizeLabels { get; } =
        new ReadOnlyDictionary<CompanySize, string>(new Dictionary<CompanySize, string>
        {
            [CompanySize.SmallMedium] = "KMU",
            [CompanySize.LargeEnterprise] = "Großunternehmen"
        });

    public static IReadOnlyDictionary<CompanyIndustry, string> IndustryLabels { get; } =
        new ReadOnlyDictionary<CompanyIndustry, string>(new Dictionary<CompanyIndustry, string>
        {
            [CompanyIndustry.Other] = "Sonstige Branche / noch offen",
            [CompanyIndustry.Finance] = "Finanzen und Versicherungen",
            [CompanyIndustry.Health] = "Gesundheit und Pflege",
            [CompanyIndustry.Manufacturing] = "Industrie und Fertigung",
            [CompanyIndustry.Energy] = "Energie und Versorgung",
            [CompanyIndustry.Transport] = "Verkehr und Logistik",
            [CompanyIndustry.DigitalServices] = "IT und digitale Dienste",
            [CompanyIndustry.PublicSector] = "Öffentliche Verwaltung",
            [CompanyIndustry.TradeAndServices] = "Handel und Dienstleistungen",
            [CompanyIndustry.EducationResearch] = "Bildung und Forschung"
        });

    public static IReadOnlyDictionary<CompanyFactors, string> FactorLabels { get; } =
        new ReadOnlyDictionary<CompanyFactors, string>(new Dictionary<CompanyFactors, string>
        {
            [CompanyFactors.PersonalData] = "Personenbezogene Daten werden verarbeitet",
            [CompanyFactors.Nis2Review] = "NIS2-Betroffenheit prüfen",
            [CompanyFactors.DoraReview] = "DORA-Betroffenheit prüfen",
            [CompanyFactors.AiSystems] = "KI-Systeme werden genutzt oder bereitgestellt",
            [CompanyFactors.CloudServices] = "Cloud-Dienste werden genutzt",
            [CompanyFactors.CriticalProcesses] = "Kritische Geschäftsprozesse sind vorhanden",
            [CompanyFactors.OpenSourceSoftware] = "Open-Source-Software wird eingesetzt oder weitergegeben",
            [CompanyFactors.ExternalSuppliers] = "Externe Lieferanten oder Dienstleister sind für wesentliche Leistungen relevant"
        });

    public static IReadOnlyDictionary<CompanyRiskFocus, string> RiskFocusLabels { get; } =
        new ReadOnlyDictionary<CompanyRiskFocus, string>(new Dictionary<CompanyRiskFocus, string>
        {
            [CompanyRiskFocus.Balanced] = "Ausgewogene Erstaufnahme",
            [CompanyRiskFocus.DataProtection] = "Datenschutz",
            [CompanyRiskFocus.InformationSecurity] = "Informationssicherheit",
            [CompanyRiskFocus.OperationalResilience] = "Betriebsstabilität und Notfallvorsorge",
            [CompanyRiskFocus.ThirdPartyRisk] = "IT-Dienstleister und Cloud-Abhängigkeiten",
            [CompanyRiskFocus.SupplyChain] = "Softwarelieferkette",
            [CompanyRiskFocus.AiGovernance] = "KI-Governance"
        });

    public static IReadOnlyDictionary<string, string> TopicLabels { get; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PRIVACY"] = "Datenschutz",
            ["ISMS"] = "Informationssicherheit",
            ["NIS2"] = "NIS2",
            ["AI"] = "KI-Governance",
            ["CLOUD"] = "Cloud Security",
            ["BCM"] = "Business Continuity",
            ["OSS"] = "Open Source",
            ["DORA"] = "DORA"
        });

    public static CompanyProfile Validate(CompanyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!Enum.IsDefined(profile.Size) || !Enum.IsDefined(profile.Industry) || !Enum.IsDefined(profile.RiskFocus) ||
            (profile.Factors & ~AllFactors) != 0)
        {
            throw new ArgumentException("Unternehmensprofil enthält eine unbekannte Auswahl.", nameof(profile));
        }

        if (profile.SiteCount is < 1 or > 10000 || profile.OrganizationalUnitCount is < 1 or > 10000)
        {
            throw new ArgumentException("Für Standorte und Organisationseinheiten jeweils 1 bis 10.000 angeben.", nameof(profile));
        }

        if (profile.Topics is null || profile.Topics.Any(topic => topic is null || !TopicLabels.ContainsKey(topic)))
        {
            throw new ArgumentException("Unternehmensprofil enthält ein unbekanntes Fachthema.", nameof(profile));
        }

        return profile with { Topics = profile.Topics.Distinct(StringComparer.Ordinal).ToArray() };
    }

    public static IReadOnlyList<CompanyModuleRecommendation> Build(CompanyProfile profile)
    {
        profile = Validate(profile);
        var plan = new List<CompanyModuleRecommendation>();
        bool enterprise = profile.Size == CompanySize.LargeEnterprise;
        bool distributed = profile.SiteCount > 1 || profile.OrganizationalUnitCount > 1;
        bool criticalProcesses = Has(CompanyFactors.CriticalProcesses);
        string riskTopic = profile.RiskFocus switch
        {
            CompanyRiskFocus.DataProtection => "PRIVACY",
            CompanyRiskFocus.OperationalResilience => "BCM",
            CompanyRiskFocus.ThirdPartyRisk => "CLOUD",
            CompanyRiskFocus.SupplyChain => "OSS",
            CompanyRiskFocus.AiGovernance => "AI",
            CompanyRiskFocus.InformationSecurity => "ISMS",
            _ => profile.Topics.FirstOrDefault() ?? "ISMS"
        };

        Add("governance", "Verantwortung und Geltungsbereich", "ISMS", "Control",
            enterprise || distributed
                ? "Gemeinsame Basis-Controls festlegen, Scope je Standort oder Einheit benennen und lokale Abweichungen dokumentieren."
                : "Einen überschaubaren Geltungsbereich und je Control eine verantwortliche Rolle festlegen.",
            null,
            "Verantwortung und ein definierter Geltungsbereich bilden die Grundlage für die Umsetzung.",
            When(enterprise, "Das Profil ist auf ein Großunternehmen ausgerichtet."),
            When(distributed, $"Das Profil umfasst {profile.SiteCount} Standorte und {profile.OrganizationalUnitCount} Organisationseinheiten."));

        Add("risk", "Risiken und Maßnahmen priorisieren", riskTopic, "Risk",
            "Wesentliche Risiken im gewählten Schwerpunkt erfassen, Auswirkungen und Verantwortung beschreiben und Maßnahmen ableiten.",
            null,
            $"Der gewählte Risikofokus lautet „{RiskFocusLabels[profile.RiskFocus]}“.");

        AddTopic("privacy", "Datenschutz", "PRIVACY", "Requirement",
            "Verarbeitung und betroffene Datenarten erfassen. Relevante Anforderungen und Maßnahmen mit Quellenbezug bewerten.",
            "Datenschutz rückt bei Verarbeitung personenbezogener Daten oder einem entsprechenden Risikofokus nach vorn.",
            When(Has(CompanyFactors.PersonalData), "Das Profil nennt die Verarbeitung personenbezogener Daten."),
            When(profile.Industry is CompanyIndustry.Health or CompanyIndustry.EducationResearch, "In dieser Branche kann der Umgang mit sensiblen Daten eine frühe Prüfung sinnvoll machen."),
            Focus(CompanyRiskFocus.DataProtection));

        AddTopic("nis2", "NIS2-Betroffenheit prüfen", "NIS2", "Requirement",
            "Tätigkeiten, Unternehmensdaten und geltende nationale Umsetzung erfassen. Die eigene Anwendbarkeitsentscheidung mit Quelle und Begründung dokumentieren.",
            "Eine frühe Prüfung ist bei einem NIS2-Hinweis oder in Bereichen wie Energie, Verkehr, Gesundheit und digitalen Diensten sinnvoll.",
            When(Has(CompanyFactors.Nis2Review), "Das Profil sieht eine NIS2-Prüfung vor."),
            When(profile.Industry is CompanyIndustry.Energy or CompanyIndustry.Transport or CompanyIndustry.Health or CompanyIndustry.DigitalServices,
                "Tätigkeiten in der gewählten Branche können in den Anwendungsbereich fallen und sollten geprüft werden."));

        AddTopic("dora", "DORA-Betroffenheit prüfen", "DORA", "Requirement",
            "Art der Finanzdienstleistung und bereitgestellte IKT-Leistungen erfassen. Betroffenheit und einschlägige Anforderungen fachlich beurteilen.",
            "Eine frühe Prüfung ist bei einem DORA-Hinweis oder bei Tätigkeiten in der Finanzbranche sinnvoll.",
            When(Has(CompanyFactors.DoraReview), "Das Profil sieht eine DORA-Prüfung vor."),
            When(profile.Industry == CompanyIndustry.Finance, "Die gewählte Finanzbranche gibt Anlass zu einer Prüfung des DORA-Anwendungsbereichs."));

        AddTopic("ai", "KI-Systeme und Einsatzkontext", "AI", "AiSystem",
            "KI-Systeme erfassen, Einsatzkontext und Rolle beschreiben und die einschlägigen Anforderungen prüfen.",
            "KI-Governance rückt beim Einsatz oder bei der Bereitstellung von KI-Systemen nach vorn.",
            When(Has(CompanyFactors.AiSystems), "Das Profil nennt den Einsatz oder die Bereitstellung von KI-Systemen."),
            Focus(CompanyRiskFocus.AiGovernance));

        AddTopic("cloud", "Cloud-Dienste und IKT-Dienstleister", "CLOUD", "Control",
            "Wesentliche Dienste und Abhängigkeiten im Geltungsbereich beschreiben und Kontrollen für Zugriff, Ausfall und Dienstleisterwechsel festlegen.",
            "Das Modul rückt bei genutzten Cloud-Diensten oder einem Risikofokus auf IT-Dienstleister nach vorn.",
            When(Has(CompanyFactors.CloudServices), "Das Profil nennt genutzte Cloud-Dienste."),
            Focus(CompanyRiskFocus.ThirdPartyRisk));

        Add("supplier", "Lieferanten und Dienstleister bewerten", "", "Supplier",
            "Wesentliche Leistungen, Verträge, verantwortliche Einheiten und Prüftermine im Lieferantenregister erfassen. Risiken und Controls zuordnen.",
            "Das Register lässt sich bei wesentlichen externen Abhängigkeiten ergänzen.",
            When(Has(CompanyFactors.ExternalSuppliers), "Das Profil nennt für wesentliche Leistungen relevante externe Partner."),
            When(Has(CompanyFactors.CloudServices), "Genutzte Cloud-Dienste schaffen Abhängigkeiten von externen Anbietern."),
            When(Has(CompanyFactors.DoraReview) || profile.Industry == CompanyIndustry.Finance, "Für die DORA-Prüfung sollten die IKT-Dienstleister und ihre Leistungen erhoben werden."),
            Focus(CompanyRiskFocus.ThirdPartyRisk));

        AddTopic("bcm", "Notfallvorsorge und Wiederanlauf", "BCM", "Control",
            "Kritische Prozesse und Abhängigkeiten erfassen, Wiederanlaufziele dokumentieren und einen Notfalltest vorbereiten.",
            "Die Notfallvorsorge rückt bei kritischen Prozessen oder einem Fokus auf Betriebsstabilität nach vorn.",
            When(criticalProcesses, "Das Profil nennt kritische Geschäftsprozesse."),
            Focus(CompanyRiskFocus.OperationalResilience));

        AddTopic("oss", "Open Source und Softwarelieferkette", "OSS", "OssComponent",
            "Die wichtigsten Komponenten mit Version, Lizenz und Herkunft erfassen und Zuständigkeiten für Aktualisierung und Freigabe festlegen.",
            "Das Modul rückt beim Einsatz oder bei der Weitergabe von Open Source oder einem Fokus auf Lieferkettenrisiken nach vorn.",
            When(Has(CompanyFactors.OpenSourceSoftware), "Das Profil nennt eingesetzte oder weitergegebene Open-Source-Software."),
            Focus(CompanyRiskFocus.SupplyChain));

        Add("evidence", "Nachweise bereitstellen", "", "Evidence",
            "Für umgesetzte Controls prüfbare Dokumente mit Herkunft und Gültigkeit hinterlegen. Gemeinsame Nachweise bei fachlicher Eignung wiederverwenden.",
            "Für den Einstieg lassen sich Nachweise zusammen mit den ersten umgesetzten Controls erfassen.",
            When(enterprise || distributed, "Mehrere Verantwortungsbereiche können gemeinsame und lokale Nachweise benötigen."),
            When(criticalProcesses, "Für kritische Geschäftsprozesse ist eine früh prüfbare Dokumentation sinnvoll."));

        Add("audit", "Prüfungen planen", riskTopic, "Audit",
            "Einen geeigneten Prüf-Scope festlegen, Controls zuordnen und Stichproben anhand konkreter Kriterien und Nachweise dokumentieren.",
            "Interne Prüfungen können auf die erste Umsetzung und Sammlung von Nachweisen folgen.",
            When(enterprise || distributed, "Die auf mehrere Bereiche ausgerichtete Umsetzung lässt sich mit abgestimmten Prüfumfängen nachvollziehen."),
            When(criticalProcesses, "Für kritische Prozesse empfiehlt sich eine geplante Wirksamkeitsprüfung."));

        Add("mapping", "Anforderungen und Controls zuordnen", "", "Mappings",
            "Kontrollen mit Anforderungen verknüpfen. Abdeckung, Scope und verbleibende Lücken je Zuordnung begründen.",
            "Zuordnungen helfen besonders, sobald mehrere Themen oder Organisationseinheiten überlappende Anforderungen bearbeiten.",
            When(profile.Topics.Length > 1, "Mehrere gewählte Themen können sich in der Umsetzung überschneiden."),
            When(enterprise, "Bestehende Kontrollen lassen sich bereichsübergreifend mit Anforderungen abgleichen."));

        return plan.OrderBy(module => module.Priority).ToList().AsReadOnly();

        bool Has(CompanyFactors factor) => (profile.Factors & factor) != 0;

        string? Focus(CompanyRiskFocus focus) => When(profile.RiskFocus == focus, "Das Thema entspricht dem gewählten Risikofokus.");

        void AddTopic(string id, string title, string topic, string page, string nextStep, string optionalReason, params string?[] reasons)
        {
            var selection = When(profile.Topics.Contains(topic, StringComparer.Ordinal), "Das Fachthema wurde im Profil ausgewählt.");
            Add(id, title, topic, page, nextStep, optionalReason, [selection, .. reasons]);
        }

        void Add(string id, string title, string topic, string page, string nextStep, string? optionalReason, params string?[] reasons)
        {
            string why = string.Join(" ", reasons.OfType<string>());
            var module = why.Length == 0 && optionalReason is not null
                ? new CompanyModuleRecommendation(id, title, CompanyModulePriority.Optional, optionalReason, nextStep, topic, page)
                : new CompanyModuleRecommendation(id, title, CompanyModulePriority.Key, why, nextStep, topic, page);
            plan.Add(module);
        }
    }

    private static string? When(bool applies, string reason) => applies ? reason : null;
}

public sealed partial class Workspace
{
    private const string CompanyProfileKey = "CompanyProfile";
    private const int CompanyProfileVersion = 1;

    private sealed record StoredCompanyProfile(int Version, CompanyProfile Profile);

    public Task<CompanyProfile?> GetCompanyProfileAsync() => Read(ReadCompanyProfile);

    private static CompanyProfile? ReadCompanyProfile(SqliteConnection c)
    {
        string json = Scalar(c, null, "SELECT Value FROM Metadata WHERE Key=$p0", CompanyProfileKey);
        if (json.Length == 0)
        {
            return null;
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredCompanyProfile>(json);
            if (stored is null || stored.Version != CompanyProfileVersion)
            {
                throw new InvalidDataException("Version des Unternehmensprofils wird nicht unterstützt.");
            }

            return CompanyPlanning.Validate(stored.Profile);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new InvalidDataException("Unternehmensprofil kann nicht gelesen werden.", ex);
        }
    }

    public async Task SaveCompanyProfileAsync(CompanyProfile profile)
    {
        var validated = CompanyPlanning.Validate(profile);
        await Change("CompanyProfileSaved", CompanyProfileKey,
            JsonSerializer.Serialize(validated), (c, t) =>
            {
                PersistCompanyProfile(c, t, validated);
                return true;
            });
    }

    private static void PersistCompanyProfile(SqliteConnection c, SqliteTransaction t, CompanyProfile profile)
    {
        var stored = new StoredCompanyProfile(CompanyProfileVersion, CompanyPlanning.Validate(profile));
        Exec(c, t, "INSERT INTO Metadata(Key,Value) VALUES($p0,$p1) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value",
            CompanyProfileKey, JsonSerializer.Serialize(stored));
    }
}
