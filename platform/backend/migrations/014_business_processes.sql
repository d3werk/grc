INSERT INTO catalog_link_types (kind, from_kind, to_kind, label) VALUES
    ('process_parent', 'business_process', 'business_process', 'Übergeordneter Geschäftsprozess'),
    ('process_dependency', 'business_process', 'business_process', 'Abhängigkeit von einem Geschäftsprozess'),
    ('process_supplier', 'business_process', 'supplier', 'Geschäftsprozess nutzt Lieferantenleistung'),
    ('process_control', 'business_process', 'control', 'Control schützt Geschäftsprozess');

ALTER TABLE catalog_links ADD CONSTRAINT process_parent_same_scope
    CHECK (kind <> 'process_parent' OR from_scope = to_scope);

CREATE UNIQUE INDEX catalog_process_single_parent ON catalog_links(tenant_id, from_id)
    WHERE kind = 'process_parent';
