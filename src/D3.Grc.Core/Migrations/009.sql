CREATE TABLE RiskAssessments (
    Sequence INTEGER PRIMARY KEY CHECK (Sequence > 0),
    Id TEXT NOT NULL UNIQUE,
    RiskId TEXT NOT NULL REFERENCES Records(Id) ON DELETE RESTRICT,
    AssessedOn TEXT NOT NULL CHECK (length(AssessedOn) = 10 AND date(AssessedOn, '+0 days') IS NOT NULL AND date(AssessedOn, '+0 days') = AssessedOn),
    InherentLikelihood INTEGER NOT NULL CHECK (typeof(InherentLikelihood) = 'integer' AND InherentLikelihood BETWEEN 1 AND 5),
    InherentImpact INTEGER NOT NULL CHECK (typeof(InherentImpact) = 'integer' AND InherentImpact BETWEEN 1 AND 5),
    ResidualLikelihood INTEGER,
    ResidualImpact INTEGER,
    Response INTEGER NOT NULL CHECK (typeof(Response) = 'integer' AND Response BETWEEN 0 AND 4),
    Rationale TEXT NOT NULL CHECK (length(trim(Rationale)) BETWEEN 1 AND 30000),
    Actor TEXT NOT NULL CHECK (length(trim(Actor)) > 0),
    RiskVersion INTEGER NOT NULL CHECK (typeof(RiskVersion) = 'integer' AND RiskVersion >= 1),
    RecordedUtc TEXT NOT NULL,
    CHECK ((ResidualLikelihood IS NULL AND ResidualImpact IS NULL) OR
           (typeof(ResidualLikelihood) = 'integer' AND ResidualLikelihood BETWEEN 1 AND 5 AND
            typeof(ResidualImpact) = 'integer' AND ResidualImpact BETWEEN 1 AND 5))
);

CREATE INDEX IX_RiskAssessments_RiskDateSequence ON RiskAssessments(RiskId, AssessedOn DESC, Sequence DESC);

CREATE TRIGGER RiskAssessments_ValidateRisk BEFORE INSERT ON RiskAssessments
WHEN NOT EXISTS (SELECT 1 FROM Records WHERE Id=NEW.RiskId AND Kind='Risk' AND Version=NEW.RiskVersion)
BEGIN
    SELECT RAISE(ABORT, 'Eine gültige aktuelle Risikoversion ist erforderlich.');
END;

CREATE TRIGGER RiskAssessments_PreventUpdate BEFORE UPDATE ON RiskAssessments
BEGIN
    SELECT RAISE(ABORT, 'Risikobewertungen bleiben unverändert erhalten.');
END;

CREATE TRIGGER RiskAssessments_PreventDelete BEFORE DELETE ON RiskAssessments
BEGIN
    SELECT RAISE(ABORT, 'Risikobewertungen bleiben in der Historie erhalten.');
END;

CREATE TRIGGER RiskAssessments_PreventReplace BEFORE INSERT ON RiskAssessments
WHEN EXISTS (SELECT 1 FROM RiskAssessments WHERE Id=NEW.Id OR Sequence=NEW.Sequence)
BEGIN
    SELECT RAISE(ABORT, 'Vorhandene Risikobewertungen bleiben unverändert.');
END;

INSERT INTO Metadata(Key, Value) VALUES('MinAppVersion', '0.10.0')
ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
