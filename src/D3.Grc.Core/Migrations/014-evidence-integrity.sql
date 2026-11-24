CREATE TRIGGER Evidence_PreventReplacement BEFORE INSERT ON Evidence
WHEN EXISTS (SELECT 1 FROM Evidence WHERE Id = NEW.Id)
BEGIN
    SELECT RAISE(ABORT, 'Gespeicherte Nachweise dürfen nicht ersetzt werden.');
END;

CREATE TRIGGER Evidence_PreventUpdate BEFORE UPDATE ON Evidence
BEGIN
    SELECT RAISE(ABORT, 'Gespeicherte Nachweise dürfen nicht überschrieben werden.');
END;

CREATE TRIGGER Evidence_PreventDelete BEFORE DELETE ON Evidence
BEGIN
    SELECT RAISE(ABORT, 'Gespeicherte Nachweise bleiben zur Prüfung erhalten.');
END;

CREATE TRIGGER EvidenceLinks_PreventReplacement BEFORE INSERT ON EvidenceLinks
WHEN EXISTS (SELECT 1 FROM EvidenceLinks WHERE EvidenceId = NEW.EvidenceId AND ControlId = NEW.ControlId)
BEGIN
    SELECT RAISE(ABORT, 'Diese Nachweiszuordnung besteht bereits.');
END;

CREATE TRIGGER EvidenceLinks_PreventUpdate BEFORE UPDATE ON EvidenceLinks
BEGIN
    SELECT RAISE(ABORT, 'Nachweiszuordnungen bleiben zur Prüfung erhalten.');
END;

CREATE TRIGGER EvidenceLinks_PreventDelete BEFORE DELETE ON EvidenceLinks
BEGIN
    SELECT RAISE(ABORT, 'Nachweiszuordnungen bleiben zur Prüfung erhalten.');
END;

CREATE TABLE AuditTestSnapshots (
    Id INTEGER PRIMARY KEY,
    TestId TEXT NOT NULL REFERENCES AuditTests(Id) ON DELETE RESTRICT,
    AuditId TEXT NOT NULL REFERENCES Records(Id) ON DELETE RESTRICT,
    AuditScope TEXT NOT NULL,
    ControlId TEXT NOT NULL REFERENCES Records(Id) ON DELETE RESTRICT,
    ControlCode TEXT NOT NULL,
    ControlTitle TEXT NOT NULL,
    ControlVersion INTEGER,
    SourceId TEXT NOT NULL REFERENCES Sources(Id) ON DELETE RESTRICT,
    CriteriaJson TEXT,
    EvidenceId TEXT REFERENCES Evidence(Id) ON DELETE RESTRICT,
    EvidenceSha256 TEXT,
    Outcome TEXT NOT NULL,
    Notes TEXT NOT NULL,
    TestedUtc TEXT NOT NULL,
    IsLegacy INTEGER NOT NULL CHECK (IsLegacy IN (0, 1)),
    CHECK ((EvidenceId IS NULL) = (EvidenceSha256 IS NULL))
);

CREATE INDEX IX_AuditTestSnapshots_Audit ON AuditTestSnapshots(AuditId, Id DESC);
CREATE INDEX IX_AuditTestSnapshots_Evidence ON AuditTestSnapshots(EvidenceId) WHERE EvidenceId IS NOT NULL;

CREATE VIEW CurrentAuditTestSnapshot AS
SELECT test.Id AS TestId, test.AuditId, audit.Scope AS AuditScope, test.ControlId, control.Code AS ControlCode,
       control.Title AS ControlTitle, criteria.ControlVersion, test.SourceId, criteria.CriterionJson,
       test.EvidenceId, evidence.Hash AS EvidenceSha256, test.Outcome, test.Notes, test.TestedUtc
FROM AuditTests test
JOIN Records audit ON audit.Id=test.AuditId
JOIN Records control ON control.Id=test.ControlId
LEFT JOIN AuditCriteria criteria ON criteria.AuditId=test.AuditId AND criteria.ControlId=test.ControlId
LEFT JOIN Evidence evidence ON evidence.Id=test.EvidenceId;

INSERT INTO AuditTestSnapshots (TestId, AuditId, AuditScope, ControlId, ControlCode, ControlTitle, ControlVersion,
    SourceId, CriteriaJson, EvidenceId, EvidenceSha256, Outcome, Notes, TestedUtc, IsLegacy)
SELECT snapshot.*, 1 FROM CurrentAuditTestSnapshot snapshot ORDER BY snapshot.TestedUtc, snapshot.TestId;

CREATE TRIGGER AuditCriteria_SnapshotInsert AFTER INSERT ON AuditCriteria
BEGIN
    INSERT INTO AuditTestSnapshots (TestId, AuditId, AuditScope, ControlId, ControlCode, ControlTitle, ControlVersion,
        SourceId, CriteriaJson, EvidenceId, EvidenceSha256, Outcome, Notes, TestedUtc, IsLegacy)
    SELECT snapshot.*, 0 FROM CurrentAuditTestSnapshot snapshot
    WHERE snapshot.AuditId = NEW.AuditId AND snapshot.ControlId = NEW.ControlId;
END;

CREATE TRIGGER AuditCriteria_SnapshotUpdate AFTER UPDATE ON AuditCriteria
BEGIN
    INSERT INTO AuditTestSnapshots (TestId, AuditId, AuditScope, ControlId, ControlCode, ControlTitle, ControlVersion,
        SourceId, CriteriaJson, EvidenceId, EvidenceSha256, Outcome, Notes, TestedUtc, IsLegacy)
    SELECT snapshot.*, 0 FROM CurrentAuditTestSnapshot snapshot
    WHERE snapshot.AuditId = NEW.AuditId AND snapshot.ControlId = NEW.ControlId;
END;

CREATE TRIGGER AuditTestSnapshots_PreventReplacement BEFORE INSERT ON AuditTestSnapshots
WHEN EXISTS (SELECT 1 FROM AuditTestSnapshots WHERE Id = NEW.Id)
BEGIN
    SELECT RAISE(ABORT, 'Prüfstände dürfen nicht ersetzt werden.');
END;

CREATE TRIGGER AuditTestSnapshots_PreventUpdate BEFORE UPDATE ON AuditTestSnapshots
BEGIN
    SELECT RAISE(ABORT, 'Prüfstände dürfen nicht überschrieben werden.');
END;

CREATE TRIGGER AuditTestSnapshots_PreventDelete BEFORE DELETE ON AuditTestSnapshots
BEGIN
    SELECT RAISE(ABORT, 'Prüfstände bleiben zur Nachverfolgung erhalten.');
END;
