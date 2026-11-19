CREATE TABLE SupplierDetails (
    SupplierId TEXT PRIMARY KEY NOT NULL REFERENCES Records(Id) ON DELETE CASCADE,
    Service TEXT NOT NULL CHECK (length(trim(Service)) BETWEEN 1 AND 1000),
    Criticality INTEGER NOT NULL CHECK (Criticality IN (0,1,2,3)),
    AgreementReference TEXT NOT NULL CHECK (length(AgreementReference) <= 1000),
    RiskRationale TEXT NOT NULL CHECK (length(trim(RiskRationale)) BETWEEN 1 AND 30000)
);

CREATE TABLE SupplierReviews (
    Id TEXT PRIMARY KEY NOT NULL,
    SupplierId TEXT NOT NULL REFERENCES SupplierDetails(SupplierId) ON DELETE RESTRICT,
    ReviewedOn TEXT NOT NULL CHECK (length(ReviewedOn) = 10),
    Reviewer TEXT NOT NULL CHECK (length(trim(Reviewer)) BETWEEN 1 AND 240),
    Outcome INTEGER NOT NULL CHECK (Outcome IN (0,1,2)),
    Notes TEXT NOT NULL CHECK (length(trim(Notes)) BETWEEN 1 AND 30000),
    NextReview TEXT CHECK (NextReview IS NULL OR (length(NextReview) = 10 AND NextReview > ReviewedOn)),
    RecordedUtc TEXT NOT NULL,
    CHECK (Outcome = 2 OR NextReview IS NOT NULL)
);

CREATE INDEX IX_SupplierReviews_SupplierReviewedOn ON SupplierReviews(SupplierId, ReviewedOn DESC);

CREATE TRIGGER SupplierReviews_PreventUpdate BEFORE UPDATE ON SupplierReviews
BEGIN
    SELECT RAISE(ABORT, 'Dienstleisterprüfungen werden als Historie aufbewahrt.');
END;

CREATE TRIGGER SupplierReviews_PreventDelete BEFORE DELETE ON SupplierReviews
BEGIN
    SELECT RAISE(ABORT, 'Dienstleisterprüfungen werden als Historie aufbewahrt.');
END;

INSERT INTO Metadata(Key, Value) VALUES('MinAppVersion', '0.8.0')
ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
