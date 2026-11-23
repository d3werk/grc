import { useState } from 'react';
import { ArrowRight, ChartNoAxesCombined, ClipboardCheck, Download, Fingerprint, LayoutDashboard, Network, TriangleAlert } from 'lucide-react';
import type { Client, DashboardData, Kind, Status } from './api';
import { download, errorMessage, query } from './api';
import { kindInfo, statusLabels } from './Catalog';
import type { Page } from './Workspace';
import { dateLabel, Empty, Help, Loading, Notice, useData } from './ui';

type Navigate = (page: Page, id?: string, create?: boolean) => void;

export function Dashboard({ api, scope, contextLabel, permissions, navigate }: {
  api: Client; scope: string; contextLabel: string; permissions: ReadonlySet<string>; navigate: Navigate;
}) {
  const result = useData(signal => api.json<DashboardData>(`/dashboard${query({ scope })}`, { signal }), [api, scope]);
  const data = result.data;
  const steps: { kind: Kind; label: string }[] = [
    { kind: 'org_unit', label: 'Organisationseinheit anlegen' },
    { kind: 'asset', label: 'Asset erfassen' },
    { kind: 'it_service', label: 'IT-Dienst anlegen' },
    { kind: 'risk', label: 'Risiko erfassen' },
    { kind: 'control', label: 'Control erfassen' },
    { kind: 'measure', label: 'Maßnahme planen' },
    { kind: 'processing_activity', label: 'Verarbeitung erfassen' }
  ];
  const next = data && steps.find(item => !data.record_counts[item.kind]);
  const hasNext = next && permissions.has('records.write');
  const canEvidence = Boolean(data && !data.evidence?.total && permissions.has('evidence.write') && permissions.has('evidence.read'));
  const canMatrix = permissions.has('reviews.read');

  return <section className="page dashboard">
    <header className="hero">
      <div className="hero-copy"><div className="hero-context"><LayoutDashboard size={17} aria-hidden="true" /> {contextLabel}</div>
        <h1>Projektüberblick</h1>
        <p>Bestand, Risiken, Kontrollen und Nachweise im selben Geltungsbereich.</p>
      </div>
      <div className="hero-actions">
        <button type="button" className="light-button" onClick={() => navigate('asset')}><Network size={17} aria-hidden="true" /> Assetregister</button>
        {canMatrix && <button className="hero-outline" type="button" onClick={() => navigate('risk_matrix')}>
          <TriangleAlert size={17} aria-hidden="true" /> Risikomatrix</button>}
      </div>
    </header>
    {result.loading ? <Loading /> : result.error ? <Notice message={result.error} /> : data && <>
      <div className="section-heading"><h2>Register im Überblick</h2><small>Stand {dateLabel(data.as_of)}</small></div>
      <div className="metric-grid">
        {(['asset', 'risk', 'control', 'measure'] as const).map(kind => {
          const meta = kindInfo[kind];
          const Icon = meta.icon;
          const count = data.record_counts[kind] ?? 0;
          return <button type="button" className={`metric ${kind}`} key={kind} onClick={() => navigate(kind)}
            aria-label={`${meta.title} öffnen, ${count} ${count === 1 ? 'Eintrag' : 'Einträge'}`}>
            <span className="metric-top"><span className="glyph"><Icon size={20} aria-hidden="true" /></span><ArrowRight size={16} aria-hidden="true" /></span>
            <strong>{count}</strong><span>{meta.title}</span>
          </button>;
        })}
      </div>
      <div className="dashboard-grid">
        <section className="panel summary-panel">
          <div className="panel-title">
            <h2><TriangleAlert size={20} aria-hidden="true" /> Risikobewertungen</h2>
            <Help label="Berechnung der Risikokennzahlen">Die Auswertung verwendet je Risiko die letzte Bewertung. Als älter gilt eine Bewertung aus einer früheren Risikofassung.</Help>
          </div>
          {data.risk ? <><dl className="stat-list">
            <div><dt>Mit Bewertung</dt><dd>{data.risk.assessed}</dd></div>
            <div><dt>Ohne Bewertung</dt><dd>{data.risk.unassessed}</dd></div>
            <div><dt>Mit älterem Risikostand</dt><dd>{data.risk.stale}</dd></div>
          </dl><button className="link-button" type="button" onClick={() => navigate('risk_matrix')}>Matrix anzeigen <ArrowRight size={16} aria-hidden="true" /></button></>
            : <p className="muted">Für Bewertungen besteht keine Leseberechtigung.</p>}
        </section>
        <section className="panel summary-panel">
          <div className="panel-title">
            <h2><ClipboardCheck size={20} aria-hidden="true" /> Nachweislage</h2>
            <Help label="Berechnung der Nachweiskennzahlen">Ein verknüpfter Nachweis zählt bis einschließlich seines Gültigkeitsdatums. Nachweise ohne Enddatum zählen ebenfalls.</Help>
          </div>
          {data.evidence ? <><dl className="stat-list">
            <div><dt>Erfasste Nachweise</dt><dd>{data.evidence.total}</dd></div>
            <div><dt>Abgelaufene Nachweise</dt><dd>{data.evidence.expired}</dd></div>
            <div><dt>Controls ohne gültigen Nachweis</dt><dd>{data.evidence.controls_without_current}</dd></div>
          </dl><button className="link-button" type="button" onClick={() => navigate('evidence')}>Nachweise anzeigen <ArrowRight size={16} aria-hidden="true" /></button></>
            : <p className="muted">Für Nachweise besteht keine Leseberechtigung.</p>}
        </section>
      </div>
      {(hasNext || canEvidence) && <section className="panel next-step">
        <div><h2>Nächster Schritt</h2><p>Beginnen Sie mit dem ersten fehlenden Registereintrag im aktuellen Filter.</p></div>
        <button type="button" className="primary" onClick={() => hasNext ? navigate(next.kind, undefined, true) : navigate('evidence')}>
          {hasNext ? next.label : 'Nachweis erfassen'} <ArrowRight size={16} aria-hidden="true" /></button>
      </section>}
    </>}
  </section>;
}

interface PrivacyReportData {
  scope: string | null;
  as_of: string;
  activities: {
    id: string;
    code: string;
    title: string;
    scope: string;
    status: Status;
    owner: string;
    attributes: Record<string, string>;
    relations: { relation: string; id: string; kind: Kind; code: string; title: string; scope: string; status: Status }[];
  }[];
}

export function Reports({ api, scope, catalogRead, navigate }: { api: Client; scope: string; catalogRead: boolean; navigate: Navigate }) {
  const [selected, setSelected] = useState<'management' | 'privacy'>('management');
  const [management, setManagement] = useState<DashboardData>();
  const [privacy, setPrivacy] = useState<PrivacyReportData>();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  async function getReport(format: 'json' | 'csv') {
    setPending(true);
    setError('');
    try {
      const path = `/reports/${selected}${query({ scope, format })}`;
      if (format === 'csv') download(await api.file(path), selected === 'privacy' ? 'D3-GRC-Datenschutz.csv' : 'D3-GRC-Management.csv');
      else if (selected === 'privacy') setPrivacy(await api.json<PrivacyReportData>(path));
      else setManagement(await api.json<DashboardData>(path));
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  return <section className="page">
    <header className="page-heading"><div className="heading-text"><span className="glyph"><ChartNoAxesCombined aria-hidden="true" /></span>
      <div><h1>Berichte</h1><p>Berichtsstand für den ausgewählten Geltungsbereich abrufen oder als CSV speichern.</p></div></div></header>
    <div className="actions report-switch" role="group" aria-label="Berichtsauswahl">
      <button type="button" aria-pressed={selected === 'management'} disabled={pending} onClick={() => { setSelected('management'); setError(''); }}>
        <ChartNoAxesCombined size={17} aria-hidden="true" /> Management</button>
      <button type="button" aria-pressed={selected === 'privacy'} disabled={pending} onClick={() => { setSelected('privacy'); setError(''); }}>
        <Fingerprint size={17} aria-hidden="true" /> Datenschutz</button></div>
    <section className="panel report-panel">
      <div className="panel-title"><h2>{selected === 'privacy' ? 'Datenschutzbericht' : 'Managementbericht'}</h2>
        <div className="actions"><button type="button" disabled={pending} onClick={() => void getReport('json')}>Bericht anzeigen</button>
          <button type="button" className="primary" disabled={pending} onClick={() => void getReport('csv')}><Download size={17} aria-hidden="true" /> CSV speichern</button></div></div>
      {error && <Notice message={error} />}
      {pending && <Loading />}
      {selected === 'management' && (!management ? <Empty title="Bericht abrufen">Die Kennzahlen werden aus den Registern für den Berichtsstand ermittelt.</Empty> :
        <div className="table-scroll"><table><caption>Registerbestand am {dateLabel(management.as_of)}</caption><thead><tr>
          <th scope="col">Register</th><th scope="col">Anzahl</th></tr></thead>
          <tbody>{Object.entries(management.record_counts).map(([kind, value]) => <tr key={kind}>
            <th scope="row">{kindInfo[kind as Kind]?.title ?? kind}</th><td>{value}</td></tr>)}</tbody></table></div>)}
      {selected === 'privacy' && (!privacy ? <Empty title="Bericht abrufen">
        Enthält Verarbeitungen mit den freigegebenen Bezügen zu Assets, Controls, Prozessen und Folgenabschätzungen.</Empty>
        : privacy.activities.length === 0 ? <Empty title="Keine Verarbeitungen im Bericht" /> :
          <div className="table-scroll"><table><caption>Verarbeitungsverzeichnis am {dateLabel(privacy.as_of)}</caption><thead><tr>
            <th scope="col">Verarbeitung</th><th scope="col">Zweck und Rechtsgrundlage</th><th scope="col">Bezüge</th></tr></thead>
            <tbody>{privacy.activities.map(activity => <tr key={activity.id}><th scope="row">
              {catalogRead ? <button type="button" className="link-button" onClick={() => navigate('processing_activity', activity.id)}>{activity.title}</button>
                : activity.title}
              <small>{activity.scope || 'Ohne Bereich'} · {statusLabels[activity.status]}</small></th>
              <td><p>{activity.attributes.purpose || '–'}</p><small>{activity.attributes.legal_basis}</small></td>
              <td>{activity.relations.length ? <ul className="plain-list">{activity.relations.map(relation => <li key={`${relation.relation}:${relation.id}`}>
                <small>{kindInfo[relation.kind]?.singular}</small> {catalogRead
                  ? <button className="link-button" type="button" onClick={() => navigate(relation.kind === 'control' ? 'tom' : relation.kind, relation.id)}>
                    {relation.title}</button> : relation.title}</li>)}</ul> : '–'}</td></tr>)}</tbody>
          </table></div>)}
    </section>
  </section>;
}
