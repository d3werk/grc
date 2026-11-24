INSERT INTO catalog_link_types (kind, from_kind, to_kind, label) VALUES
    ('audit_subject_org_unit', 'audit_subject', 'org_unit', 'Prüfeinheit umfasst Organisationseinheit'),
    ('audit_subject_process', 'audit_subject', 'business_process', 'Prüfeinheit umfasst Geschäftsprozess'),
    ('audit_subject_it_service', 'audit_subject', 'it_service', 'Prüfeinheit umfasst IT-Dienst'),
    ('audit_subject_risk', 'audit_subject', 'risk', 'Risiko beeinflusst Prüfpriorität'),
    ('audit_engagement_subject', 'audit_engagement', 'audit_subject', 'Prüfauftrag umfasst Prüfeinheit'),
    ('audit_engagement_requirement', 'audit_engagement', 'requirement', 'Anforderung begründet Prüfkriterien'),
    ('audit_test_engagement', 'audit_test', 'audit_engagement', 'Prüfschritt gehört zum Prüfauftrag'),
    ('audit_test_control', 'audit_test', 'control', 'Prüfschritt bewertet Control'),
    ('audit_finding_engagement', 'audit_finding', 'audit_engagement', 'Feststellung gehört zum Prüfauftrag'),
    ('audit_finding_test', 'audit_finding', 'audit_test', 'Feststellung ergibt sich aus Prüfschritt'),
    ('audit_finding_measure', 'audit_finding', 'measure', 'Maßnahme bearbeitet Feststellung'),
    ('audit_finding_risk', 'audit_finding', 'risk', 'Feststellung betrifft Risiko'),
    ('audit_finding_requirement', 'audit_finding', 'requirement', 'Feststellung betrifft Anforderung');
