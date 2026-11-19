using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public enum SupplierCriticality { Low = 0, Medium = 1, High = 2, Critical = 3 }

public enum SupplierReviewOutcome { Approved = 0, ApprovedWithActions = 1, Rejected = 2 }

public sealed record SupplierDetails(
    string Service,
    SupplierCriticality Criticality,
    string AgreementReference,
    string RiskRationale);

public sealed record SupplierReviewInput(
    DateOnly ReviewedOn,
    string Reviewer,
    SupplierReviewOutcome Outcome,
    string Notes,
    DateOnly? NextReview);

public sealed record SupplierReview(
    string Id,
    string SupplierId,
    DateOnly ReviewedOn,
    string Reviewer,
    SupplierReviewOutcome Outcome,
    string Notes,
    DateOnly? NextReview,
    string RecordedUtc);

public static class SupplierRegister
{
    public static IReadOnlyDictionary<SupplierCriticality, string> CriticalityLabels { get; } =
        new ReadOnlyDictionary<SupplierCriticality, string>(new Dictionary<SupplierCriticality, string>
        {
            [SupplierCriticality.Low] = "Niedrig",
            [SupplierCriticality.Medium] = "Mittel",
            [SupplierCriticality.High] = "Hoch",
            [SupplierCriticality.Critical] = "Kritisch"
        });

    public static IReadOnlyDictionary<SupplierReviewOutcome, string> ReviewOutcomeLabels { get; } =
        new ReadOnlyDictionary<SupplierReviewOutcome, string>(new Dictionary<SupplierReviewOutcome, string>
        {
            [SupplierReviewOutcome.Approved] = "Freigegeben",
            [SupplierReviewOutcome.ApprovedWithActions] = "Freigegeben mit Maßnahmen",
            [SupplierReviewOutcome.Rejected] = "Abgelehnt"
        });

    public static bool IsReviewStatus(string status) => status is "Freigegeben" or "Maßnahmen offen" or "Abgelehnt";

    internal static string ReviewStatus(SupplierReviewOutcome outcome) => outcome switch
    {
        SupplierReviewOutcome.Approved => "Freigegeben",
        SupplierReviewOutcome.ApprovedWithActions => "Maßnahmen offen",
        SupplierReviewOutcome.Rejected => "Abgelehnt",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };

    internal static SupplierDetails ValidateDetails(SupplierDetails? details)
    {
        if (details is null)
            throw new ArgumentException("Leistung und Risikobegründung des Dienstleisters erfassen.", nameof(details));

        var service = details.Service?.Trim() ?? "";
        var reference = details.AgreementReference?.Trim() ?? "";
        var rationale = details.RiskRationale?.Trim() ?? "";
        if (service.Length is < 1 or > 1000)
            throw new ArgumentException("Leistung mit 1 bis 1.000 Zeichen angeben.", nameof(details));
        if (!Enum.IsDefined(details.Criticality))
            throw new ArgumentException("Kritikalität auswählen.", nameof(details));
        if (reference.Length > 1000)
            throw new ArgumentException("Vertragsreferenz mit höchstens 1.000 Zeichen angeben.", nameof(details));
        if (rationale.Length is < 1 or > 30000)
            throw new ArgumentException("Risiko mit 1 bis 30.000 Zeichen begründen.", nameof(details));

        return details with { Service = service, AgreementReference = reference, RiskRationale = rationale };
    }

    internal static SupplierReviewInput ValidateReview(SupplierReviewInput review)
    {
        ArgumentNullException.ThrowIfNull(review);
        var reviewer = review.Reviewer?.Trim() ?? "";
        var notes = review.Notes?.Trim() ?? "";
        if (review.ReviewedOn == DateOnly.MinValue || review.ReviewedOn > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Ein gültiges Prüfdatum bis einschließlich heute angeben.", nameof(review));
        if (reviewer.Length is < 1 or > 240)
            throw new ArgumentException("Prüfende Person oder Rolle mit 1 bis 240 Zeichen angeben.", nameof(review));
        if (!Enum.IsDefined(review.Outcome))
            throw new ArgumentException("Prüfergebnis auswählen.", nameof(review));
        if (notes.Length is < 1 or > 30000)
            throw new ArgumentException("Prüfentscheidung mit 1 bis 30.000 Zeichen begründen.", nameof(review));
        if (review.NextReview is { } nextReview && nextReview <= review.ReviewedOn)
            throw new ArgumentException("Die nächste Prüfung muss nach dem Prüfdatum liegen.", nameof(review));
        if (review.Outcome != SupplierReviewOutcome.Rejected && review.NextReview is null)
            throw new ArgumentException("Für eine Freigabe den Termin der nächsten Prüfung angeben.", nameof(review));

        return review with { Reviewer = reviewer, Notes = notes };
    }
}

public sealed partial class Workspace
{
    private const string SupplierDateFormat = "yyyy-MM-dd";

    public Task<Entry> SupplierEntryAsync(string supplierId) =>
        Read(connection => GetSupplier(connection, null, supplierId));

    public Task<SupplierDetails?> SupplierDetailsAsync(string supplierId) =>
        Read(connection => ReadSupplierDetails(connection, [supplierId]).GetValueOrDefault(supplierId));

    public Task<IReadOnlyDictionary<string, SupplierDetails>> SupplierDetailsAsync(IReadOnlyCollection<string> supplierIds)
    {
        ArgumentNullException.ThrowIfNull(supplierIds);
        var ids = supplierIds.ToArray();
        return Read(connection => ReadSupplierDetails(connection, ids));
    }

    public Task<List<SupplierReview>> SupplierReviewsAsync(string supplierId) =>
        Read(connection => ReadSupplierReviews(connection, [supplierId]));

    public async Task RecordSupplierReviewAsync(string supplierId, SupplierReviewInput review)
    {
        review = SupplierRegister.ValidateReview(review);
        var reviewId = Guid.NewGuid().ToString("N");
        var reviewedOn = FormatSupplierDate(review.ReviewedOn);
        var nextReview = review.NextReview is { } next ? FormatSupplierDate(next) : null;
        var detail = JsonSerializer.Serialize(new { reviewId, review });

        await Change("SupplierReviewRecorded", supplierId, detail, (connection, transaction) =>
        {
            var supplier = GetSupplier(connection, transaction, supplierId);
            if (supplier.Status == "Beendet")
                throw new ArgumentException("Vor einer weiteren Prüfung den Dienstleister wieder in Bearbeitung nehmen.");

            var latestReview = LatestSupplierReviewDate(connection, transaction, supplierId);
            Exec(connection, transaction, """
                INSERT INTO SupplierReviews (Id, SupplierId, ReviewedOn, Reviewer, Outcome, Notes, NextReview, RecordedUtc)
                VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7)
                """, reviewId, supplierId, reviewedOn, review.Reviewer, (int)review.Outcome, review.Notes,
                nextReview, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));

            if (string.CompareOrdinal(reviewedOn, latestReview) >= 0)
                Exec(connection, transaction, "UPDATE Records SET Status=$p0, DueDate=$p1, Version=Version+1 WHERE Id=$p2",
                    SupplierRegister.ReviewStatus(review.Outcome), nextReview ?? "", supplierId);

            return true;
        });
    }

    private static IReadOnlyDictionary<string, SupplierDetails> ReadSupplierDetails(SqliteConnection connection, IEnumerable<string> supplierIds) =>
        Query(connection, null, """
            SELECT SupplierId, Service, Criticality, AgreementReference, RiskRationale FROM SupplierDetails
            WHERE SupplierId IN (SELECT value FROM json_each($p0))
            """, reader => (Id: reader.GetString(0), Details: new SupplierDetails(
                reader.GetString(1), (SupplierCriticality)reader.GetInt32(2), reader.GetString(3), reader.GetString(4))),
            JsonSerializer.Serialize(supplierIds)).ToDictionary(row => row.Id, row => row.Details, StringComparer.Ordinal);

    private static List<SupplierReview> ReadSupplierReviews(SqliteConnection connection, IEnumerable<string> supplierIds) =>
        Query(connection, null, """
            SELECT Id, SupplierId, ReviewedOn, Reviewer, Outcome, Notes, NextReview, RecordedUtc FROM SupplierReviews
            WHERE SupplierId IN (SELECT value FROM json_each($p0))
            ORDER BY ReviewedOn DESC, rowid DESC
            """, reader => new SupplierReview(reader.GetString(0), reader.GetString(1), ParseSupplierDate(reader.GetString(2)),
                reader.GetString(3), (SupplierReviewOutcome)reader.GetInt32(4), reader.GetString(5),
                reader.IsDBNull(6) ? null : ParseSupplierDate(reader.GetString(6)), reader.GetString(7)), JsonSerializer.Serialize(supplierIds));

    private static void SaveSupplierDetails(SqliteConnection connection, SqliteTransaction transaction, string supplierId, SupplierDetails details) =>
        Exec(connection, transaction, """
            INSERT INTO SupplierDetails (SupplierId, Service, Criticality, AgreementReference, RiskRationale) VALUES ($p0, $p1, $p2, $p3, $p4)
            ON CONFLICT(SupplierId) DO UPDATE SET Service=excluded.Service, Criticality=excluded.Criticality,
                AgreementReference=excluded.AgreementReference, RiskRationale=excluded.RiskRationale
            """, supplierId, details.Service, (int)details.Criticality, details.AgreementReference, details.RiskRationale);

    private static void ValidateSupplierEdit(SqliteConnection connection, SqliteTransaction transaction, Entry supplier, Entry? previous)
    {
        if (string.IsNullOrWhiteSpace(supplier.Owner))
            throw new ArgumentException("Eine verantwortliche Rolle oder Person für den Dienstleister angeben.");
        if (supplier.Status != previous?.Status && SupplierRegister.IsReviewStatus(supplier.Status))
            throw new ArgumentException("Freigabe, offene Maßnahmen und Ablehnung über eine Dienstleisterprüfung festhalten.");

        if (supplier.DueDate == "")
            return;

        if (!DateOnly.TryParseExact(supplier.DueDate, SupplierDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var nextReview) ||
            nextReview == DateOnly.MinValue)
            throw new ArgumentException("Nächste Prüfung als Datum im Format JJJJ-MM-TT angeben.");

        var latestReview = LatestSupplierReviewDate(connection, transaction, supplier.Id);
        if (string.CompareOrdinal(supplier.DueDate, latestReview) <= 0)
            throw new ArgumentException("Die nächste Prüfung muss nach dem letzten Prüfdatum liegen.");
    }

    private static string LatestSupplierReviewDate(SqliteConnection connection, SqliteTransaction transaction, string supplierId) =>
        Scalar(connection, transaction, "SELECT MAX(ReviewedOn) FROM SupplierReviews WHERE SupplierId=$p0", supplierId);

    private static Entry GetSupplier(SqliteConnection connection, SqliteTransaction? transaction, string supplierId)
    {
        var supplier = Get(connection, transaction, supplierId);
        RequireKind(supplier, "Supplier");
        return supplier;
    }

    private static string FormatSupplierDate(DateOnly date) => date.ToString(SupplierDateFormat, CultureInfo.InvariantCulture);

    private static DateOnly ParseSupplierDate(string date) => DateOnly.ParseExact(date, SupplierDateFormat, CultureInfo.InvariantCulture);
}
