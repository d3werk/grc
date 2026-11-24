CREATE TABLE DesktopModuleFields (
    RecordId TEXT PRIMARY KEY REFERENCES Records(Id) ON DELETE CASCADE,
    Data TEXT NOT NULL CHECK(json_valid(Data) AND json_type(Data)='object')
);

INSERT OR IGNORE INTO Topics(Id,Title) VALUES ('PROCESS','Prozessmanagement');
INSERT OR IGNORE INTO TopicSources(TopicId,SourceId) VALUES ('PROCESS','INTERNAL');
