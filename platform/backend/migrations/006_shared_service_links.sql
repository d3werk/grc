INSERT INTO catalog_link_types (kind, from_kind, to_kind, label) VALUES
    ('processing_it_service', 'processing_activity', 'it_service', 'Verarbeitung nutzt IT-Dienst'),
    ('control_it_service', 'control', 'it_service', 'Control schützt IT-Dienst'),
    ('risk_processing_activity', 'risk', 'processing_activity', 'Risiko betrifft Verarbeitung');
