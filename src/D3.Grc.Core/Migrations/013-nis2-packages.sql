CREATE TABLE Nis2Materializations (
    Scope TEXT NOT NULL CHECK(length(Scope) BETWEEN 1 AND 1000 AND Scope=trim(Scope)),
    PackageKey TEXT NOT NULL CHECK(PackageKey IN ('21a','21b','21c','21d','21e','21f','21g','21h','21i','21j','23')),
    ItemKey TEXT NOT NULL CHECK(length(ItemKey) BETWEEN 1 AND 120),
    RecordId TEXT NOT NULL REFERENCES Records(Id) ON DELETE CASCADE,
    PRIMARY KEY (Scope, PackageKey, ItemKey)
);

CREATE INDEX Nis2MaterializationsRecord ON Nis2Materializations(RecordId);
