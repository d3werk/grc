ALTER TABLE catalog_records ADD COLUMN rich_text jsonb NOT NULL DEFAULT '{}'::jsonb
    CHECK (jsonb_typeof(rich_text) = 'object');
ALTER TABLE catalog_links ADD COLUMN rationale_rich_text jsonb
    CHECK (jsonb_typeof(rationale_rich_text) = 'object');
ALTER TABLE risk_assessments ADD COLUMN rationale_rich_text jsonb
    CHECK (jsonb_typeof(rationale_rich_text) = 'object');

GRANT UPDATE (rich_text) ON catalog_records TO grc_runtime;
GRANT UPDATE (rationale_rich_text) ON catalog_links TO grc_runtime;
