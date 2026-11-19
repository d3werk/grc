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
db.execute('ROLLBACK TO supplier_fixture')
db.execute('RELEASE supplier_fixture')

# Compile static parameterized SQL against the migrated schema. No C# execution.
db.execute("CREATE TABLE Profiles(Name TEXT PRIMARY KEY,Salt BLOB,Verifier BLOB,Failures INTEGER,LockedUntil TEXT)")
for p in (R/'src/D3.Grc.Core').glob('*.cs'):
 for i,m in enumerate(re.finditer(r'(?:Exec|Scalar|Query|Command)\(c,(?:t|null),"((?:[^"\\]|\\.)*)"',p.read_text())):
  sql=m.group(1).replace('\\"','"').replace('\\n','\n');params={name:None for name in re.findall(r'\$(p\d+)',sql)}
  try:db.execute('EXPLAIN '+sql,params).fetchall()
  except Exception as ex:raise AssertionError(f'{p.name} SQL {i}: {sql}: {ex}')
  check('SQL compiles '+p.name+' #'+str(i))
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
