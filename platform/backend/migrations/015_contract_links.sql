INSERT INTO catalog_link_types (kind, from_kind, to_kind, label) VALUES
    ('contract_supplier', 'contract', 'supplier', 'Vertrag gilt mit Lieferant'),
    ('contract_service', 'contract', 'it_service', 'Vertrag regelt IT-Dienst'),
    ('contract_process', 'contract', 'business_process', 'Vertrag betrifft Geschäftsprozess'),
    ('contract_risk', 'contract', 'risk', 'Vertrag betrifft Risiko'),
    ('contract_control', 'contract', 'control', 'Control überwacht Vertragspflicht'),
    ('contract_measure', 'contract', 'measure', 'Maßnahme bearbeitet Vertragspflicht');
