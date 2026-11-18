CREATE TABLE Topics(Id TEXT PRIMARY KEY,Title TEXT NOT NULL);
CREATE TABLE TopicSources(TopicId TEXT NOT NULL REFERENCES Topics(Id),SourceId TEXT NOT NULL REFERENCES Sources(Id),PRIMARY KEY(TopicId,SourceId));
CREATE TABLE RecordTopics(RecordId TEXT NOT NULL REFERENCES Records(Id) ON DELETE CASCADE,TopicId TEXT NOT NULL REFERENCES Topics(Id),PRIMARY KEY(RecordId,TopicId));
CREATE TABLE SourceVersions(Id TEXT PRIMARY KEY,SourceId TEXT NOT NULL REFERENCES Sources(Id),Label TEXT NOT NULL,RetrievedUtc TEXT NOT NULL DEFAULT '',AcceptedUtc TEXT NOT NULL DEFAULT '',Hash TEXT NOT NULL DEFAULT '',ReviewNote TEXT NOT NULL DEFAULT 'Fachliche Prüfung offen');
CREATE TABLE RecordSources(RecordId TEXT NOT NULL REFERENCES Records(Id) ON DELETE CASCADE,SourceId TEXT NOT NULL REFERENCES Sources(Id),Locator TEXT NOT NULL,Role TEXT NOT NULL DEFAULT 'Primär',PRIMARY KEY(RecordId,SourceId));
CREATE TABLE MappingAssessments(LinkId TEXT PRIMARY KEY REFERENCES Links(Id) ON DELETE CASCADE,Coverage TEXT NOT NULL DEFAULT 'Teilweise',Scope TEXT NOT NULL DEFAULT '',Approval TEXT NOT NULL DEFAULT 'Entwurf');
CREATE TABLE Applicability(RecordId TEXT PRIMARY KEY REFERENCES Records(Id) ON DELETE CASCADE,Decision TEXT NOT NULL,Reason TEXT NOT NULL,Reviewer TEXT NOT NULL,ReviewedUtc TEXT NOT NULL);
CREATE TABLE AuditCriteria(AuditId TEXT NOT NULL REFERENCES Records(Id),ControlId TEXT NOT NULL REFERENCES Records(Id),ControlVersion INTEGER NOT NULL,CriterionJson TEXT NOT NULL,PRIMARY KEY(AuditId,ControlId));
CREATE TABLE ExportJobs(Id TEXT PRIMARY KEY,CreatedUtc TEXT NOT NULL,Actor TEXT NOT NULL,TopicId TEXT NOT NULL,Scope TEXT NOT NULL,Format TEXT NOT NULL,FileName TEXT NOT NULL,State TEXT NOT NULL);
INSERT INTO Topics VALUES('PRIVACY','Datenschutz');
INSERT INTO Topics VALUES('ISMS','Informationssicherheit');
INSERT INTO Topics VALUES('NIS2','NIS2');
INSERT INTO Topics VALUES('AI','KI-Governance');
INSERT INTO Topics VALUES('CLOUD','Cloud Security');
INSERT INTO Topics VALUES('BCM','Business Continuity');
INSERT INTO Topics VALUES('OSS','Open Source');
INSERT INTO Topics VALUES('DORA','DORA');
INSERT INTO Topics VALUES('REVIEW','Zuordnung prüfen');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('INTERNAL','Interner D3-Arbeitsstandard','D3 GRC','','Organisation','Eigene Arbeitshilfen, keine externe Rechtsquelle.','D3-BASE');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('GDPR','DSGVO','Europäische Union','https://eur-lex.europa.eu/eli/reg/2016/679/oj','Datenschutz','Aktuelle konsolidierte Fassung und Anwendbarkeit fachlich prüfen.','2016/679');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('BDSG','BDSG','Gesetze im Internet','https://www.gesetze-im-internet.de/bdsg_2018/','Datenschutz','Ergänzende nationale Regelungen; konkrete Vorschrift prüfen.','BDSG');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('NIS2','NIS2-Richtlinie','Europäische Union','https://eur-lex.europa.eu/eli/dir/2022/2555/oj','Cybersicherheit','EU-Richtlinie und nationale Umsetzung getrennt behandeln.','2022/2555');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('BSIG','BSI-Gesetz','Gesetze im Internet','https://www.gesetze-im-internet.de/bsig_2025/','Cybersicherheit','Betroffenheit und aktuelle nationale Fassung prüfen.','BSIG');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('ISO27001','ISO/IEC 27001:2022','ISO','https://www.iso.org/standard/27001','ISMS','Öffentliche Referenz, keine lizenzierten Normtexte.','ISO 27001:2022');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('BSIGS','IT-Grundschutz-Kompendium','BSI','https://www.bsi.bund.de/DE/Themen/Unternehmen-und-Organisationen/Standards-und-Zertifizierung/IT-Grundschutz/IT-Grundschutz-Kompendium/it-grundschutz-kompendium_node.html','ISMS','Konkrete Ausgabe und Bausteine auswählen; keine Volltextkopie.','IT-Grundschutz');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('BSI2004','BSI-Standard 200-4','BSI','https://www.bsi.bund.de/DE/Themen/Unternehmen-und-Organisationen/Standards-und-Zertifizierung/IT-Grundschutz/BSI-Standards/BSI-Standard-200-4-Business-Continuity-Management/bsi-standard-200-4_Business_Continuity_Management_node.html','BCM','BCM-Referenz, Scope und Ausgabe dokumentieren.','BSI 200-4');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('AIACT','KI-Verordnung','Europäische Union','https://eur-lex.europa.eu/eli/reg/2024/1689/oj','KI','Rollen, Risikoeinordnung und zeitliche Anwendbarkeit prüfen.','2024/1689');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('MSSEC','Microsoft Cloud Security','Microsoft','https://learn.microsoft.com/en-us/security/','Cloud','Technische Herstellerdokumentation, keine Rechtsquelle.','Microsoft Learn');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('SPDX','SPDX-Spezifikationen','Linux Foundation','https://spdx.dev/use/specifications/','OSS','Version und Nutzungsmodell dokumentieren.','SPDX');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('CYCLONEDX','CycloneDX','OWASP','https://cyclonedx.org/specification/overview/','OSS','BOM-Version und Vollständigkeit prüfen.','CycloneDX');
INSERT OR IGNORE INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference) VALUES('DORA','DORA','Europäische Union','https://eur-lex.europa.eu/eli/reg/2022/2554/oj','Resilienz','Anwendungsbereich und technische Standards prüfen.','2022/2554');
INSERT OR IGNORE INTO TopicSources VALUES('PRIVACY','INTERNAL');
INSERT OR IGNORE INTO TopicSources VALUES('ISMS','INTERNAL');
INSERT OR IGNORE INTO TopicSources VALUES('NIS2','INTERNAL');
INSERT OR IGNORE INTO TopicSources VALUES('AI','INTERNAL');
INSERT OR IGNORE INTO TopicSources VALUES('CLOUD','INTERNAL');
INSERT OR IGNORE INTO TopicSources VALUES('BCM','INTERNAL');
INSERT OR IGNORE INTO TopicSources VALUES('OSS','INTERNAL');
INSERT OR IGNORE INTO TopicSources VALUES('DORA','INTERNAL');
INSERT OR IGNORE INTO TopicSources VALUES('PRIVACY','GDPR');
INSERT OR IGNORE INTO TopicSources VALUES('PRIVACY','BDSG');
INSERT OR IGNORE INTO TopicSources VALUES('NIS2','NIS2');
INSERT OR IGNORE INTO TopicSources VALUES('NIS2','BSIG');
INSERT OR IGNORE INTO TopicSources VALUES('ISMS','ISO27001');
INSERT OR IGNORE INTO TopicSources VALUES('ISMS','BSIGS');
INSERT OR IGNORE INTO TopicSources VALUES('BCM','BSI2004');
INSERT OR IGNORE INTO TopicSources VALUES('AI','AIACT');
INSERT OR IGNORE INTO TopicSources VALUES('CLOUD','MSSEC');
INSERT OR IGNORE INTO TopicSources VALUES('OSS','SPDX');
INSERT OR IGNORE INTO TopicSources VALUES('OSS','CYCLONEDX');
INSERT OR IGNORE INTO TopicSources VALUES('DORA','DORA');
INSERT INTO RecordTopics SELECT r.Id,t.TopicId FROM Records r JOIN TopicSources t ON t.SourceId=r.SourceId WHERE r.SourceId<>'INTERNAL';
INSERT OR IGNORE INTO RecordTopics SELECT Id,'REVIEW' FROM Records WHERE Id NOT IN(SELECT RecordId FROM RecordTopics);
INSERT INTO RecordSources SELECT Id,SourceId,Locator,'Primär' FROM Records;
INSERT INTO SourceVersions SELECT 'initial-'||Id,Id,Reference,'','','','Fachliche Prüfung offen' FROM Sources;
INSERT INTO MappingAssessments(LinkId) SELECT Id FROM Links;
INSERT OR REPLACE INTO Metadata VALUES('MinAppVersion','0.6.0');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-GDPR','Requirement','Datenschutzmaßnahmen risikobezogen festlegen · REQ','GDPR','Art. 32; aktuelle Fassung prüfen','Risiken für Rechte und Freiheiten bewerten. Angemessene technische und organisatorische Maßnahmen begründen und ihre Wirksamkeit prüfen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-GDPR','Requirement','V06-REQ-GDPR','Datenschutzmaßnahmen risikobezogen festlegen · REQ','GDPR','V06-REQ-GDPR','Art. 32; aktuelle Fassung prüfen','Risiken für Rechte und Freiheiten bewerten. Angemessene technische und organisatorische Maßnahmen begründen und ihre Wirksamkeit prüfen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-GDPR','PRIVACY');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-GDPR','GDPR','Art. 32; aktuelle Fassung prüfen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-GDPR','Control','Datenschutzmaßnahmen risikobezogen festlegen · CTRL','GDPR','Art. 32; aktuelle Fassung prüfen','Umsetzung: Risiken für Rechte und Freiheiten bewerten. Angemessene technische und organisatorische Maßnahmen begründen und ihre Wirksamkeit prüfen.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-GDPR','Control','V06-CTRL-GDPR','Datenschutzmaßnahmen risikobezogen festlegen · CTRL','GDPR','V06-CTRL-GDPR','Art. 32; aktuelle Fassung prüfen','Umsetzung: Risiken für Rechte und Freiheiten bewerten. Angemessene technische und organisatorische Maßnahmen begründen und ihre Wirksamkeit prüfen.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-GDPR','PRIVACY');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-GDPR','GDPR','Art. 32; aktuelle Fassung prüfen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-GDPR','Risk','Datenschutzmaßnahmen risikobezogen festlegen · RISK','GDPR','Art. 32; aktuelle Fassung prüfen','Szenario: Die vorgesehene Steuerung zu „Datenschutzmaßnahmen risikobezogen festlegen“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-GDPR','Audit','Datenschutzmaßnahmen risikobezogen festlegen · AUD','GDPR','Art. 32; aktuelle Fassung prüfen','Prüfziel: Umsetzung von „Datenschutzmaßnahmen risikobezogen festlegen“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-GDPR','Evidence','Datenschutzmaßnahmen risikobezogen festlegen · EV','GDPR','Art. 32; aktuelle Fassung prüfen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Datenschutzmaßnahmen risikobezogen festlegen“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-GDPR','AuditTest','Datenschutzmaßnahmen risikobezogen festlegen · TEST','GDPR','Art. 32; aktuelle Fassung prüfen','Prüfhandlung: Umsetzung von „Datenschutzmaßnahmen risikobezogen festlegen“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-GDPR','Finding','Datenschutzmaßnahmen risikobezogen festlegen · FIND','GDPR','Art. 32; aktuelle Fassung prüfen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-GDPR','Measure','Datenschutzmaßnahmen risikobezogen festlegen · MEAS','GDPR','Art. 32; aktuelle Fassung prüfen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-GDPR','V06-CTRL-GDPR','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-BDSG','Requirement','Nationale Datenschutzanforderungen prüfen · REQ','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Ergänzende nationale Vorschriften für den konkreten Verarbeitungskontext identifizieren. Anwendbarkeit, Ausnahmen und Verantwortlichkeiten dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-BDSG','Requirement','V06-REQ-BDSG','Nationale Datenschutzanforderungen prüfen · REQ','BDSG','V06-REQ-BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Ergänzende nationale Vorschriften für den konkreten Verarbeitungskontext identifizieren. Anwendbarkeit, Ausnahmen und Verantwortlichkeiten dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-BDSG','PRIVACY');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-BDSG','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-BDSG','Control','Nationale Datenschutzanforderungen prüfen · CTRL','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Umsetzung: Ergänzende nationale Vorschriften für den konkreten Verarbeitungskontext identifizieren. Anwendbarkeit, Ausnahmen und Verantwortlichkeiten dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-BDSG','Control','V06-CTRL-BDSG','Nationale Datenschutzanforderungen prüfen · CTRL','BDSG','V06-CTRL-BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Umsetzung: Ergänzende nationale Vorschriften für den konkreten Verarbeitungskontext identifizieren. Anwendbarkeit, Ausnahmen und Verantwortlichkeiten dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-BDSG','PRIVACY');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-BDSG','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-BDSG','Risk','Nationale Datenschutzanforderungen prüfen · RISK','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Szenario: Die vorgesehene Steuerung zu „Nationale Datenschutzanforderungen prüfen“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-BDSG','Audit','Nationale Datenschutzanforderungen prüfen · AUD','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Prüfziel: Umsetzung von „Nationale Datenschutzanforderungen prüfen“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-BDSG','Evidence','Nationale Datenschutzanforderungen prüfen · EV','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Nationale Datenschutzanforderungen prüfen“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-BDSG','AuditTest','Nationale Datenschutzanforderungen prüfen · TEST','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Prüfhandlung: Umsetzung von „Nationale Datenschutzanforderungen prüfen“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-BDSG','Finding','Nationale Datenschutzanforderungen prüfen · FIND','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-BDSG','Measure','Nationale Datenschutzanforderungen prüfen · MEAS','BDSG','Konkrete BDSG-Vorschrift fallbezogen ergänzen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-BDSG','V06-CTRL-BDSG','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-NIS2','Requirement','Cyberrisikomanagement festlegen · REQ','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','EU-Vorgaben als Ausgangspunkt verwenden und nationale Umsetzung getrennt prüfen. Risikomanagement, Vorfallbehandlung und Verantwortlichkeiten dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-NIS2','Requirement','V06-REQ-NIS2','Cyberrisikomanagement festlegen · REQ','NIS2','V06-REQ-NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','EU-Vorgaben als Ausgangspunkt verwenden und nationale Umsetzung getrennt prüfen. Risikomanagement, Vorfallbehandlung und Verantwortlichkeiten dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-NIS2','NIS2');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-NIS2','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-NIS2','Control','Cyberrisikomanagement festlegen · CTRL','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Umsetzung: EU-Vorgaben als Ausgangspunkt verwenden und nationale Umsetzung getrennt prüfen. Risikomanagement, Vorfallbehandlung und Verantwortlichkeiten dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-NIS2','Control','V06-CTRL-NIS2','Cyberrisikomanagement festlegen · CTRL','NIS2','V06-CTRL-NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Umsetzung: EU-Vorgaben als Ausgangspunkt verwenden und nationale Umsetzung getrennt prüfen. Risikomanagement, Vorfallbehandlung und Verantwortlichkeiten dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-NIS2','NIS2');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-NIS2','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-NIS2','Risk','Cyberrisikomanagement festlegen · RISK','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Szenario: Die vorgesehene Steuerung zu „Cyberrisikomanagement festlegen“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-NIS2','Audit','Cyberrisikomanagement festlegen · AUD','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Prüfziel: Umsetzung von „Cyberrisikomanagement festlegen“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-NIS2','Evidence','Cyberrisikomanagement festlegen · EV','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Cyberrisikomanagement festlegen“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-NIS2','AuditTest','Cyberrisikomanagement festlegen · TEST','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Prüfhandlung: Umsetzung von „Cyberrisikomanagement festlegen“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-NIS2','Finding','Cyberrisikomanagement festlegen · FIND','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-NIS2','Measure','Cyberrisikomanagement festlegen · MEAS','NIS2','Art. 20/21/23; nationale Umsetzung gesondert prüfen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-NIS2','V06-CTRL-NIS2','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-BSIG','Requirement','Nationale Betroffenheit dokumentieren · REQ','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Einrichtungsart, Sektor und relevante Kriterien anhand der aktuellen nationalen Fassung prüfen. Entscheidung, Prüfer und Wiedervorlage dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-BSIG','Requirement','V06-REQ-BSIG','Nationale Betroffenheit dokumentieren · REQ','BSIG','V06-REQ-BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Einrichtungsart, Sektor und relevante Kriterien anhand der aktuellen nationalen Fassung prüfen. Entscheidung, Prüfer und Wiedervorlage dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-BSIG','NIS2');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-BSIG','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-BSIG','Control','Nationale Betroffenheit dokumentieren · CTRL','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Umsetzung: Einrichtungsart, Sektor und relevante Kriterien anhand der aktuellen nationalen Fassung prüfen. Entscheidung, Prüfer und Wiedervorlage dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-BSIG','Control','V06-CTRL-BSIG','Nationale Betroffenheit dokumentieren · CTRL','BSIG','V06-CTRL-BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Umsetzung: Einrichtungsart, Sektor und relevante Kriterien anhand der aktuellen nationalen Fassung prüfen. Entscheidung, Prüfer und Wiedervorlage dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-BSIG','NIS2');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-BSIG','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-BSIG','Risk','Nationale Betroffenheit dokumentieren · RISK','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Szenario: Die vorgesehene Steuerung zu „Nationale Betroffenheit dokumentieren“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-BSIG','Audit','Nationale Betroffenheit dokumentieren · AUD','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Prüfziel: Umsetzung von „Nationale Betroffenheit dokumentieren“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-BSIG','Evidence','Nationale Betroffenheit dokumentieren · EV','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Nationale Betroffenheit dokumentieren“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-BSIG','AuditTest','Nationale Betroffenheit dokumentieren · TEST','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Prüfhandlung: Umsetzung von „Nationale Betroffenheit dokumentieren“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-BSIG','Finding','Nationale Betroffenheit dokumentieren · FIND','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-BSIG','Measure','Nationale Betroffenheit dokumentieren · MEAS','BSIG','Aktuelle BSIG-Fassung; konkrete Vorschrift ergänzen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-BSIG','V06-CTRL-BSIG','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-ISO27001','Requirement','Informationssicherheitsrisiken behandeln · REQ','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Bewertungsmethode, Risikoeigentümer und Behandlungsentscheidungen festlegen. Verbleibende Risiken und Freigaben nachvollziehbar dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-ISO27001','Requirement','V06-REQ-ISO27001','Informationssicherheitsrisiken behandeln · REQ','ISO27001','V06-REQ-ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Bewertungsmethode, Risikoeigentümer und Behandlungsentscheidungen festlegen. Verbleibende Risiken und Freigaben nachvollziehbar dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-ISO27001','ISMS');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-ISO27001','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-ISO27001','Control','Informationssicherheitsrisiken behandeln · CTRL','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Umsetzung: Bewertungsmethode, Risikoeigentümer und Behandlungsentscheidungen festlegen. Verbleibende Risiken und Freigaben nachvollziehbar dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-ISO27001','Control','V06-CTRL-ISO27001','Informationssicherheitsrisiken behandeln · CTRL','ISO27001','V06-CTRL-ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Umsetzung: Bewertungsmethode, Risikoeigentümer und Behandlungsentscheidungen festlegen. Verbleibende Risiken und Freigaben nachvollziehbar dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-ISO27001','ISMS');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-ISO27001','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-ISO27001','Risk','Informationssicherheitsrisiken behandeln · RISK','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Szenario: Die vorgesehene Steuerung zu „Informationssicherheitsrisiken behandeln“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-ISO27001','Audit','Informationssicherheitsrisiken behandeln · AUD','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Prüfziel: Umsetzung von „Informationssicherheitsrisiken behandeln“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-ISO27001','Evidence','Informationssicherheitsrisiken behandeln · EV','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Informationssicherheitsrisiken behandeln“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-ISO27001','AuditTest','Informationssicherheitsrisiken behandeln · TEST','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Prüfhandlung: Umsetzung von „Informationssicherheitsrisiken behandeln“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-ISO27001','Finding','Informationssicherheitsrisiken behandeln · FIND','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-ISO27001','Measure','Informationssicherheitsrisiken behandeln · MEAS','ISO27001','Eigene Arbeitshilfe; Normfundstelle anhand lizenzierter Fassung prüfen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-ISO27001','V06-CTRL-ISO27001','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-BSIGS','Requirement','Passende Grundschutz-Bausteine modellieren · REQ','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Informationsverbund und Zielobjekte erfassen. Passende Bausteine und Ausgabe bestimmen, Anforderungen bewerten und Abweichungen dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-BSIGS','Requirement','V06-REQ-BSIGS','Passende Grundschutz-Bausteine modellieren · REQ','BSIGS','V06-REQ-BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Informationsverbund und Zielobjekte erfassen. Passende Bausteine und Ausgabe bestimmen, Anforderungen bewerten und Abweichungen dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-BSIGS','ISMS');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-BSIGS','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-BSIGS','Control','Passende Grundschutz-Bausteine modellieren · CTRL','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Umsetzung: Informationsverbund und Zielobjekte erfassen. Passende Bausteine und Ausgabe bestimmen, Anforderungen bewerten und Abweichungen dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-BSIGS','Control','V06-CTRL-BSIGS','Passende Grundschutz-Bausteine modellieren · CTRL','BSIGS','V06-CTRL-BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Umsetzung: Informationsverbund und Zielobjekte erfassen. Passende Bausteine und Ausgabe bestimmen, Anforderungen bewerten und Abweichungen dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-BSIGS','ISMS');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-BSIGS','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-BSIGS','Risk','Passende Grundschutz-Bausteine modellieren · RISK','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Szenario: Die vorgesehene Steuerung zu „Passende Grundschutz-Bausteine modellieren“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-BSIGS','Audit','Passende Grundschutz-Bausteine modellieren · AUD','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Prüfziel: Umsetzung von „Passende Grundschutz-Bausteine modellieren“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-BSIGS','Evidence','Passende Grundschutz-Bausteine modellieren · EV','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Passende Grundschutz-Bausteine modellieren“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-BSIGS','AuditTest','Passende Grundschutz-Bausteine modellieren · TEST','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Prüfhandlung: Umsetzung von „Passende Grundschutz-Bausteine modellieren“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-BSIGS','Finding','Passende Grundschutz-Bausteine modellieren · FIND','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-BSIGS','Measure','Passende Grundschutz-Bausteine modellieren · MEAS','BSIGS','Konkrete Ausgabe, Baustein und Anforderungs-ID ergänzen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-BSIGS','V06-CTRL-BSIGS','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-BSI2004','Requirement','Geschäftsfortführung und Wiederanlauf planen · REQ','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Kritische Tätigkeiten, Abhängigkeiten und Wiederanlaufziele erfassen. Fortführungs- und Wiederherstellungspläne erstellen und durch Übungen prüfen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-BSI2004','Requirement','V06-REQ-BSI2004','Geschäftsfortführung und Wiederanlauf planen · REQ','BSI2004','V06-REQ-BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Kritische Tätigkeiten, Abhängigkeiten und Wiederanlaufziele erfassen. Fortführungs- und Wiederherstellungspläne erstellen und durch Übungen prüfen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-BSI2004','BCM');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-BSI2004','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-BSI2004','Control','Geschäftsfortführung und Wiederanlauf planen · CTRL','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Umsetzung: Kritische Tätigkeiten, Abhängigkeiten und Wiederanlaufziele erfassen. Fortführungs- und Wiederherstellungspläne erstellen und durch Übungen prüfen.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-BSI2004','Control','V06-CTRL-BSI2004','Geschäftsfortführung und Wiederanlauf planen · CTRL','BSI2004','V06-CTRL-BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Umsetzung: Kritische Tätigkeiten, Abhängigkeiten und Wiederanlaufziele erfassen. Fortführungs- und Wiederherstellungspläne erstellen und durch Übungen prüfen.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-BSI2004','BCM');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-BSI2004','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-BSI2004','Risk','Geschäftsfortführung und Wiederanlauf planen · RISK','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Szenario: Die vorgesehene Steuerung zu „Geschäftsfortführung und Wiederanlauf planen“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-BSI2004','Audit','Geschäftsfortführung und Wiederanlauf planen · AUD','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Prüfziel: Umsetzung von „Geschäftsfortführung und Wiederanlauf planen“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-BSI2004','Evidence','Geschäftsfortführung und Wiederanlauf planen · EV','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Geschäftsfortführung und Wiederanlauf planen“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-BSI2004','AuditTest','Geschäftsfortführung und Wiederanlauf planen · TEST','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Prüfhandlung: Umsetzung von „Geschäftsfortführung und Wiederanlauf planen“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-BSI2004','Finding','Geschäftsfortführung und Wiederanlauf planen · FIND','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-BSI2004','Measure','Geschäftsfortführung und Wiederanlauf planen · MEAS','BSI2004','BSI-Standard 200-4; konkrete Fundstelle ergänzen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-BSI2004','V06-CTRL-BSI2004','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-MSSEC','Requirement','Privilegierte Cloudzugriffe absichern · REQ','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Administrationskonten, starke Authentifizierung, Notfallzugriff und regelmäßige Berechtigungsprüfung im konkreten Tenant festlegen. Keine automatische Aussage über eine vorhandene Konfiguration.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-MSSEC','Requirement','V06-REQ-MSSEC','Privilegierte Cloudzugriffe absichern · REQ','MSSEC','V06-REQ-MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Administrationskonten, starke Authentifizierung, Notfallzugriff und regelmäßige Berechtigungsprüfung im konkreten Tenant festlegen. Keine automatische Aussage über eine vorhandene Konfiguration.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-MSSEC','CLOUD');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-MSSEC','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-MSSEC','Control','Privilegierte Cloudzugriffe absichern · CTRL','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Umsetzung: Administrationskonten, starke Authentifizierung, Notfallzugriff und regelmäßige Berechtigungsprüfung im konkreten Tenant festlegen. Keine automatische Aussage über eine vorhandene Konfiguration.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-MSSEC','Control','V06-CTRL-MSSEC','Privilegierte Cloudzugriffe absichern · CTRL','MSSEC','V06-CTRL-MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Umsetzung: Administrationskonten, starke Authentifizierung, Notfallzugriff und regelmäßige Berechtigungsprüfung im konkreten Tenant festlegen. Keine automatische Aussage über eine vorhandene Konfiguration.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-MSSEC','CLOUD');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-MSSEC','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-MSSEC','Risk','Privilegierte Cloudzugriffe absichern · RISK','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Szenario: Die vorgesehene Steuerung zu „Privilegierte Cloudzugriffe absichern“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-MSSEC','Audit','Privilegierte Cloudzugriffe absichern · AUD','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Prüfziel: Umsetzung von „Privilegierte Cloudzugriffe absichern“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-MSSEC','Evidence','Privilegierte Cloudzugriffe absichern · EV','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Privilegierte Cloudzugriffe absichern“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-MSSEC','AuditTest','Privilegierte Cloudzugriffe absichern · TEST','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Prüfhandlung: Umsetzung von „Privilegierte Cloudzugriffe absichern“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-MSSEC','Finding','Privilegierte Cloudzugriffe absichern · FIND','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-MSSEC','Measure','Privilegierte Cloudzugriffe absichern · MEAS','MSSEC','Konkrete offizielle Produktdokumentation und Stand ergänzen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-MSSEC','V06-CTRL-MSSEC','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-AIACT','Requirement','KI-Freigabe und Aufsicht organisieren · REQ','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','KI-System, Organisationsrolle, Zweck, Datenarten und menschliche Aufsicht erfassen. Rechtliche Einordnung und zeitliche Anwendbarkeit fachlich freigeben.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-AIACT','Requirement','V06-REQ-AIACT','KI-Freigabe und Aufsicht organisieren · REQ','AIACT','V06-REQ-AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','KI-System, Organisationsrolle, Zweck, Datenarten und menschliche Aufsicht erfassen. Rechtliche Einordnung und zeitliche Anwendbarkeit fachlich freigeben.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-AIACT','AI');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-AIACT','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-AIACT','Control','KI-Freigabe und Aufsicht organisieren · CTRL','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Umsetzung: KI-System, Organisationsrolle, Zweck, Datenarten und menschliche Aufsicht erfassen. Rechtliche Einordnung und zeitliche Anwendbarkeit fachlich freigeben.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-AIACT','Control','V06-CTRL-AIACT','KI-Freigabe und Aufsicht organisieren · CTRL','AIACT','V06-CTRL-AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Umsetzung: KI-System, Organisationsrolle, Zweck, Datenarten und menschliche Aufsicht erfassen. Rechtliche Einordnung und zeitliche Anwendbarkeit fachlich freigeben.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-AIACT','AI');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-AIACT','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-AIACT','Risk','KI-Freigabe und Aufsicht organisieren · RISK','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Szenario: Die vorgesehene Steuerung zu „KI-Freigabe und Aufsicht organisieren“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-AIACT','Audit','KI-Freigabe und Aufsicht organisieren · AUD','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Prüfziel: Umsetzung von „KI-Freigabe und Aufsicht organisieren“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-AIACT','Evidence','KI-Freigabe und Aufsicht organisieren · EV','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „KI-Freigabe und Aufsicht organisieren“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-AIACT','AuditTest','KI-Freigabe und Aufsicht organisieren · TEST','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Prüfhandlung: Umsetzung von „KI-Freigabe und Aufsicht organisieren“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-AIACT','Finding','KI-Freigabe und Aufsicht organisieren · FIND','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-AIACT','Measure','KI-Freigabe und Aufsicht organisieren · MEAS','AIACT','Aktuelle KI-Verordnung; rollenbezogene Vorschrift ergänzen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-AIACT','V06-CTRL-AIACT','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-SPDX','Requirement','Lizenzprüfung releasebezogen dokumentieren · REQ','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Komponenten, Versionen, Lizenzangaben und konkrete Nutzung/Distribution erfassen. Unklare Lizenzbedingungen zur fachlichen Prüfung eskalieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-SPDX','Requirement','V06-REQ-SPDX','Lizenzprüfung releasebezogen dokumentieren · REQ','SPDX','V06-REQ-SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Komponenten, Versionen, Lizenzangaben und konkrete Nutzung/Distribution erfassen. Unklare Lizenzbedingungen zur fachlichen Prüfung eskalieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-SPDX','OSS');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-SPDX','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-SPDX','Control','Lizenzprüfung releasebezogen dokumentieren · CTRL','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Umsetzung: Komponenten, Versionen, Lizenzangaben und konkrete Nutzung/Distribution erfassen. Unklare Lizenzbedingungen zur fachlichen Prüfung eskalieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-SPDX','Control','V06-CTRL-SPDX','Lizenzprüfung releasebezogen dokumentieren · CTRL','SPDX','V06-CTRL-SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Umsetzung: Komponenten, Versionen, Lizenzangaben und konkrete Nutzung/Distribution erfassen. Unklare Lizenzbedingungen zur fachlichen Prüfung eskalieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-SPDX','OSS');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-SPDX','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-SPDX','Risk','Lizenzprüfung releasebezogen dokumentieren · RISK','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Szenario: Die vorgesehene Steuerung zu „Lizenzprüfung releasebezogen dokumentieren“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-SPDX','Audit','Lizenzprüfung releasebezogen dokumentieren · AUD','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Prüfziel: Umsetzung von „Lizenzprüfung releasebezogen dokumentieren“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-SPDX','Evidence','Lizenzprüfung releasebezogen dokumentieren · EV','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Lizenzprüfung releasebezogen dokumentieren“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-SPDX','AuditTest','Lizenzprüfung releasebezogen dokumentieren · TEST','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Prüfhandlung: Umsetzung von „Lizenzprüfung releasebezogen dokumentieren“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-SPDX','Finding','Lizenzprüfung releasebezogen dokumentieren · FIND','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-SPDX','Measure','Lizenzprüfung releasebezogen dokumentieren · MEAS','SPDX','SPDX-Spezifikation plus tatsächlicher Lizenztext','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-SPDX','V06-CTRL-SPDX','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-CYCLONEDX','Requirement','Release-SBOM nachvollziehbar erstellen · REQ','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','BOM-Version, Erstellungswerkzeug, Komponenten und Abhängigkeiten dokumentieren. Vollständigkeit und Zuordnung zum Release prüfen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-CYCLONEDX','Requirement','V06-REQ-CYCLONEDX','Release-SBOM nachvollziehbar erstellen · REQ','CYCLONEDX','V06-REQ-CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','BOM-Version, Erstellungswerkzeug, Komponenten und Abhängigkeiten dokumentieren. Vollständigkeit und Zuordnung zum Release prüfen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-CYCLONEDX','OSS');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-CYCLONEDX','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-CYCLONEDX','Control','Release-SBOM nachvollziehbar erstellen · CTRL','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Umsetzung: BOM-Version, Erstellungswerkzeug, Komponenten und Abhängigkeiten dokumentieren. Vollständigkeit und Zuordnung zum Release prüfen.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-CYCLONEDX','Control','V06-CTRL-CYCLONEDX','Release-SBOM nachvollziehbar erstellen · CTRL','CYCLONEDX','V06-CTRL-CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Umsetzung: BOM-Version, Erstellungswerkzeug, Komponenten und Abhängigkeiten dokumentieren. Vollständigkeit und Zuordnung zum Release prüfen.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-CYCLONEDX','OSS');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-CYCLONEDX','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-CYCLONEDX','Risk','Release-SBOM nachvollziehbar erstellen · RISK','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Szenario: Die vorgesehene Steuerung zu „Release-SBOM nachvollziehbar erstellen“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-CYCLONEDX','Audit','Release-SBOM nachvollziehbar erstellen · AUD','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Prüfziel: Umsetzung von „Release-SBOM nachvollziehbar erstellen“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-CYCLONEDX','Evidence','Release-SBOM nachvollziehbar erstellen · EV','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „Release-SBOM nachvollziehbar erstellen“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-CYCLONEDX','AuditTest','Release-SBOM nachvollziehbar erstellen · TEST','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Prüfhandlung: Umsetzung von „Release-SBOM nachvollziehbar erstellen“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-CYCLONEDX','Finding','Release-SBOM nachvollziehbar erstellen · FIND','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-CYCLONEDX','Measure','Release-SBOM nachvollziehbar erstellen · MEAS','CYCLONEDX','CycloneDX Specification Overview; Formatversion ergänzen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-CYCLONEDX','V06-CTRL-CYCLONEDX','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-REQ-DORA','Requirement','IKT-Abhängigkeiten und Dienstleister steuern · REQ','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Anwendungsbereich und Kritikalität von IKT-Dienstleistungen prüfen. Vertrags-, Überwachungs- und Exit-Anforderungen fallbezogen dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-REQ-DORA','Requirement','V06-REQ-DORA','IKT-Abhängigkeiten und Dienstleister steuern · REQ','DORA','V06-REQ-DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Anwendungsbereich und Kritikalität von IKT-Dienstleistungen prüfen. Vertrags-, Überwachungs- und Exit-Anforderungen fallbezogen dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-REQ-DORA','DORA');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-REQ-DORA','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-CTRL-DORA','Control','IKT-Abhängigkeiten und Dienstleister steuern · CTRL','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Umsetzung: Anwendungsbereich und Kritikalität von IKT-Dienstleistungen prüfen. Vertrags-, Überwachungs- und Exit-Anforderungen fallbezogen dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,IsSeed) VALUES('seed-V06-CTRL-DORA','Control','V06-CTRL-DORA','IKT-Abhängigkeiten und Dienstleister steuern · CTRL','DORA','V06-CTRL-DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Umsetzung: Anwendungsbereich und Kritikalität von IKT-Dienstleistungen prüfen. Vertrags-, Überwachungs- und Exit-Anforderungen fallbezogen dokumentieren.
Ausnahmen und Prüfverfahren festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO RecordTopics VALUES('seed-V06-CTRL-DORA','DORA');
INSERT OR IGNORE INTO RecordSources VALUES('seed-V06-CTRL-DORA','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Primär');
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-RISK-DORA','Risk','IKT-Abhängigkeiten und Dienstleister steuern · RISK','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Szenario: Die vorgesehene Steuerung zu „IKT-Abhängigkeiten und Dienstleister steuern“ fehlt oder ist unwirksam. Betroffene Werte, Ursachen, Auswirkungen, Eintrittsbedingungen und Restrisiko konkret bewerten.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-AUD-DORA','Audit','IKT-Abhängigkeiten und Dienstleister steuern · AUD','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Prüfziel: Umsetzung von „IKT-Abhängigkeiten und Dienstleister steuern“ anhand festgelegter Kriterien prüfen. Scope, Zeitraum, Prüfer, Unabhängigkeit und Stichprobe vor Beginn dokumentieren.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-EV-DORA','Evidence','IKT-Abhängigkeiten und Dienstleister steuern · EV','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Erwarteter Nachweis: datiertes Dokument, Konfigurationsauszug oder Prüfprotokoll zu „IKT-Abhängigkeiten und Dienstleister steuern“. Herkunft, Scope, Version, Prüfer und Gültigkeit dokumentieren. Diese Vorlage ist kein tatsächlicher Nachweis.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-TEST-DORA','AuditTest','IKT-Abhängigkeiten und Dienstleister steuern · TEST','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Prüfhandlung: Umsetzung von „IKT-Abhängigkeiten und Dienstleister steuern“ anhand der ausgewählten Quelle und konkreter Stichprobe prüfen. Erwartung und objektive Beobachtung gegenüberstellen. Ergebnis erst nach tatsächlicher Prüfung festlegen.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-FIND-DORA','Finding','IKT-Abhängigkeiten und Dienstleister steuern · FIND','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Kriterium: [Quelle/Fundstelle]. Beobachtung: [objektiver Sachverhalt]. Stichprobe: [Umfang]. Abweichung und Auswirkung: [begründet ergänzen]. Keine Feststellung ohne tatsächliche Prüfung.

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO Templates(Id,Kind,Title,SourceId,Locator,Body,Revision) VALUES('V06-MEAS-DORA','Measure','IKT-Abhängigkeiten und Dienstleister steuern · MEAS','DORA','Aktuelle DORA-Fassung und einschlägige technische Standards prüfen','Maßnahme: [konkrete Tätigkeit]. Verantwortlich: [Rolle]. Termin: [Datum]. Erfolgskriterium: [messbar]. Umsetzungsnachweis: [Datei]. Wirksamkeitsprüfung: [Prüfer/Termin].

Geltungsbereich: [ergänzen]
Eigene Ergänzungen: [ergänzen]',1);
INSERT OR IGNORE INTO MappingSuggestions(RequirementId,ControlId,Reason) VALUES('V06-REQ-DORA','V06-CTRL-DORA','Thematischer Vorschlag. Scope und Abdeckungsgrenzen fachlich prüfen; erst Bestätigung erzeugt eine aktive Zuordnung.');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G000','Governance','Unternehmenssteuerung','Rollen, Regeln und Entscheidungsprozesse für nachvollziehbare Steuerung.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Governance');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G001','Compliance','Anforderungserfüllung','Behandlung einschlägiger externer und interner Anforderungen, kein bloßer Prozentwert.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Compliance');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G002','ISMS','Informationssicherheitsmanagementsystem','Rahmen zur Planung, Umsetzung, Prüfung und Verbesserung der Informationssicherheit.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='ISMS');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G003','DSMS','Datenschutzmanagementsystem','Organisatorischer Rahmen für Datenschutzprozesse.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='DSMS');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G004','Requirement','Anforderung','Dokumentierte Erwartung mit Quelle, Fundstelle und Scope.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Requirement');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G005','Audit','Systematische Prüfung','Geplante Prüfung anhand definierter Kriterien und objektiver Nachweise.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Audit');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G006','Asset','Schutzobjekt','Information, System, Prozess oder Ressource mit Bedeutung für die Organisation.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Asset');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G007','Risikoeigentümer','Risk Owner','Verantwortung für Bewertung, Behandlung und Überwachung eines Risikos.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Risikoeigentümer');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G008','Inhärentes Risiko','Ausgangsrisiko','Risiko vor Berücksichtigung der methodisch definierten risikomindernden Maßnahmen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Inhärentes Risiko');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G009','Risikobehandlung','Risk Treatment','Vermeiden, Vermindern, Übertragen oder begründetes Akzeptieren von Risiken.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Risikobehandlung');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G010','Wirksamkeit','Effectiveness','Erreichen des beabsichtigten Ergebnisses im konkreten Scope.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Wirksamkeit');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G011','DSGVO','Datenschutz-Grundverordnung','Verordnung (EU) 2016/679; aktuelle Fassung und Anwendbarkeit prüfen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='DSGVO');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G012','TOM','Technische und organisatorische Maßnahmen','Vorkehrungen zur Behandlung von Datenschutz- und Sicherheitsrisiken.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='TOM');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G013','Verantwortlicher','Controller','Datenschutzrechtliche Rolle für Zwecke und Mittel einer Verarbeitung; konkrete Zuordnung prüfen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Verantwortlicher');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G014','Auftragsverarbeiter','Processor','Datenschutzrechtliche Rolle bei Verarbeitung im Auftrag; tatsächliche Verhältnisse prüfen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Auftragsverarbeiter');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G015','NIS2','NIS2-Richtlinie','EU-Richtlinie und nationale Umsetzung sind getrennt zu prüfen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='NIS2');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G016','DORA','Digital Operational Resilience Act','EU-Verordnung zur digitalen operationalen Resilienz im Finanzsektor.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='DORA');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G017','AI Act','KI-Verordnung','Verordnung (EU) 2024/1689; Rolle, Änderungen und zeitliche Anwendbarkeit prüfen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='AI Act');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G018','GPAI','General-Purpose AI','KI-Modelle mit allgemeinem Verwendungszweck; nicht mit jedem KI-System gleichzusetzen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='GPAI');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G019','Menschliche Aufsicht','Human Oversight','Festgelegte menschliche Überwachung und Eingriffsmöglichkeit.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Menschliche Aufsicht');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G020','Open Source','Offener Quellcode','Software unter geeigneter offener Lizenz; sichtbarer Code allein genügt nicht.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Open Source');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G021','Open Weight','Offene Modellgewichte','Verfügbarkeit von Modellgewichten ist nicht automatisch Open Source.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Open Weight');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G022','SPDX','System Package Data Exchange','Standard für strukturierte Stücklisten und Lizenzinformationen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='SPDX');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G023','CycloneDX','BOM-Standard','Standard für Komponenten-, Abhängigkeits- und Lieferketteninformationen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='CycloneDX');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G024','Copyleft','Lizenzprinzip','Weitergabepflichten anhand konkreter Lizenz und Nutzung prüfen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='Copyleft');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G025','CVE','Common Vulnerabilities and Exposures','Kennung für dokumentierte Schwachstellen, kein alleiniger Risikomaßstab.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='CVE');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G026','CVSS','Common Vulnerability Scoring System','Technische Schwachstellenschwere, keine vollständige Kontextbewertung.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='CVSS');
INSERT INTO Glossary(Id,Term,Expanded,Definition,SourceId) SELECT 'V06-G027','RBAC','Role-Based Access Control','Berechtigungsmodell auf Basis zugewiesener Rollen.','INTERNAL' WHERE NOT EXISTS(SELECT 1 FROM Glossary WHERE Term='RBAC');
