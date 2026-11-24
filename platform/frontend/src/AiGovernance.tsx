import { ArrowRight, Bot, CalendarClock, ClipboardCheck, TriangleAlert } from 'lucide-react';
import type { ModuleDefinition, ModuleOverviewProps } from './ModuleWorkspace';
import { dateLabel, todayIso } from './ui';

export const aiKinds = {
  ai_system: {
    title: 'KI-Systeme',
    singular: 'KI-System',
    icon: Bot,
    description: 'Einsatz, Modell, Daten, Risiken und Freigabe eines KI-Systems dokumentieren.',
  },
};

function AiOverview({ rows, openRecord }: ModuleOverviewProps) {
  const systems = rows.filter(record => record.kind === 'ai_system');
  const today = todayIso();
  const ongoing = systems.filter(record => record.status !== 'archived' && record.status !== 'completed');
  const overdue = ongoing.filter(record => record.attributes.review_due && record.attributes.review_due < today)
    .sort((a, b) => a.attributes.review_due.localeCompare(b.attributes.review_due) || a.title.localeCompare(b.title, 'de'));
  const metrics = [
    { label: 'Registrierte KI-Systeme', count: systems.length, icon: Bot },
    { label: 'In Prüfung', count: ongoing.filter(record => record.status === 'in_review').length, icon: ClipboardCheck },
    { label: 'Hohes oder kritisches Risiko', count: ongoing.filter(record =>
      ['Hoch', 'Kritisch'].includes(record.attributes.risk_class)).length, icon: TriangleAlert },
    { label: 'Überfällige Überprüfungen', count: overdue.length, icon: CalendarClock },
  ];

  return <>
    <section className="metric-grid" aria-label="Stand der KI-Governance">
      {metrics.map(({ label, count, icon: Icon }) =>
        <div className="panel metric" key={label}>
          <span className="metric-top"><Icon size={20} aria-hidden="true" /></span>
          <strong>{count}</strong><span>{label}</span>
        </div>)}
    </section>
    {overdue.length > 0 && <section className="panel" aria-labelledby="ai-reviews-heading">
      <h2 id="ai-reviews-heading">Überfällige Überprüfungen</h2>
      <div className="table-scroll"><table>
        <thead><tr><th scope="col">KI-System</th><th scope="col">Verantwortung</th><th scope="col">Prüftermin</th></tr></thead>
        <tbody>{overdue.map(record => <tr key={record.id}>
          <td><button type="button" className="link-button" onClick={() => openRecord('ai_system', record.id)}>
            {record.title}<ArrowRight size={15} aria-hidden="true" /></button></td>
          <td>{record.owner || '–'}</td>
          <td><time dateTime={record.attributes.review_due}>{dateLabel(record.attributes.review_due)}</time></td>
        </tr>)}</tbody>
      </table></div>
    </section>}
  </>;
}

export const aiModule: ModuleDefinition = {
  title: 'KI-Governance',
  description: 'KI-Einsätze erfassen, fachlich einstufen und Einsatzentscheidungen sowie laufende Überprüfungen dokumentieren.',
  overview: AiOverview,
  sections: [{
    kind: 'ai_system',
    label: 'KI-Inventar',
    description: 'Verantwortliche Stellen erfassen Risiken, Aufsicht, Freigabe und Prüftermine. Risiken, Controls und Anforderungen lassen sich mit bestehenden Registereinträgen verknüpfen.',
    columns: [
      { key: 'risk_class', label: 'Interne Risikoklasse' },
      { key: 'decision', label: 'Einsatzentscheidung' },
      { key: 'review_due', label: 'Nächste Überprüfung' },
    ],
  }],
};
