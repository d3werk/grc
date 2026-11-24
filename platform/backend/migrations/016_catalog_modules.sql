ALTER TABLE catalog_records DROP CONSTRAINT catalog_records_kind_check;
ALTER TABLE catalog_records ADD CONSTRAINT catalog_records_kind_check CHECK (kind IN (
    'org_unit', 'asset', 'it_service', 'business_process', 'risk', 'control', 'measure',
    'framework_version', 'requirement', 'processing_activity', 'supplier', 'dpia',
    'audit_subject', 'audit_engagement', 'audit_test', 'audit_finding',
    'resilience_service', 'resilience_scenario', 'continuity_bia', 'continuity_plan', 'continuity_exercise',
    'ai_system', 'contract'
));

ALTER TABLE evidence_links DROP CONSTRAINT evidence_links_target_kind;
ALTER TABLE evidence_links ADD CONSTRAINT evidence_links_target_kind CHECK (record_kind IN (
    'control', 'measure', 'audit_engagement', 'audit_test', 'audit_finding',
    'resilience_scenario', 'continuity_plan', 'continuity_exercise', 'ai_system', 'contract'
));
