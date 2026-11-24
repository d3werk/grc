CREATE TABLE IksProfiles (
    ControlId TEXT PRIMARY KEY NOT NULL REFERENCES Records(Id) ON DELETE CASCADE,
    Objective TEXT NOT NULL CHECK (length(trim(Objective)) BETWEEN 1 AND 4000),
    ControlType INTEGER NOT NULL CHECK (ControlType BETWEEN 0 AND 2),
    ControlMode INTEGER NOT NULL CHECK (ControlMode BETWEEN 0 AND 2),
    Frequency INTEGER NOT NULL CHECK (Frequency BETWEEN 0 AND 6),
    IsKeyControl INTEGER NOT NULL CHECK (IsKeyControl IN (0,1))
);

CREATE TABLE IksChecks (
    Id TEXT PRIMARY KEY NOT NULL,
    ControlId TEXT NOT NULL REFERENCES Records(Id) ON DELETE RESTRICT,
    ControlVersion INTEGER NOT NULL CHECK (ControlVersion >= 1),
    CheckedOn TEXT NOT NULL CHECK (length(CheckedOn) = 10),
    Reviewer TEXT NOT NULL CHECK (length(trim(Reviewer)) BETWEEN 1 AND 240),
    CheckType INTEGER NOT NULL CHECK (CheckType BETWEEN 0 AND 3),
    Outcome INTEGER NOT NULL CHECK (Outcome BETWEEN 0 AND 2),
    TestApproach TEXT NOT NULL CHECK (length(trim(TestApproach)) BETWEEN 1 AND 4000),
    Notes TEXT NOT NULL CHECK (length(trim(Notes)) BETWEEN 1 AND 30000),
    EvidenceId TEXT REFERENCES Evidence(Id) ON DELETE RESTRICT,
    MeasureId TEXT REFERENCES Records(Id) ON DELETE RESTRICT,
    RecordedUtc TEXT NOT NULL,
    CHECK (Outcome <> 0 OR CheckType NOT IN (0,3) OR EvidenceId IS NOT NULL)
);

CREATE INDEX IX_IksChecks_ControlCheckedOn ON IksChecks(ControlId, CheckedOn DESC);

CREATE TRIGGER IksChecks_PreventUpdate BEFORE UPDATE ON IksChecks
BEGIN
    SELECT RAISE(ABORT, 'Kontrollprüfungen bleiben als Historie erhalten.');
END;

CREATE TRIGGER IksChecks_PreventDelete BEFORE DELETE ON IksChecks
BEGIN
    SELECT RAISE(ABORT, 'Kontrollprüfungen bleiben als Historie erhalten.');
END;
