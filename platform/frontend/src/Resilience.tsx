import { Activity, ArrowRight, ClipboardCheck, Clock3, HeartPulse, ListChecks, Network } from 'lucide-react';
import type { CatalogRecord, Kind } from './api';
import { recordTitle } from './Catalog';
import type { ModuleDefinition, ModuleOverviewProps } from './ModuleWorkspace';
import { dateLabel, Empty, todayIso } from './ui';

export const resilienceKinds = {
  resilience_service: {
    title: 'Kritische Geschäftsdienste', singular: 'Geschäftsdienst', icon: Network,
    description: 'Auswirkungen einer Störung, Toleranzgrenzen und betriebliche Abhängigkeiten erfassen.'
  },
  resilience_scenario: {
    title: 'Szenariotests', singular: 'Szenariotest', icon: Activity,
    description: 'Störungen erproben, beobachtete Unterbrechungen bewerten und Maßnahmen verknüpfen.'
  },
  continuity_bia: {
    title: 'Business Impact Analysen', singular: 'Business Impact Analyse', icon: ListChecks,
    description: 'Auswirkungen auf Prozesse, Abhängigkeiten und Wiederanlaufziele beurteilen.'
  },
  continuity_plan: {
    title: 'Notfall- und Wiederanlaufpläne', singular: 'Wiederanlaufplan', icon: HeartPulse,
    description: 'Aktivierung, Sofortmaßnahmen, Eskalation und Wiederanlauf für den Störungsfall festlegen.'
  },
  continuity_exercise: {
    title: 'Notfallübungen', singular: 'Notfallübung', icon: ClipboardCheck,
    description: 'Pläne anhand von Übungen prüfen und Feststellungen mit Maßnahmen verbinden.'
  },
};

export const resilienceModule: ModuleDefinition = {
  title: 'Operationelle Resilienz',
  description: 'Kritische Geschäftsdienste und ihre Abhängigkeiten erfassen. Mit Szenariotests überprüfen, ob der Betrieb innerhalb der vereinbarten Toleranz bleibt.',
  overview: ResilienceOverview,
  sections: [
    {
      kind: 'resilience_service', label: 'Geschäftsdienste', description: resilienceKinds.resilience_service.description,
      columns: [{ key: 'tolerance_minutes', label: 'Zeittoleranz (Min.)' }, { key: 'next_review_on', label: 'Nächste Prüfung' }],
    },
    {
      kind: 'resilience_scenario', label: 'Szenariotests', description: resilienceKinds.resilience_scenario.description,
      columns: [{ key: 'scheduled_on', label: 'Testtermin' }, { key: 'result', label: 'Gesamturteil' }],
    },
  ],
};

export const continuityModule: ModuleDefinition = {
  title: 'Business Continuity',
  description: 'Ausfallfolgen und Wiederanlaufziele analysieren, Maßnahmen für den Notfall planen und die Pläne in Übungen erproben.',
  overview: ContinuityOverview,
  sections: [
    {
      kind: 'continuity_bia', label: 'Auswirkungsanalysen', description: resilienceKinds.continuity_bia.description,
      columns: [{ key: 'criticality', label: 'Kritikalität' }, { key: 'rto_minutes', label: 'RTO (Min.)' }],
    },
    {
      kind: 'continuity_plan', label: 'Wiederanlaufpläne', description: resilienceKinds.continuity_plan.description,
      columns: [{ key: 'released_on', label: 'Freigabe' }, { key: 'next_review_on', label: 'Nächste Prüfung' }],
    },
    {
      kind: 'continuity_exercise', label: 'Übungen', description: resilienceKinds.continuity_exercise.description,
      columns: [{ key: 'scheduled_on', label: 'Übungstermin' }, { key: 'result', label: 'Gesamturteil' }],
    },
  ],
};

interface Metric {
  label: string;
  count: number;
  kind: Kind;
  icon: typeof Network;
}

function measuredTimes(record: CatalogRecord) {
  const attributes = record.attributes;
  const [planned, actual] = record.kind === 'resilience_scenario'
    ? [attributes.tolerance_snapshot_minutes, attributes.disruption_minutes]
    : [attributes.rto_snapshot_minutes, attributes.recovery_minutes];
  return planned && actual ? { planned: Number(planned), actual: Number(actual) } : undefined;
}

function deadline(record: CatalogRecord) {
  if (record.status === 'archived') return undefined;
  if (record.kind === 'resilience_scenario' || record.kind === 'continuity_exercise') {
    return record.status === 'completed' ? undefined : record.attributes.scheduled_on;
  }
  return record.attributes.next_review_on;
}

function ReviewOverview({ items, tests, metrics, openRecord }: {
  items: CatalogRecord[];
  tests: CatalogRecord[];
  metrics: Metric[];
  openRecord: ModuleOverviewProps['openRecord'];
}) {
  const today = todayIso();
  const deadlines = items.flatMap(record => {
    const due = deadline(record);
    return due ? [{ record, due }] : [];
  }).sort((left, right) => left.due.localeCompare(right.due));
  const results = tests.filter(record => record.status === 'completed').sort((left, right) =>
    (right.attributes.performed_on ?? '').localeCompare(left.attributes.performed_on ?? ''));

  return <>
    <div className="metric-grid">
      {metrics.map(({ kind, label, count, icon: Icon }) => <button className="metric" type="button" key={label}
        onClick={() => openRecord(kind)}>
        <span className="metric-top"><span className="glyph"><Icon size={20} aria-hidden="true" /></span><ArrowRight size={16} aria-hidden="true" /></span>
        <strong>{count}</strong><span>{label}</span>
      </button>)}
    </div>
    <div className="dashboard-grid">
      <section className="panel summary-panel">
        <div className="panel-title"><h2><Clock3 size={20} aria-hidden="true" /> Anstehende und überfällige Termine</h2></div>
        {deadlines.length ? <div className="table-scroll"><table>
          <thead><tr><th scope="col">Eintrag</th><th scope="col">Termin</th></tr></thead>
          <tbody>{deadlines.slice(0, 6).map(({ record, due }) => <tr key={record.id}>
            <th scope="row"><button type="button" className="link-button" onClick={() => openRecord(record.kind, record.id)}>{recordTitle(record)}</button></th>
            <td>{dateLabel(due)}{due < today && <small>Überfällig</small>}</td>
          </tr>)}</tbody>
        </table></div> : <Empty title="Keine offenen Termine" />}
      </section>
      <section className="panel summary-panel">
        <div className="panel-title"><h2><ClipboardCheck size={20} aria-hidden="true" /> Letzte Ergebnisse</h2></div>
        {results.length ? <div className="table-scroll"><table>
          <thead><tr><th scope="col">Eintrag</th><th scope="col">Urteil und Zeitvergleich</th></tr></thead>
          <tbody>{results.slice(0, 6).map(record => {
            const times = measuredTimes(record);
            return <tr key={record.id}>
              <th scope="row"><button type="button" className="link-button" onClick={() => openRecord(record.kind, record.id)}>{recordTitle(record)}</button>
                <small>{dateLabel(record.attributes.performed_on)}</small></th>
              <td>{record.attributes.result}{times && <small>{times.actual} von {times.planned} Min. · {times.actual > times.planned ? 'Zeitgrenze überschritten' : 'Zeitgrenze eingehalten'}</small>}</td>
            </tr>;
          })}</tbody>
        </table></div> : <Empty title="Keine abgeschlossenen Prüfungen" />}
      </section>
    </div>
  </>;
}

function ResilienceOverview({ rows, openRecord }: ModuleOverviewProps) {
  const services = rows.filter(record => record.kind === 'resilience_service' && record.status !== 'archived');
  const tests = rows.filter(record => record.kind === 'resilience_scenario' && record.status !== 'archived');
  const breaches = tests.filter(record => {
    const times = measuredTimes(record);
    return record.status === 'completed' && times && times.actual > times.planned;
  }).length;
  return <ReviewOverview items={[...services, ...tests]} tests={tests} openRecord={openRecord} metrics={[
    { kind: 'resilience_service', label: 'Geschäftsdienste', count: services.length, icon: Network },
    { kind: 'resilience_scenario', label: 'Offene Szenariotests', count: tests.filter(record => record.status !== 'completed').length, icon: Activity },
    { kind: 'resilience_scenario', label: 'Zeitgrenzen überschritten', count: breaches, icon: Clock3 },
  ]} />;
}

function ContinuityOverview({ rows, openRecord }: ModuleOverviewProps) {
  const analyses = rows.filter(record => record.kind === 'continuity_bia' && record.status !== 'archived');
  const plans = rows.filter(record => record.kind === 'continuity_plan' && record.status !== 'archived');
  const exercises = rows.filter(record => record.kind === 'continuity_exercise' && record.status !== 'archived');
  return <ReviewOverview items={[...analyses, ...plans, ...exercises]} tests={exercises} openRecord={openRecord} metrics={[
    { kind: 'continuity_bia', label: 'Auswirkungsanalysen', count: analyses.length, icon: ListChecks },
    { kind: 'continuity_plan', label: 'Aktive Wiederanlaufpläne', count: plans.filter(record => record.status === 'active').length, icon: HeartPulse },
    { kind: 'continuity_exercise', label: 'Offene Übungen', count: exercises.filter(record => record.status !== 'completed').length, icon: ClipboardCheck },
  ]} />;
}
