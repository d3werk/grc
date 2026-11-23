ALTER TABLE evidence_links ALTER COLUMN record_kind DROP EXPRESSION;
ALTER TABLE evidence_links ALTER COLUMN record_kind SET DEFAULT 'control';
ALTER TABLE evidence_links ADD CONSTRAINT evidence_links_target_kind
    CHECK (record_kind IN ('control', 'measure'));
