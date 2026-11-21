"""Independent SQL/XML/source checks. These do NOT execute .NET, Avalonia or C# tests."""
from argparse import ArgumentParser
from pathlib import Path
import sqlite3,json,re,xml.etree.ElementTree as ET,hashlib
parser = ArgumentParser(description=__doc__)
parser.add_argument('--report', type=Path, help='Optional output path for all check results')
args = parser.parse_args()
R=Path(__file__).resolve().parents[1];checks=[]
def check(name,value=True):
 if not value:raise AssertionError(name)
 checks.append(name)
for p in list(R.rglob('*.csproj'))+[R/'Directory.Build.props']:
 root=ET.parse(p).getroot();check('XML '+str(p.relative_to(R)))
 for ref in root.findall('.//ProjectReference'):check('Project reference '+ref.attrib['Include'],(p.parent/ref.attrib['Include']).resolve().is_file())
check('Stable SDK pinned',json.loads((R/'global.json').read_text())['sdk']['allowPrerelease'] is False)
source=sqlite3.connect(R/'content/catalog-v05.db');db=sqlite3.connect(':memory:');source.backup(db);source.close();db.execute('PRAGMA foreign_keys=ON')
# Modify a fixture, not the delivered reference database.
db.execute("UPDATE Templates SET CustomBody='Preserve own template' WHERE Id='REQ-001'")
db.execute("UPDATE Records SET Body='Preserve own record',Owner='Fixture owner' WHERE Id='seed-REQ-001'")
db.execute("UPDATE Sources SET CustomSummary='Preserve source note' WHERE Id='GDPR'")
before={table:db.execute('SELECT * FROM '+table).fetchall() for table in ['Records','Templates','Glossary','Sources','Evidence','AuditTests','Journal']};db.commit()
db.executescript((R/'src/D3.Grc.Core/Migrations/006.sql').read_text())
for table,rows in before.items():
 for row in rows:
  current=db.execute('SELECT * FROM '+table+' WHERE Id=?',(row[0],)).fetchone()
  check('Migration preserves '+table+' '+str(row[0]),current==row)
check('Foreign keys valid',db.execute('PRAGMA foreign_key_check').fetchall()==[])
check('SQLite integrity',db.execute('PRAGMA integrity_check').fetchone()[0]=='ok')
check('Privacy source filter',set(x[0] for x in db.execute("SELECT SourceId FROM TopicSources WHERE TopicId='PRIVACY'"))=={'INTERNAL','GDPR','BDSG'})
check('BCM source filter',set(x[0] for x in db.execute("SELECT SourceId FROM TopicSources WHERE TopicId='BCM'"))=={'INTERNAL','BSI2004'})
check('Cloud source filter',set(x[0] for x in db.execute("SELECT SourceId FROM TopicSources WHERE TopicId='CLOUD'"))=={'INTERNAL','MSSEC'})
check('No orphan records',db.execute('SELECT COUNT(*) FROM Records WHERE Id NOT IN(SELECT RecordId FROM RecordTopics)').fetchone()[0]==0)
check('No fabricated evidence',db.execute('SELECT COUNT(*) FROM Evidence').fetchone()[0]==0)
check('No fabricated audit results',db.execute('SELECT COUNT(*) FROM AuditTests').fetchone()[0]==0)
check('No fabricated successful source checks',db.execute("SELECT COUNT(*) FROM Sources WHERE LastSuccess<>''").fetchone()[0]==0)
check('All starter records drafts',db.execute("SELECT COUNT(*) FROM Records WHERE Status<>'Entwurf'").fetchone()[0]==0)
check('48 glossary terms',db.execute('SELECT COUNT(*) FROM Glossary').fetchone()[0]==48)
check('126 starter templates',db.execute('SELECT COUNT(*) FROM Templates').fetchone()[0]==126)
check('36 starter records',db.execute('SELECT COUNT(*) FROM Records').fetchone()[0]==36)
check('13 sources',db.execute('SELECT COUNT(*) FROM Sources').fetchone()[0]==13)
check('12 suggestions',db.execute('SELECT COUNT(*) FROM MappingSuggestions').fetchone()[0]==12)
check('All suggestions type-correct',db.execute("SELECT COUNT(*) FROM MappingSuggestions m JOIN Templates a ON a.Id=m.RequirementId JOIN Templates b ON b.Id=m.ControlId WHERE a.Kind<>'Requirement' OR b.Kind<>'Control'").fetchone()[0]==0)

# Migration 007 must preserve populated 006 tables and add an empty register.
protected_tables = ['Records', 'RecordTopics', 'Templates', 'Glossary', 'Sources', 'Evidence', 'AuditTests', 'Journal']
before_007 = {table: db.execute(f'SELECT * FROM {table}').fetchall() for table in protected_tables}
db.executescript((R/'src/D3.Grc.Core/Migrations/007.sql').read_text())
for table, rows in before_007.items():
    check(f'Supplier migration preserves {table}', db.execute(f'SELECT * FROM {table}').fetchall() == rows)
check('Supplier migration FK', db.execute('PRAGMA foreign_key_check').fetchall() == [])
check('Supplier migration integrity', db.execute('PRAGMA integrity_check').fetchone()[0] == 'ok')
check('No fabricated suppliers', db.execute('SELECT COUNT(*) FROM SupplierDetails').fetchone()[0] == 0)
check('No fabricated supplier reviews', db.execute('SELECT COUNT(*) FROM SupplierReviews').fetchone()[0] == 0)

db.execute('SAVEPOINT supplier_fixture')
db.execute("INSERT INTO Records(Id,Kind,Code,Title,SourceId,Body) VALUES('fixture-supplier','Supplier','','Fixture','INTERNAL','')")
db.execute("INSERT INTO SupplierDetails VALUES('fixture-supplier','Synthetic service',3,'','Synthetic rationale')")
db.execute("""INSERT INTO SupplierReviews VALUES
    ('fixture-review','fixture-supplier','2026-10-15','Fixture reviewer',0,'Synthetic assessment','2027-01-15','2026-10-15T08:00:00Z')
    """)
for statement, label in [
    ("UPDATE SupplierReviews SET Notes='Edited' WHERE Id='fixture-review'", 'Supplier review cannot be updated'),
    ("DELETE FROM SupplierReviews WHERE Id='fixture-review'", 'Supplier review cannot be deleted'),
]:
    try:
        db.execute(statement)
    except sqlite3.IntegrityError:
        check(label)
    else:
        check(label, False)
db.execute('RELEASE supplier_fixture')

# Migration 008 adds an empty document register and a separate, additive template collection.
# Retain a populated supplier and its control link to verify upgrade compatibility.
control_id = db.execute("SELECT Id FROM Records WHERE Kind='Control' ORDER BY Id LIMIT 1").fetchone()[0]
db.execute("INSERT INTO RecordTopics VALUES('fixture-supplier','ISMS')")
db.execute("""INSERT INTO Links(Id,SourceRecordId,TargetRecordId,Type,Reason,SourceId)
    VALUES('fixture-supplier-link','fixture-supplier',?,'Dienstleister → Control','Synthetic relationship','INTERNAL')""",
    (control_id,))
db.execute("""INSERT INTO MappingAssessments(LinkId,Coverage,Scope,Approval)
    VALUES('fixture-supplier-link','Teilweise','','Bestätigt')""")
migration_008_tables = protected_tables + [
    'Links', 'MappingAssessments', 'RecordSources', 'SupplierDetails', 'SupplierReviews'
]
before_008 = {table: db.execute(f'SELECT * FROM {table}').fetchall() for table in migration_008_tables}
db.executescript((R/'src/D3.Grc.Core/Migrations/008.sql').read_text())
template_migration = (R/'src/D3.Grc.Core/Migrations/008-templates.sql').read_text()
db.executescript(template_migration)
for table, rows in before_008.items():
    query = ("""SELECT * FROM Templates WHERE Id NOT LIKE 'D3-WP-%' AND Id NOT LIKE 'D3-DOC-%'"""
             if table == 'Templates' else f'SELECT * FROM {table}')
    check(f'Document migration preserves {table}', db.execute(query).fetchall() == rows)
check('Document migration FK', db.execute('PRAGMA foreign_key_check').fetchall() == [])
check('Document migration integrity', db.execute('PRAGMA integrity_check').fetchone()[0] == 'ok')
check('Document minimum app version', db.execute("SELECT Value FROM Metadata WHERE Key='MinAppVersion'").fetchone() == ('0.9.0',))
for table in ['Documents', 'PublishedDocumentVersions', 'DocumentLinks']:
    check(f'No fabricated {table}', db.execute(f'SELECT COUNT(*) FROM {table}').fetchone()[0] == 0)

new_templates = db.execute("""SELECT Id,Kind,SourceId,Title,Locator,Body,Revision,CustomBody
    FROM Templates WHERE Id LIKE 'D3-WP-%' ORDER BY Id""").fetchall()
check('27 new working templates', len(new_templates) == 27)
check('157 total starter templates', db.execute('SELECT COUNT(*) FROM Templates').fetchone()[0] == 157)
check('Working template titles are distinct', len({row[3].casefold() for row in new_templates}) == len(new_templates))
working_topics = ('PRIVACY', 'ISMS', 'NIS2', 'AI', 'CLOUD', 'BCM', 'OSS', 'DORA', 'GOV')
for topic in working_topics:
    group = [row for row in new_templates if row[0].startswith(f'D3-WP-{topic}-')]
    check(f'Three working template kinds for {topic}', len(group) == len({row[1] for row in group}) == 3)
    for template_id, _, source_id, title, locator, body, revision, custom in group:
        topic_id = 'ISMS' if topic == 'GOV' else topic
        check(f'Working template source matches topic {template_id}',
              db.execute('SELECT 1 FROM TopicSources WHERE TopicId=? AND SourceId=?', (topic_id, source_id)).fetchone() == (1,))
        check(f'Working template starts as editable baseline {template_id}',
              bool(title.strip() and locator.strip()) and revision == 1 and custom == '')
        headings = re.findall(r'^## +\S[^\r\n]*$', body, re.MULTILINE)
        check(f'Working template sections and project fields {template_id}',
              len(headings) >= 3 and re.search(r'\[[^\]\r\n]+\]', body) is not None)

policy_templates = db.execute("""SELECT Id,Kind,SourceId,Title,Locator,Body,Revision,CustomBody
    FROM Templates WHERE Id LIKE 'D3-DOC-%' ORDER BY Id""").fetchall()
policy_sources = {
    'D3-DOC-ISMS': ('ISO27001', {'ISMS'}),
    'D3-DOC-PRIVACY': ('GDPR', {'PRIVACY'}),
    'D3-DOC-BCM': ('BSI2004', {'BCM'}),
    'D3-DOC-SUPPLIER': ('INTERNAL', set(working_topics) - {'GOV'})
}
check('Four original document policy templates', {row[0] for row in policy_templates} == set(policy_sources))
for template_id, kind, source_id, title, locator, body, revision, custom in policy_templates:
    expected_source, expected_topics = policy_sources[template_id]
    source_topics = {row[0] for row in db.execute('SELECT TopicId FROM TopicSources WHERE SourceId=?', (source_id,))}
    check(f'Policy template source matches topic {template_id}', source_id == expected_source and expected_topics <= source_topics)
    check(f'Policy template starts as editable baseline {template_id}',
          kind == 'Policy' and bool(title.strip() and locator.strip()) and revision == 1 and custom == '')
    headings = re.findall(r'^## +\S[^\r\n]*$', body, re.MULTILINE)
    table_divider = re.search(r'^\|(?:\s*:?-+:?\s*\|){2,}\s*$', body, re.MULTILINE)
    check(f'Policy template includes sections, order, tables and project fields {template_id}',
          len(body) >= 2800 and len(headings) >= 4 and table_divider is not None
          and re.search(r'^1\. \S', body, re.MULTILINE) is not None
          and re.search(r'\[[^\]\r\n]+\]', body) is not None)

db.execute("""UPDATE Templates SET CustomBody='Preserve working template edit'
    WHERE Id IN ('D3-WP-PRIVACY-REQ','D3-DOC-ISMS')""")
db.executescript(template_migration)
check('Template seed rerun preserves edited versions', db.execute(
    "SELECT COUNT(*) FROM Templates WHERE Id IN ('D3-WP-PRIVACY-REQ','D3-DOC-ISMS') AND CustomBody='Preserve working template edit'"
).fetchone() == (2,))

db.execute('SAVEPOINT document_fixture')
db.execute("""INSERT INTO Documents(Id,Title,TopicId,Scope,Category,Owner,Content,Status,Version,Revision,
    PublishedVersion,UpdatedUtc) VALUES('fixture-document','Fixture','ISMS','Test',0,'Fixture owner','Draft',3,1,1,1,'2026-11-20T09:00:00Z')""")
db.execute("""INSERT INTO PublishedDocumentVersions VALUES('fixture-document',1,'Fixture','ISMS','Test',0,
    'Fixture owner','Published text','Reviewer','2026-11-20T08:00:00Z','Reviewed','Publisher','2026-11-20T09:00:00Z','Released')""")
for statement, label in [
    ("UPDATE PublishedDocumentVersions SET Content='Edited' WHERE DocumentId='fixture-document'",
     'Published document version cannot be updated'),
    ("DELETE FROM PublishedDocumentVersions WHERE DocumentId='fixture-document'",
     'Published document version cannot be deleted'),
    ("UPDATE Documents SET PublishedVersion=NULL WHERE Id='fixture-document'",
     'Published document requires a publication version'),
]:
    try:
        db.execute(statement)
    except sqlite3.IntegrityError:
        check(label)
    else:
        check(label, False)
db.execute('ROLLBACK TO document_fixture')
db.execute('RELEASE document_fixture')

# Compile static parameterized SQL against the migrated schema. No C# execution.
db.execute("CREATE TABLE Profiles(Name TEXT PRIMARY KEY,Salt BLOB,Verifier BLOB,Failures INTEGER,LockedUntil TEXT)")
static_sql = re.compile(
    r'\b(?:Exec|Scalar|Query|Command)\(\s*\w+\s*,\s*\w+\s*,\s*'
    r'(?:"""(?P<raw>.*?)"""|"(?P<quoted>(?:[^"\\]|\\.)*)")(?=\s*[,\)])',
    re.DOTALL)
for path in (R/'src/D3.Grc.Core').glob('*.cs'):
    for index, match in enumerate(static_sql.finditer(path.read_text())):
        sql = match['raw'] if match['raw'] is not None else match['quoted'].replace('\\"', '"').replace('\\n', '\n')
        parameters = dict.fromkeys(re.findall(r'\$([A-Za-z_]\w*)', sql))
        try:
            db.execute('EXPLAIN ' + sql, parameters).fetchall()
        except sqlite3.Error as ex:
            raise AssertionError(f'{path.name} SQL {index}: {sql}: {ex}') from ex
        check(f'SQL compiles {path.name} #{index}')
# Reference database itself must be clean and populated.
ref=sqlite3.connect(R/'content/catalog-v06-reference.db');check('Reference FK',ref.execute('PRAGMA foreign_key_check').fetchall()==[]);check('Reference integrity',ref.execute('PRAGMA integrity_check').fetchone()[0]=='ok');ref.close()
for p in (R/'src').rglob('*.cs'):
 text=p.read_text();check('No ambiguous Template pattern '+p.name,'is not Template' not in text)
check('No vulnerable native SQLite 2.1.11 pin',not any('Version="2.1.11"' in p.read_text() for p in R.rglob('*.csproj')))
check('No prerelease SDK',not any('11.0.100-rc' in p.read_text() for p in R.rglob('*.json')))
check('Logo preserved',hashlib.sha256((R/'content/logo.png').read_bytes()).hexdigest()!='')
report = {'scope': 'Independent SQL/XML/source checks only', 'passed': len(checks), 'checks': checks}
if args.report:
    args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='checks'},indent=2))
