import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { ClipboardCheck, Download, Eye, Grid2X2, Link2, Plus, UploadCloud } from 'lucide-react';
import type { Assessment, AssessmentInput, CatalogRecord, Client, Evidence, MatrixData } from './api';
import { download, errorMessage, evidenceTargetKinds, query } from './api';
import { kindInfo } from './Catalog';
import { RichTextEditor, RichTextPreview } from './RichText';
import type { RichTextDocument } from './RichText';
import { ScopeSelect } from './ScopeSelect';
import { dateLabel, Empty, Field, Loading, Notice, todayIso, useData } from './ui';

const responseLabels = {
  undecided: 'Noch offen', mitigate: 'Mindern', avoid: 'Vermeiden', transfer: 'Übertragen', accept: 'Akzeptieren'
};
const evidenceTargetLabel = (record: CatalogRecord) => `${kindInfo[record.kind].singular} · ${record.title}`;
const likelihood = ['Sehr unwahrscheinlich', 'Unwahrscheinlich', 'Möglich', 'Wahrscheinlich', 'Sehr wahrscheinlich'];
const impact = ['Sehr gering', 'Gering', 'Mittel', 'Hoch', 'Sehr hoch'];

function riskBand(score: number): [string, string] {
  if (score <= 4) return ['Niedrig', 'low'];
  if (score <= 9) return ['Mittel', 'medium'];
  if (score <= 15) return ['Hoch', 'high'];
  return ['Kritisch', 'critical'];
}

export function RiskAssessments({ api, record, write, read }: { api: Client; record: CatalogRecord; write: boolean; read: boolean }) {
  const path = `/risks/${record.id}/assessments`;
  const result = useData(signal => read ? api.json<Assessment[]>(path, { signal }) : Promise.resolve(null), [api, path, read]);
  const [form, setForm] = useState(false);
  const [residual, setResidual] = useState(false);
  const [rationale, setRationale] = useState<{ text: string; document?: RichTextDocument }>({ text: '' });
  const [requestError, setRequestError] = useState('');
  const [pending, setPending] = useState(false);

  function closeForm() {
    setForm(false);
    setResidual(false);
    setRationale({ text: '' });
    setRequestError('');
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    const reason = rationale.text.trim();
    if (!reason) {
      setRequestError('Geben Sie eine Begründung ein.');
      return;
    }
    const values = new FormData(event.currentTarget);
    const payload: AssessmentInput = {
      risk_version: record.version,
      assessed_on: String(values.get('assessed_on')),
      inherent_likelihood: Number(values.get('inherent_likelihood')),
      inherent_impact: Number(values.get('inherent_impact')),
      residual_likelihood: residual ? Number(values.get('residual_likelihood')) : null,
      residual_impact: residual ? Number(values.get('residual_impact')) : null,
      response: String(values.get('response')),
      rationale: reason,
      rationale_rich_text: rationale.document
    };
    setPending(true);
    setRequestError('');
    try {
      await api.json<Assessment>(path, { method: 'POST', body: JSON.stringify(payload) });
      closeForm();
      result.refresh();
    } catch (reason) {
      setRequestError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  function scale(label: string, name: string, terms: string[]) {
    return <Field label={label}><select name={name} required defaultValue="">
      <option value="" disabled>Wert auswählen</option>{terms.map((term, index) =>
        <option key={term} value={index + 1}>{index + 1} · {term}</option>)}</select></Field>;
  }

  return <section className="detail-section">
    <div className="panel-title"><h3><ClipboardCheck size={17} aria-hidden="true" /> Risikobewertungen</h3>
      {write && !form && <button type="button" onClick={() => setForm(true)}><Plus size={16} aria-hidden="true" /> Risiko bewerten</button>}</div>
    {result.loading && <Loading />}
    {result.error && <Notice message={result.error} />}
    {result.data?.length === 0 && <p className="muted">Für dieses Risiko wurde noch keine Bewertung erfasst.</p>}
    {result.data && result.data.length > 0 && <ul className="assessment-list">
      {result.data.map((row, index) => <li key={row.id}>
        <div className="panel-title"><strong>{index === 0 ? 'Aktuelle Bewertung' : 'Frühere Bewertung'}</strong><small>{dateLabel(row.assessed_on)}</small></div>
        {row.risk_version !== record.version && <p className="muted small">Bewertung aus Risikofassung {row.risk_version}.</p>}
        <dl className="record-meta">
          <div><dt>Ausgangsrisiko</dt><dd>{row.inherent_likelihood} × {row.inherent_impact} · {row.inherent_score} · {riskBand(row.inherent_score)[0]}</dd></div>
          <div><dt>Restrisiko</dt><dd>{row.residual_score === null ? 'Offen' : `${row.residual_likelihood} × ${row.residual_impact} · ${row.residual_score} · ${riskBand(row.residual_score)[0]}`}</dd></div>
        </dl>
        <p>{responseLabels[row.response as keyof typeof responseLabels] ?? row.response}</p>
        <RichTextPreview text={row.rationale} document={row.rationale_rich_text} />
      </li>)}
    </ul>}
    {form && <form className="inline-form" onSubmit={submit}>
      {requestError && <Notice message={requestError} />}
      <Field label="Bewertungsstichtag"><input name="assessed_on" type="date" required max={todayIso()} defaultValue={todayIso()} /></Field>
      <h4>Ausgangsrisiko</h4>
      <div className="form-grid">{scale('Eintrittswahrscheinlichkeit', 'inherent_likelihood', likelihood)}
        {scale('Auswirkung', 'inherent_impact', impact)}</div>
      <label className="check"><input type="checkbox" checked={residual} onChange={event => setResidual(event.target.checked)} /> Restrisiko ergänzen</label>
      {residual && <div className="form-grid">{scale('Verbleibende Eintrittswahrscheinlichkeit', 'residual_likelihood', likelihood)}
        {scale('Verbleibende Auswirkung', 'residual_impact', impact)}</div>}
      <Field label="Risikobehandlung"><select name="response">{Object.entries(responseLabels).map(([key, label]) =>
        <option key={key} value={key}>{label}</option>)}</select></Field>
      <RichTextEditor label="Begründung" value={rationale.text} document={rationale.document}
        onChange={(text, document) => setRationale({ text, document })} disabled={pending} compact />
      <div className="actions"><button className="primary" type="submit" disabled={pending}>Bewertung speichern</button>
        <button type="button" disabled={pending} onClick={closeForm}>Abbrechen</button></div>
    </form>}
  </section>;
}

export function RiskMatrix({ api, scope }: { api: Client; scope: string }) {
  const [stage, setStage] = useState<'inherent' | 'residual'>('inherent');
  const result = useData(signal => api.json<MatrixData>(`/risk-matrix${query({ scope, stage })}`, { signal }), [api, scope, stage]);
  return <section className="page">
    <header className="page-heading"><div className="heading-text"><span className="glyph"><Grid2X2 aria-hidden="true" /></span>
      <div><h1>Risikomatrix</h1><p>Aktuelle Bewertungen nach Eintrittswahrscheinlichkeit und Auswirkung.</p></div></div>
      <Field label="Bewertungssicht"><select value={stage} onChange={event => setStage(event.target.value as typeof stage)}>
        <option value="inherent">Ausgangsrisiko</option><option value="residual">Restrisiko</option></select></Field></header>
    {result.loading ? <Loading /> : result.error ? <Notice message={result.error} /> : result.data && <div className="panel matrix">
      <p>{result.data.total} Risiken · {result.data.unassessed} ohne Werte in dieser Sicht · {result.data.stale} mit älterem Risikostand</p>
      <div className="table-scroll"><table aria-label="Risikomatrix nach Wahrscheinlichkeit und Auswirkung">
        <thead><tr><th scope="col">Auswirkung</th>{likelihood.map((word, index) =>
          <th key={word} scope="col" title={word}>{index + 1}</th>)}</tr></thead>
        <tbody>{[5, 4, 3, 2, 1].map(effect => <tr key={effect}><th scope="row">{effect} {impact[effect - 1]}</th>
          {[1, 2, 3, 4, 5].map(chance => {
            const [label, color] = riskBand(chance * effect);
            const count = result.data?.cells.find(cell => cell.likelihood === chance && cell.impact === effect)?.count;
            return <td className={color} key={chance}><strong>{count ?? '–'}</strong><small>{label}</small></td>;
          })}</tr>)}</tbody>
      </table></div>
      <p className="muted">Spalten zeigen die Eintrittswahrscheinlichkeit von 1 bis 5. Die Zahl je Zelle gibt die Anzahl der Risiken an.</p>
    </div>}
  </section>;
}

export function EvidenceView({ api, scope, records, write, targetId, startUpload, onBack }: {
  api: Client; scope: string; records: CatalogRecord[]; write: boolean;
  targetId?: string; startUpload?: boolean; onBack?: () => void;
}) {
  const recordId = targetId;
  const result = useData(signal => api.json<Evidence[]>(`/evidence${query({ scope, record_id: recordId })}`, { signal }), [api, scope, recordId]);
  const [form, setForm] = useState(Boolean(startUpload) && write);
  const [kind, setKind] = useState<'file' | 'external'>('file');
  const [activeId, setActiveId] = useState<string>();
  const [linkId, setLinkId] = useState('');
  const [uploadTarget, setUploadTarget] = useState(targetId ?? '');
  const [preview, setPreview] = useState<{ id: string; text: string } | { id: string; image: string }>();
  const [requestError, setRequestError] = useState('');
  const [pending, setPending] = useState(false);
  const targets = records.filter(record => evidenceTargetKinds.has(record.kind));
  const contextRecord = targets.find(record => record.id === targetId);
  const active = result.data?.find(item => item.id === activeId);
  const linked = targets.find(record => record.id === uploadTarget);
  const availableTargets = targets.filter(item => item.scope === active?.scope && !active?.linked_record_ids.includes(item.id));

  useEffect(() => () => {
    if (preview && 'image' in preview) URL.revokeObjectURL(preview.image);
  }, [preview]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const fields = new FormData(event.currentTarget);
    if (!fields.get('record_id')) fields.delete('record_id');
    if (!fields.get('valid_until')) fields.delete('valid_until');
    setPending(true);
    setRequestError('');
    try {
      const file = fields.get('file');
      if (file instanceof File && file.size > 4 * 1024 * 1024) throw new Error('size');
      const evidence = await api.json<Evidence>(kind === 'file' ? '/evidence/upload' : '/evidence', kind === 'file'
        ? { method: 'POST', body: fields }
        : { method: 'POST', body: JSON.stringify(Object.fromEntries(fields)) });
      setActiveId(evidence.id);
      setPreview(undefined);
      setUploadTarget('');
      setKind('file');
      setForm(false);
      result.refresh();
    } catch (failure) {
      setRequestError(failure instanceof Error && failure.message === 'size' ? 'Die Datei darf höchstens 4 MiB groß sein.' : errorMessage(failure));
    } finally {
      setPending(false);
    }
  }

  async function openFile(item: Evidence, mode: 'preview' | 'download') {
    setPending(true);
    setRequestError('');
    try {
      const file = await api.file(`/evidence/${item.id}/file`);
      if (mode === 'download') download(file, item.filename ?? 'Nachweis');
      else if (file.type === 'image/png') setPreview({ id: item.id, image: URL.createObjectURL(file) });
      else setPreview({ id: item.id, text: await file.text() });
    } catch (reason) {
      setRequestError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  async function link(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!active) return;
    setPending(true);
    setRequestError('');
    try {
      await api.json(`/evidence/${active.id}/links`, { method: 'POST', body: JSON.stringify({ record_id: linkId }) });
      setLinkId('');
      result.refresh();
    } catch (reason) {
      setRequestError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  return <section className="page">
    <header className="page-heading"><div className="heading-text"><span className="glyph"><ClipboardCheck aria-hidden="true" /></span>
      <div><h1>Nachweise</h1><p>{contextRecord ? evidenceTargetLabel(contextRecord) : 'Prüfdateien und Referenzen verwalten und mit Facheinträgen verknüpfen.'}</p></div></div>
      <div className="actions">
        {onBack && <button type="button" onClick={onBack}>Zurück zum Eintrag</button>}
        {write && <button className="primary" type="button" onClick={() => {
          setUploadTarget(targetId ?? '');
          setForm(true);
        }}><UploadCloud size={18} aria-hidden="true" /> Nachweis erfassen</button>}
      </div></header>
    {requestError && <Notice message={requestError} />}
    {form && <form className="panel edit-panel" onSubmit={save}>
      <h2>Nachweis erfassen</h2>
      <div className="form-grid">
        <Field label="Art"><select value={kind} onChange={event => setKind(event.target.value as typeof kind)}>
          <option value="file">Datei</option><option value="external">Externe Referenz</option></select></Field>
        <Field label="Titel"><input name="title" required /></Field>
        <Field label="Herkunft"><input name="origin" required /></Field>
        <Field label="Verknüpfter Eintrag"><select name="record_id" value={uploadTarget}
          disabled={Boolean(contextRecord)} onChange={event => setUploadTarget(event.target.value)}>
          <option value="">Bezug später festlegen</option>{targets.map(target => <option value={target.id} key={target.id}>{evidenceTargetLabel(target)}</option>)}</select></Field>
        {contextRecord && <input type="hidden" name="record_id" value={contextRecord.id} />}
        <Field label="Geltungsbereich" hint={linked ? 'Der Bereich wird aus dem verknüpften Eintrag übernommen.' : undefined}>
          <ScopeSelect key={uploadTarget} mode="evidence" name="scope" defaultValue={linked?.scope ?? scope} disabled={Boolean(linked)} />
        </Field>
        {linked && <input type="hidden" name="scope" value={linked.scope} />}
        <Field label="Erfasst am"><input name="collected_on" type="date" required max={todayIso()} defaultValue={todayIso()} /></Field>
        <Field label="Gültig bis"><input name="valid_until" type="date" /></Field>
      </div>
      {kind === 'file' ? <Field label="Datei" hint="PNG und UTF-8-Text (.txt, .log), höchstens 4 MiB">
        <input name="file" type="file" required accept=".png,.txt,.log" /></Field>
        : <Field label="HTTPS-Referenz"><input name="url" required type="url" placeholder="https://…" /></Field>}
      <div className="actions"><button className="primary" type="submit" disabled={pending}>Nachweis speichern</button>
        <button type="button" disabled={pending} onClick={() => setForm(false)}>Abbrechen</button></div>
    </form>}
    {!form && <div className="register-layout">
      <section className="panel">
        <h2>Bestand</h2>
        {result.loading ? <Loading /> : result.error ? <Notice message={result.error} /> : result.data?.length
          ? <div className="table-scroll"><table><thead><tr><th scope="col">Nachweis</th><th scope="col">Gültig bis</th></tr></thead>
            <tbody>{result.data.map(item => <tr key={item.id} className={item.id === activeId ? 'selected' : ''}>
              <td><button type="button" className="row-button" aria-pressed={item.id === activeId} onClick={() => {
                setActiveId(item.id); setPreview(undefined); setLinkId(''); setRequestError('');
              }}><strong>{item.title}</strong><small>{item.filename ?? 'Externe Referenz'}</small></button></td>
              <td>{dateLabel(item.valid_until)}</td></tr>)}</tbody>
          </table></div> : <Empty title="Noch keine Nachweise">{recordId
            ? 'Diesem Eintrag ist kein Nachweis zugeordnet.' : 'Im aktuellen Geltungsbereich gibt es keine erfassten Nachweise.'}</Empty>}
      </section>
      <section className="panel record-detail">
        {active ? <><h2>{active.title}</h2><dl className="record-meta">
          <div><dt>Geltungsbereich</dt><dd>{active.scope}</dd></div><div><dt>Herkunft</dt><dd>{active.origin}</dd></div>
          <div><dt>Erfasst</dt><dd>{dateLabel(active.collected_on)}</dd></div><div><dt>Gültig bis</dt><dd>{dateLabel(active.valid_until)}</dd></div>
        </dl>
          {active.kind === 'file' ? <div className="actions">
            <button type="button" disabled={pending} onClick={() => void openFile(active, 'preview')}><Eye size={16} aria-hidden="true" /> Vorschau</button>
            <button type="button" disabled={pending} onClick={() => void openFile(active, 'download')}><Download size={16} aria-hidden="true" /> Datei speichern</button>
          </div> : active.url && <a className="break-text" href={active.url} target="_blank" rel="noopener noreferrer">{active.url}</a>}
          {preview?.id === active.id && ('image' in preview ? <img className="evidence-image" src={preview.image} alt={active.title} /> :
            <pre className="evidence-text">{preview.text}</pre>)}
          <section className="detail-section"><h3><Link2 size={17} aria-hidden="true" /> Zugeordnete Einträge</h3>
            {active.linked_record_ids.length > 0 ? <ul className="plain-list">{active.linked_record_ids.map(id => {
              const target = targets.find(item => item.id === id);
              return <li key={id}>{target ? evidenceTargetLabel(target) : 'Eintrag außerhalb des aktuellen Filters'}</li>;
            })}</ul>
              : <p className="muted">Noch keine Zuordnungen.</p>}
            {write && availableTargets.length > 0 && <form className="inline-form" onSubmit={link}>
              <Field label="Eintrag zuordnen"><select required value={linkId} onChange={event => setLinkId(event.target.value)}>
                <option value="">Eintrag auswählen</option>{availableTargets.map(item => <option key={item.id} value={item.id}>{evidenceTargetLabel(item)}</option>)}</select></Field>
              <button className="primary" type="submit" disabled={pending || !linkId}>Zuordnung speichern</button>
            </form>}
          </section></> : <Empty title="Nachweis auswählen">Datei, Herkunft, Gültigkeit und Bezüge ansehen.</Empty>}
      </section>
    </div>}
  </section>;
}
