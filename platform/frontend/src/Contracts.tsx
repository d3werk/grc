import { useState } from 'react';
import { ArrowRight, CalendarClock, CalendarDays, FileSignature, TriangleAlert } from 'lucide-react';
import type { CatalogRecord } from './api';
import { recordTitle } from './Catalog';
import type { ModuleDefinition, ModuleOverviewProps } from './ModuleWorkspace';
import { dateLabel, Empty, todayIso } from './ui';

export const contractKinds = {
  contract: {
    title: 'Vertragsregister', singular: 'Vertrag', icon: FileSignature,
    description: 'Vertragsparteien, Verantwortung, Laufzeiten und Entscheidungen zur Fortführung erfassen.'
  }
};

const deadlineFields = {
  notice_due: 'Kündigungsstichtag', review_due: 'Prüftermin', ends_on: 'Laufzeitende'
};
type DeadlineKind = keyof typeof deadlineFields;
type Deadline = { record: CatalogRecord; type: DeadlineKind; date: string };
type DeadlineFilter = 'all' | 'overdue' | 'upcoming';

function contractDeadlines(records: CatalogRecord[]): Deadline[] {
  return records.flatMap(record => {
    if (record.status !== 'active' && record.status !== 'in_review') return [];
    return (Object.keys(deadlineFields) as DeadlineKind[]).flatMap(type => {
      const date = record.attributes[type];
      return date ? [{ record, type, date }] : [];
    });
  }).sort((a, b) => a.date.localeCompare(b.date) || a.record.title.localeCompare(b.record.title, 'de'));
}

const filterLabels = {
  all: 'Erfasste Termine', overdue: 'Überfällige Termine', upcoming: 'Termine in den nächsten 30 Tagen'
};
const pageSize = 12;

function ContractOverview({ rows, openRecord }: ModuleOverviewProps) {
  const [filter, setFilter] = useState<DeadlineFilter>('upcoming');
  const [page, setPage] = useState(0);
  const contracts = rows.filter(record => record.kind === 'contract');
  const deadlines = contractDeadlines(contracts);
  const today = todayIso();
  const horizon = new Date(`${today}T00:00:00Z`);
  horizon.setUTCDate(horizon.getUTCDate() + 30);
  const horizonIso = horizon.toISOString().slice(0, 10);
  const options = {
    all: deadlines,
    overdue: deadlines.filter(deadline => deadline.date < today),
    upcoming: deadlines.filter(deadline => deadline.date >= today && deadline.date <= horizonIso)
  };
  const visible = options[filter];
  const lastPage = Math.max(0, Math.ceil(visible.length / pageSize) - 1);
  const currentPage = Math.min(page, lastPage);
  const filters = [
    { key: 'overdue', icon: TriangleAlert }, { key: 'upcoming', icon: CalendarClock }, { key: 'all', icon: CalendarDays }
  ] as const;

  return <>
    <div className="metric-grid" aria-label="Verträge und Termine">
      <button type="button" className="metric" onClick={() => openRecord('contract')}>
        <span className="metric-top"><span className="glyph"><FileSignature size={20} aria-hidden="true" /></span>
          <ArrowRight size={16} aria-hidden="true" /></span>
        <strong>{contracts.length}</strong><span>Verträge im Register</span>
      </button>
      {filters.map(({ key, icon: Icon }) => <button type="button" className="metric" key={key}
        aria-pressed={filter === key} aria-controls="contract-deadlines"
        onClick={() => { setFilter(key); setPage(0); }}>
        <span className="metric-top"><span className="glyph"><Icon size={20} aria-hidden="true" /></span></span>
        <strong>{options[key].length}</strong><span>{filterLabels[key]}</span>
      </button>)}
    </div>
    <section className="panel" id="contract-deadlines" aria-labelledby="contract-deadlines-title">
      <div className="panel-title"><h2 id="contract-deadlines-title">{filterLabels[filter]}</h2></div>
      <p className="muted">Die Ansicht berücksichtigt eingetragene Termine aktiver Verträge und Verträge in Prüfung.</p>
      {visible.length ? <div className="table-scroll"><table>
        <thead><tr><th scope="col">Vertrag</th><th scope="col">Verantwortung</th><th scope="col">Terminart</th>
          <th scope="col">Datum</th><th scope="col">Entscheidung</th></tr></thead>
        <tbody>{visible.slice(currentPage * pageSize, (currentPage + 1) * pageSize).map(({ record, type, date }) => <tr key={`${record.id}:${type}`}>
          <th scope="row"><button type="button" className="row-button" onClick={() => openRecord('contract', record.id)}>{recordTitle(record)}</button>
            {record.attributes.counterparty && <small>{record.attributes.counterparty}</small>}</th>
          <td>{record.owner || '–'}</td><td>{deadlineFields[type]}</td>
          <td><time dateTime={date}>{dateLabel(date)}</time>{date < today && <small>Überfällig</small>}</td>
          <td>{record.attributes.decision || 'Offen'}</td>
        </tr>)}</tbody>
      </table></div> : <Empty title="Keine Termine in dieser Ansicht">Öffnen Sie das Vertragsregister, um Laufzeiten, Prüftermine oder Kündigungsstichtage zu pflegen.</Empty>}
      {lastPage > 0 && <div className="actions">
        <button type="button" disabled={currentPage === 0} onClick={() => setPage(currentPage - 1)}>Zurück</button>
        <span aria-live="polite">Seite {currentPage + 1} von {lastPage + 1}</span>
        <button type="button" disabled={currentPage === lastPage} onClick={() => setPage(currentPage + 1)}>Weiter</button>
      </div>}
    </section>
  </>;
}

export const contractModule: ModuleDefinition = {
  title: 'Vertragsmanagement',
  description: 'Verträge vom Entwurf bis zum Abschluss verwalten und Kündigungs-, Prüf- und Laufzeitenden im Blick behalten.',
  sections: [{
    kind: 'contract', label: 'Vertragsregister',
    description: 'Vertragspflichten und Entscheidungen dokumentieren. Lieferanten, Dienste und Kontrollen verknüpfen.',
    columns: [
      { key: 'counterparty', label: 'Vertragspartner' }, { key: 'notice_due', label: 'Kündigungsstichtag' },
      { key: 'ends_on', label: 'Laufzeitende' }
    ]
  }],
  overview: ContractOverview
};
