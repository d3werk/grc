INSERT INTO catalog_link_types (kind, from_kind, to_kind, label) VALUES
    ('ai_system_process', 'ai_system', 'business_process', 'KI-System unterstützt Geschäftsprozess'),
    ('ai_system_it_service', 'ai_system', 'it_service', 'KI-System nutzt IT-Dienst'),
    ('ai_system_supplier', 'ai_system', 'supplier', 'Lieferant stellt KI-System oder Modell bereit'),
    ('ai_system_risk', 'ai_system', 'risk', 'KI-System ist Gegenstand der Risikobewertung'),
    ('ai_system_control', 'ai_system', 'control', 'Control schützt den KI-Einsatz'),
    ('ai_system_requirement', 'ai_system', 'requirement', 'Anforderung gilt für KI-System'),
    ('ai_system_measure', 'ai_system', 'measure', 'Maßnahme sichert den KI-Einsatz'),
    ('ai_system_processing', 'ai_system', 'processing_activity', 'KI-System verarbeitet personenbezogene Daten'),
    ('ai_system_dpia', 'ai_system', 'dpia', 'Folgenabschätzung berücksichtigt KI-System');
