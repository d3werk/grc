CREATE TABLE ControlReviewSchedules (
    ControlId TEXT PRIMARY KEY REFERENCES Records(Id) ON DELETE CASCADE,
    IntervalMonths INTEGER NOT NULL CHECK (IntervalMonths BETWEEN 1 AND 60),
    NextReviewOn TEXT NOT NULL CHECK (length(NextReviewOn) = 10),
    Version INTEGER NOT NULL CHECK (Version > 0)
);

CREATE TABLE ControlReviews (
    Sequence INTEGER PRIMARY KEY AUTOINCREMENT,
    Id TEXT NOT NULL UNIQUE,
    ControlId TEXT NOT NULL REFERENCES Records(Id) ON DELETE RESTRICT,
    PerformedOn TEXT NOT NULL CHECK (length(PerformedOn) = 10),
    ControlVersion INTEGER NOT NULL CHECK (ControlVersion > 0),
    Owner TEXT NOT NULL,
    Reviewer TEXT NOT NULL,
    Outcome TEXT NOT NULL CHECK (Outcome IN ('Wirksam','Teilweise wirksam','Verbesserungsbedarf')),
    Notes TEXT NOT NULL CHECK (length(trim(Notes)) BETWEEN 1 AND 30000),
    EvidenceId TEXT REFERENCES Evidence(Id) ON DELETE RESTRICT,
    EvidenceHash TEXT NOT NULL DEFAULT '',
    RecordedUtc TEXT NOT NULL,
    CHECK (Outcome <> 'Wirksam' OR EvidenceId IS NOT NULL)
);

CREATE INDEX IX_ControlReviews_ControlSequence ON ControlReviews(ControlId, Sequence DESC);

CREATE TRIGGER ControlReviews_PreventUpdate BEFORE UPDATE ON ControlReviews
BEGIN
    SELECT RAISE(ABORT, 'Gespeicherte Control-Prüfungen bleiben als Historie erhalten.');
END;

CREATE TRIGGER ControlReviews_PreventDelete BEFORE DELETE ON ControlReviews
BEGIN
    SELECT RAISE(ABORT, 'Gespeicherte Control-Prüfungen bleiben als Historie erhalten.');
END;

CREATE TRIGGER ControlReviews_PreventReplace BEFORE INSERT ON ControlReviews
WHEN EXISTS (SELECT 1 FROM ControlReviews WHERE Id = NEW.Id OR Sequence = NEW.Sequence)
BEGIN
    SELECT RAISE(ABORT, 'Gespeicherte Control-Prüfungen bleiben als Historie erhalten.');
END;
