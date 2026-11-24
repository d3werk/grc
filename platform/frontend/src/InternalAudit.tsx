import { useState } from 'react';
import { CalendarDays, ClipboardCheck, ClipboardList, FileCheck2, FolderSearch, Plus, TriangleAlert } from 'lucide-react';
import type { CatalogRecord } from './api';
import { recordTitle, statusLabels } from './Catalog';
import type { OpenRecord } from './Catalog';
import type { ModuleDefinition, ModuleOverviewProps } from './ModuleWorkspace';
import { dateLabel, Empty, Field, todayIso } from './ui';
import './internal-audit.css';

export const auditKinds = {
  audit_subject: {
    title: 'Prüfungsuniversum', singular: 'Prüfeinheit', icon: FolderSearch,
    description: 'Prüfbare Einheiten mit Risikoeinstufung, Wesentlichkeit und Prüfzyklus erfassen.'
  },
  audit_engagement: {
    title: 'Prüfaufträge', singular: 'Prüfauftrag', icon: ClipboardList,
    description: 'Prüfungen terminieren, das Prüfprogramm dokumentieren und den Bericht freigeben.'
  },
  audit_test: {
    title: 'Prüfschritte', singular: 'Prüfschritt', icon: FileCheck2,
    description: 'Prüfhandlungen mit Kriterien, Stichproben, Arbeitspapieren und Nachweisen festhalten.'
  },
  audit_finding: {
    title: 'Feststellungen', singular: 'Feststellung', icon: TriangleAlert,
    description: 'Abweichungen bewerten, Maßnahmen zuordnen und ihre Wirksamkeit dokumentieren.'
  }
};

export const internalAuditModule: ModuleDefinition = {
  title: 'Interne Audits',
  description: 'Prüfungen risikobasiert planen und Feststellungen bis zur Wirksamkeitsprüfung verfolgen.',
  sections: [
    {
      kind: 'audit_subject', label: auditKinds.audit_subject.title, description: auditKinds.audit_subject.description,
      columns: [{ key: 'residual_risk', label: 'Restrisiko' }, { key: 'next_audit_on', label: 'Nächste Prüfung' }]
    },
    {
      kind: 'audit_engagement', label: auditKinds.audit_engagement.title, description: auditKinds.audit_engagement.description,
      columns: [{ key: 'plan_year', label: 'Planjahr' }, { key: 'priority', label: 'Priorität' }]
    },
    {
      kind: 'audit_test', label: auditKinds.audit_test.title, description: auditKinds.audit_test.description,
      columns: [{ key: 'result', label: 'Ergebnis' }]
    },
    {
      kind: 'audit_finding', label: auditKinds.audit_finding.title, description: auditKinds.audit_finding.description,
      columns: [{ key: 'severity', label: 'Schweregrad' }, { key: 'due_on', label: 'Frist' }]
    }
  ],
  overview: InternalAuditOverview
};

const compareDates = (first: string | undefined, second: string | undefined) =>
  (first || '9999-12-31').localeCompare(second || '9999-12-31');

function riskRating(record: CatalogRecord) {
  return Number(record.attributes.residual_risk || record.attributes.inherent_risk || 0);
}

function rankSubjects(left: CatalogRecord, right: CatalogRecord) {
  return riskRating(right) - riskRating(left)
    || Number(right.attributes.materiality || 0) - Number(left.attributes.materiality || 0)
    || compareDates(left.attributes.next_audit_on, right.attributes.next_audit_on)
    || left.title.localeCompare(right.title, 'de');
}

export function InternalAuditOverview({ rows, write, openRecord }: ModuleOverviewProps) {
  const today = todayIso();
  const currentYear = today.slice(0, 4);
  const [year, setYear] = useState(currentYear);
  const engagements = rows.filter(row => row.kind === 'audit_engagement' && row.status !== 'archived');
  const years = [...new Set([currentYear, ...engagements.map(row => row.attributes.plan_year).filter(Boolean)])].sort().reverse();
  const selectedYear = years.includes(year) ? year : currentYear;
  const plan = engagements.filter(row => row.attributes.plan_year === selectedYear)
    .sort((left, right) => compareDates(left.attributes.starts_on, right.attributes.starts_on));
  const finished = plan.filter(row => row.status === 'completed').length;
  const subjects = rows.filter(row => row.kind === 'audit_subject' && row.status !== 'archived').sort(rankSubjects);
  const open = rows.filter(row => row.kind === 'audit_finding' && (row.status === 'active' || row.status === 'in_review'))
    .sort((left, right) => compareDates(left.attributes.due_on, right.attributes.due_on));
  const overdue = open.filter(row => row.attributes.due_on && row.attributes.due_on < today).length;

  return <div className="audit-overview">
    <section className="panel" aria-labelledby="audit-plan-heading">
      <div className="panel-title">
        <div><h2 id="audit-plan-heading"><CalendarDays size={20} aria-hidden="true" /> Jahresplan</h2>
          <small>{finished} von {plan.length} Prüfaufträgen abgeschlossen</small></div>
        <div className="audit-planning-actions">
          <Field label="Planjahr"><select value={selectedYear} onChange={event => setYear(event.target.value)}>
            {years.map(value => <option key={value} value={value}>{value}</option>)}
          </select></Field>
          {write && <button type="button" className="primary" onClick={() => openRecord('audit_engagement', undefined, true)}>
            <Plus size={17} aria-hidden="true" /> Prüfauftrag anlegen</button>}
        </div>
      </div>
      {plan.length ? <div className="table-scroll"><table aria-labelledby="audit-plan-heading">
        <thead><tr><th scope="col">Prüfauftrag</th><th scope="col">Zeitraum</th><th scope="col">Priorität</th>
          <th scope="col">Soll / Ist (Std.)</th><th scope="col">Stand</th></tr></thead>
        <tbody>{plan.map(row => <tr key={row.id}>
          <AuditRowHeading row={row} openRecord={openRecord} />
          <td>{dateLabel(row.attributes.starts_on)} – {dateLabel(row.attributes.ends_on)}</td>
          <td>{row.attributes.priority || '–'}</td>
          <td>{row.attributes.budget_hours ?? '–'} / {row.attributes.actual_hours ?? '–'}</td>
          <td><span className={`status ${row.status}`}>{statusLabels[row.status]}</span></td>
        </tr>)}</tbody>
      </table></div> : <Empty title={`Keine Prüfaufträge für ${selectedYear}`}>
        Ein Prüfauftrag erscheint im Jahresplan, sobald ihm ein Planjahr zugeordnet ist.</Empty>}
    </section>
    <div className="dashboard-grid">
      <section className="panel" aria-labelledby="audit-universe-heading">
        <div className="panel-title"><h2 id="audit-universe-heading"><FolderSearch size={20} aria-hidden="true" /> Prüfprioritäten</h2>
          <button type="button" className="link-button" onClick={() => openRecord('audit_subject')}>Prüfungsuniversum öffnen</button></div>
        {subjects.length ? <div className="table-scroll"><table aria-labelledby="audit-universe-heading">
          <caption>Sortiert nach Restrisiko oder inhärentem Risiko, dann nach Wesentlichkeit und Prüftermin.</caption>
          <thead><tr><th scope="col">Prüfeinheit</th><th scope="col">Risiko</th><th scope="col">Nächste Prüfung</th></tr></thead>
          <tbody>{subjects.slice(0, 6).map(row => <tr key={row.id}>
            <AuditRowHeading row={row} openRecord={openRecord} />
            <td>{riskRating(row) ? `${riskRating(row)} / 5` : '–'}
              {riskRating(row) > 0 && <small>{row.attributes.residual_risk ? 'Restrisiko' : 'Inhärent'}</small>}</td>
            <td>{dateLabel(row.attributes.next_audit_on)}</td>
          </tr>)}</tbody>
        </table></div> : <Empty title="Keine Prüfeinheiten">Erfassen Sie Prüfeinheiten, um ihre Prüfprioritäten zu vergleichen.</Empty>}
      </section>
      <section className="panel" aria-labelledby="audit-findings-heading">
        <div className="panel-title"><div>
          <h2 id="audit-findings-heading"><ClipboardCheck size={20} aria-hidden="true" /> Offene Feststellungen</h2>
          <small>{open.length} offen, davon {overdue} überfällig</small>
        </div><button type="button" className="link-button" onClick={() => openRecord('audit_finding')}>Feststellungen öffnen</button></div>
        {open.length ? <div className="table-scroll"><table aria-labelledby="audit-findings-heading">
          <thead><tr><th scope="col">Feststellung</th><th scope="col">Schweregrad</th><th scope="col">Frist</th></tr></thead>
          <tbody>{open.slice(0, 6).map(row => <tr key={row.id}>
            <AuditRowHeading row={row} openRecord={openRecord} />
            <td>{row.attributes.severity || '–'}</td>
            <td>{dateLabel(row.attributes.due_on)}
              {row.attributes.due_on && row.attributes.due_on < today && <small className="audit-overdue">Überfällig</small>}</td>
          </tr>)}</tbody>
        </table></div> : <Empty title="Keine offenen Feststellungen" />}
      </section>
    </div>
  </div>;
}

function AuditRowHeading({ row, openRecord }: { row: CatalogRecord; openRecord: OpenRecord }) {
  return <th scope="row">
    <button type="button" className="row-button" onClick={() => openRecord(row.kind, row.id)}>{recordTitle(row)}</button>
    {row.owner && <small>{row.owner}</small>}
  </th>;
}
