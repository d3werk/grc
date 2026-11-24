using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class ContractModuleTests
{
    [Fact]
    public void ActiveContractsRequirePartiesAndAValidTermAndDecisionsNeedReasons()
    {
        var validate = Assert.Single(ContractModule.Specs).Validate!;
        var contract = new Entry { Kind = ContractModule.Kind, Status = "Aktiv", Owner = "Einkauf" };
        var fields = new Dictionary<string, string>
        {
            ["internal_party"] = "Demo GmbH", ["counterparty"] = "Fiktiver Partner",
            ["starts_on"] = "2031-01-01", ["ends_on"] = "2031-12-31"
        };
        validate(contract, fields);
        Assert.Throws<ArgumentException>(() => validate(contract, new Dictionary<string, string>(fields) { ["ends_on"] = "2030-12-31" }));
        Assert.Throws<ArgumentException>(() => validate(contract, new Dictionary<string, string>(fields) { ["counterparty"] = "" }));
        Assert.Throws<ArgumentException>(() => validate(contract, new Dictionary<string, string>(fields) { ["decision"] = "Kündigen" }));
        validate(contract, new Dictionary<string, string>(fields) { ["decision"] = "Kündigen", ["decision_note"] = "Kündigung vorbereiten" });
    }

    [Fact]
    public void DeadlinesSortByDateAndExcludeEndedContracts()
    {
        static ModuleRow Row(string title, string status, Dictionary<string, string> fields) =>
            new(new Entry { Kind = ContractModule.Kind, Title = title, Status = status }, fields);

        var deadlines = ContractModule.Deadlines([
            Row("Support", "Aktiv", new() { ["notice_due"] = "2030-10-01", ["ends_on"] = "2030-12-31" }),
            Row("Lizenz", "In Prüfung", new() { ["review_due"] = "2030-09-01" }),
            Row("Archiv", "Beendet", new() { ["ends_on"] = "2029-12-31" })
        ]);
        Assert.Equal(new[] { new DateOnly(2030, 9, 1), new DateOnly(2030, 10, 1), new DateOnly(2030, 12, 31) },
            deadlines.Select(deadline => deadline.Date));
        Assert.Equal(new[] { "Prüftermin", "Kündigungsstichtag", "Laufzeitende" }, deadlines.Select(deadline => deadline.Label));
    }
}
