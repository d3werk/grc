CREATE TABLE Documents (
    Id TEXT PRIMARY KEY NOT NULL,
    Title TEXT NOT NULL,
    TopicId TEXT NOT NULL REFERENCES Topics(Id) ON DELETE RESTRICT,
    Scope TEXT NOT NULL,
    Category INTEGER NOT NULL CHECK (Category BETWEEN 0 AND 5),
    Owner TEXT NOT NULL,
    Content TEXT NOT NULL,
    Status INTEGER NOT NULL CHECK (Status BETWEEN 0 AND 4),
    Version INTEGER NOT NULL CHECK (Version >= 1),
    Revision INTEGER NOT NULL CHECK (Revision >= 1),
    PublishedVersion INTEGER CHECK (PublishedVersion BETWEEN 1 AND Version),
    UpdatedUtc TEXT NOT NULL,
    ReviewedBy TEXT NOT NULL DEFAULT '',
    ReviewedUtc TEXT NOT NULL DEFAULT '',
    ReviewNote TEXT NOT NULL DEFAULT '',
    CHECK (Status NOT IN (3,4) OR (PublishedVersion IS NOT NULL AND PublishedVersion = Version))
);

CREATE INDEX IX_Documents_Topic_Scope ON Documents(TopicId, Scope);

CREATE TABLE PublishedDocumentVersions (
    DocumentId TEXT NOT NULL REFERENCES Documents(Id) ON DELETE RESTRICT,
    Version INTEGER NOT NULL CHECK (Version >= 1),
    Title TEXT NOT NULL,
    TopicId TEXT NOT NULL REFERENCES Topics(Id) ON DELETE RESTRICT,
    Scope TEXT NOT NULL,
    Category INTEGER NOT NULL CHECK (Category BETWEEN 0 AND 5),
    Owner TEXT NOT NULL,
    Content TEXT NOT NULL,
    ReviewedBy TEXT NOT NULL,
    ReviewedUtc TEXT NOT NULL,
    ReviewNote TEXT NOT NULL,
    PublishedBy TEXT NOT NULL,
    PublishedUtc TEXT NOT NULL,
    PublicationNote TEXT NOT NULL,
    PRIMARY KEY (DocumentId, Version)
);

CREATE TRIGGER PublishedDocumentVersions_PreventUpdate BEFORE UPDATE ON PublishedDocumentVersions
BEGIN
    SELECT RAISE(ABORT, 'Veröffentlichte Dokumentfassungen bleiben unverändert.');
END;

CREATE TRIGGER PublishedDocumentVersions_PreventDelete BEFORE DELETE ON PublishedDocumentVersions
BEGIN
    SELECT RAISE(ABORT, 'Veröffentlichte Dokumentfassungen bleiben erhalten.');
END;

CREATE TABLE DocumentLinks (
    Id TEXT PRIMARY KEY NOT NULL,
    DocumentId TEXT NOT NULL REFERENCES Documents(Id) ON DELETE RESTRICT,
    Kind INTEGER NOT NULL CHECK (Kind IN (0,1)),
    RecordId TEXT NOT NULL REFERENCES Records(Id) ON DELETE RESTRICT,
    Reason TEXT NOT NULL,
    UNIQUE (DocumentId, RecordId)
);

CREATE INDEX IX_DocumentLinks_RecordId ON DocumentLinks(RecordId);

INSERT INTO Metadata(Key, Value) VALUES('MinAppVersion', '0.9.0')
ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
