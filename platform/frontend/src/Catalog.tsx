import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import type { FormEvent, RefObject } from 'react';
import { ArrowRight, BriefcaseBusiness, Building2, ClipboardList, FileText, Fingerprint, GitBranch, Link2,
  Plus, Search, Shield, TriangleAlert, Workflow } from 'lucide-react';
import type { AttributeField, CatalogRecord, Client, Kind, LinkKind, RecordLink, Status } from './api';
import { errorMessage, evidenceTargetKinds, query } from './api';
import { dateLabel, Empty, Field, Loading, Notice, useData } from './ui';
import { RiskAssessments } from './Operations';
import { formattingKey, RichTextEditor, RichTextPreview } from './RichText';
import type { RichTextDocument } from './RichText';
import { ScopeSelect } from './ScopeSelect';
import { BusinessProcessFacts, BusinessProcessFields, BusinessProcessRegister } from './BusinessProcesses';
import { aiKinds } from './AiGovernance';
import { auditKinds } from './InternalAudit';
import { contractKinds } from './Contracts';
import { resilienceKinds } from './Resilience';
import type { CatalogColumn } from './ModuleWorkspace';

export const kindInfo = {
  org_unit: { title: 'Organisation', singular: 'Organisationseinheit', icon: Building2, description: 'Einheiten und ihre Zugehörigkeit im Mandanten pflegen.' },
  asset: { title: 'Assets', singular: 'Asset', icon: BriefcaseBusiness, description: 'Informationswerte zentral erfassen und in Risiken, Controls und Verarbeitungen wiederverwenden.' },
  it_service: { title: 'IT-Dienste', singular: 'IT-Dienst', icon: Workflow, description: 'IT-Dienste einmal erfassen und mit Informationswerten, Risiken, Controls und Verarbeitungen verbinden.' },
  business_process: { title: 'Geschäftsprozesse', singular: 'Geschäftsprozess', icon: GitBranch, description: 'Prozesslandkarte, Rollen, Arbeitsschritte und betriebliche Abhängigkeiten pflegen.' },
  risk: { title: 'Risiken', singular: 'Risiko', icon: TriangleAlert, description: 'Risiken mit ihrem betroffenen Asset und den zugeordneten Controls erfassen und bewerten.' },
  control: { title: 'Controls', singular: 'Control', icon: Shield, description: 'Kontrollen für Risiken, Anforderungen und Datenschutz gemeinsam pflegen.' },
  measure: { title: 'Maßnahmen', singular: 'Maßnahme', icon: ClipboardList, description: 'Verantwortung, Umsetzung und Frist für Maßnahmen dokumentieren.' },
  framework_version: { title: 'Regelwerksfassungen', singular: 'Regelwerksfassung', icon: FileText, description: 'Regelwerke mit ihrer jeweiligen Fassung und ihren Anforderungen pflegen.' },
  requirement: { title: 'Anforderungen', singular: 'Anforderung', icon: ClipboardList, description: 'Anforderungen fachlich dokumentieren und mit Kontrollen verbinden.' },
  processing_activity: { title: 'Verzeichnis der Verarbeitungen', singular: 'Verarbeitung', icon: Fingerprint, description: 'Verarbeitungen erfassen und vorhandene IT-Dienste, Assets, Prozesse und TOM zuordnen.' },
  supplier: { title: 'Lieferanten', singular: 'Lieferant', icon: Building2, description: 'Dienstleister mit den betroffenen Assets, Risiken und Verarbeitungen verbinden.' },
  dpia: { title: 'Datenschutz-Folgenabschätzungen', singular: 'Folgenabschätzung', icon: Shield, description: 'Datenschutzrisiken, Schutzmaßnahmen und Prüfentscheidung für Verarbeitungen erfassen.' },
  ...auditKinds,
  ...resilienceKinds,
  ...aiKinds,
  ...contractKinds
} satisfies Record<Kind, { title: string; singular: string; icon: typeof Building2; description: string }>;

export const statusLabels: Record<Status, string> = {
  draft: 'Entwurf', active: 'Aktiv', in_review: 'In Prüfung', completed: 'Abgeschlossen', archived: 'Archiviert'
};

export function recordTitle(record: CatalogRecord) {
  return record.code ? `${record.code} · ${record.title}` : record.title;
}

function attributeValue(row: CatalogRecord, key: string, fields: AttributeField[]) {
  const value = row.attributes[key];
  if (!value) return '–';
  return fields.find(field => field.key === key)?.type === 'date' ? dateLabel(value) : value;
}

type Draft = Pick<CatalogRecord, 'title' | 'code' | 'description' | 'status' | 'scope' | 'owner' | 'org_unit_id' | 'attributes' | 'rich_text'>;
export type CatalogLeaveGuard = (leave: () => void) => void;
export type OpenRecord = (kind: Kind, id?: string, create?: boolean) => void;

const draftFields = ['title', 'code', 'description', 'status', 'scope', 'owner', 'org_unit_id'] as const;

interface CatalogProps {
  api: Client;
  kind: Kind;
  title?: string;
  description?: string;
  scope: string;
  rows: CatalogRecord[];
  relations: LinkKind[];
  attributeFields: AttributeField[];
  write: boolean;
  assess: boolean;
  reviewRead: boolean;
  selectedId?: string;
  create?: boolean;
  openRecord: OpenRecord;
  onEvidence?: (record: CatalogRecord, upload: boolean) => void;
  canUpload?: boolean;
  columns?: CatalogColumn[];
  reload: () => void;
  leaveGuard: RefObject<CatalogLeaveGuard | null>;
}

export function Catalog({ api, kind, title, description, scope, rows, relations, attributeFields, write, assess, reviewRead, selectedId, create,
  openRecord, onEvidence, canUpload, columns = [], reload, leaveGuard }: CatalogProps) {
  const meta = kindInfo[kind];
  const Icon = meta.icon;
  const [search, setSearch] = useState('');
  const [activeId, setActiveId] = useState<string | undefined>(selectedId);
  const [form, setForm] = useState<'new' | 'edit' | null>(create && write ? 'new' : null);
  const [linkRevision, setLinkRevision] = useState(0);
  const searchTerm = search.toLocaleLowerCase('de').trim();
  const visible = rows.filter(record => record.kind === kind &&
    [record.code, record.title, record.owner, record.description, ...Object.values(record.attributes)]
      .some(text => text.toLocaleLowerCase('de').includes(searchTerm)));
  const active = rows.find(record => record.kind === kind && record.id === activeId);
  const org = rows.find(record => record.id === active?.org_unit_id);

  return <section className="page">
    <header className="page-heading">
      <div className="heading-text"><span className="glyph"><Icon size={23} aria-hidden="true" /></span><div>
        <h1>{title ?? meta.title}</h1><p>{description ?? meta.description}</p>
      </div></div>
      {write && !form && <button className="primary" type="button" onClick={() => setForm('new')}>
        <Plus size={18} aria-hidden="true" /> {meta.singular} anlegen</button>}
    </header>
    {form && <CatalogForm api={api} kind={kind} initial={form === 'edit' ? active : undefined} scope={scope} attributeFields={attributeFields}
      orgs={rows.filter(record => record.kind === 'org_unit')} leaveGuard={leaveGuard} onCancel={() => setForm(null)}
      onSaved={record => { setActiveId(record.id); setForm(null); reload(); }} />}
    {!form && <div className="register-layout">
      {kind === 'business_process'
        ? <BusinessProcessRegister api={api} rows={rows} activeId={activeId} revision={linkRevision}
          onSelect={setActiveId} statusLabels={statusLabels} />
        : <section className="panel register-list" aria-labelledby="record-list-title">
        <div className="panel-title"><h2 id="record-list-title">Einträge</h2><span>{visible.length}</span></div>
        <label className="searchbox"><Search size={17} aria-hidden="true" />
          <input aria-label="Register durchsuchen" value={search} onChange={event => setSearch(event.target.value)} placeholder="Titel, Kürzel oder Text" /></label>
        {visible.length ? <div className="table-scroll"><table className={columns.length ? 'catalog-columns' : undefined}>
          <thead><tr><th scope="col">Eintrag</th><th scope="col">Status</th>
            {columns.map(column => <th key={column.key} scope="col">{column.label}</th>)}</tr></thead>
          <tbody>{visible.map(record => <tr key={record.id} className={record.id === activeId ? 'selected' : ''}>
            <td><button type="button" className="row-button" aria-pressed={record.id === activeId}
              onClick={() => setActiveId(record.id)}><strong>{record.title}</strong>
              {record.code && <small>{record.code}</small>}</button></td>
            <td><span className={`status ${record.status}`}>{statusLabels[record.status]}</span></td>
            {columns.map(column => <td key={column.key}>{attributeValue(record, column.key, attributeFields)}</td>)}
          </tr>)}</tbody>
        </table></div> : <Empty title={search ? 'Keine Treffer' : 'Noch keine Einträge'}>
          {search ? 'Verwenden Sie einen anderen Suchbegriff.' : 'Im aktuellen Geltungsbereich sind keine Einträge vorhanden.'}</Empty>}
      </section>}
      {active ? <section className="panel record-detail" aria-labelledby="record-detail-heading">
        <div className="panel-title"><div>
          <span className="muted">{active.code || meta.singular}</span><h2 id="record-detail-heading">{active.title}</h2>
        </div>{write && <button type="button" onClick={() => setForm('edit')}>Bearbeiten</button>}</div>
        <dl className="record-meta">
          <div><dt>Geltungsbereich</dt><dd>{active.scope || 'Ohne Angabe'}</dd></div>
          <div><dt>Organisation</dt><dd>{org?.title ?? '–'}</dd></div>
          <div><dt>Verantwortung</dt><dd>{active.owner || '–'}</dd></div>
          <div><dt>Bearbeitungsstand</dt><dd>{statusLabels[active.status]} · Fassung {active.version}</dd></div>
        </dl>
        {active.description && <div className="record-text"><RichTextPreview text={active.description} document={active.rich_text?.description} /></div>}
        {kind === 'business_process' ? <BusinessProcessFacts record={active} fields={attributeFields} />
          : Object.keys(active.attributes).length > 0 && <dl className="record-meta facts">
          {Object.entries(active.attributes).filter(([, value]) => value !== '').map(([key, value]) =>
            <div key={key}><dt>{attributeFields.find(field => field.key === key)?.label ?? key}</dt>
              <dd><RichTextPreview text={value} document={active.rich_text?.[key]} /></dd></div>)}</dl>}
        {kind === 'risk' && (assess || reviewRead) &&
          <RiskAssessments key={active.id} api={api} record={active} write={assess} read={reviewRead} />}
        <RecordRelations key={active.id} api={api} record={active} rows={rows} relations={relations}
          openRecord={openRecord} write={write} onChanged={() => setLinkRevision(value => value + 1)} />
        {evidenceTargetKinds.has(kind) && onEvidence && <div className="actions">
          <button type="button" onClick={() => onEvidence(active, false)}>Zugeordnete Nachweise ansehen</button>
          {canUpload && <button type="button" onClick={() => onEvidence(active, true)}>
            <Plus size={17} aria-hidden="true" /> Nachweis hinzufügen</button>}
        </div>}
      </section> : <section className="panel"><Empty title="Eintrag auswählen">
        Wählen Sie einen Eintrag, um Details und fachliche Bezüge zu öffnen.</Empty></section>}
    </div>}
  </section>;
}

function CatalogForm({ api, kind, initial, scope, orgs, attributeFields, onSaved, onCancel, leaveGuard }: {
  api: Client; kind: Kind; initial?: CatalogRecord; scope: string; orgs: CatalogRecord[];
  attributeFields: AttributeField[];
  onSaved: (record: CatalogRecord) => void; onCancel: () => void;
  leaveGuard: RefObject<CatalogLeaveGuard | null>;
}) {
  const [start] = useState<Draft>(() => initial ? {
    title: initial.title, code: initial.code, description: initial.description, status: initial.status,
    scope: initial.scope, owner: initial.owner, org_unit_id: initial.org_unit_id, attributes: initial.attributes,
    rich_text: initial.rich_text ?? {}
  } : { title: '', code: '', description: '', status: 'draft', scope, owner: '', org_unit_id: null, attributes: {}, rich_text: {} });
  const [draft, setDraft] = useState(start);
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);
  const [destination, setDestination] = useState<(() => void) | null>(null);
  const dialog = useRef<HTMLDialogElement>(null);
  const editor = useRef<HTMLFormElement>(null);
  const continueButton = useRef<HTMLButtonElement>(null);
  const [createKey] = useState(() => Array.from(crypto.getRandomValues(new Uint8Array(16)),
    value => value.toString(16).padStart(2, '0')).join(''));
  const changed = draftFields.some(key => draft[key] !== start[key]) || attributeFields.some(({ key }) =>
    (draft.attributes[key] ?? '') !== (start.attributes[key] ?? '')) || formattingKey(draft.rich_text) !== formattingKey(start.rich_text);

  const requestLeave: CatalogLeaveGuard = useCallback(leave => {
    if (pending) return;
    if (changed) setDestination(() => leave);
    else leave();
  }, [changed, pending]);

  useEffect(() => {
    leaveGuard.current = requestLeave;
    return () => { leaveGuard.current = null; };
  }, [leaveGuard, requestLeave]);

  useLayoutEffect(() => {
    if (destination && dialog.current && !dialog.current.open) {
      dialog.current.showModal();
      continueButton.current?.focus();
    }
  }, [destination]);

  function field<K extends keyof Draft>(key: K, value: Draft[K]) {
    setDraft(current => ({ ...current, [key]: value }));
  }

  function longText(key: string, text: string, document?: RichTextDocument) {
    setDraft(current => {
      const rich_text = { ...current.rich_text };
      if (document) rich_text[key] = document;
      else delete rich_text[key];
      return key === 'description' ? { ...current, description: text, rich_text }
        : { ...current, attributes: { ...current.attributes, [key]: text }, rich_text };
    });
  }

  async function save(leave?: () => void) {
    setPending(true);
    setError('');
    try {
      const payload = { ...draft, title: draft.title.trim() };
      const record = await api.json<CatalogRecord>(initial ? `/catalog/${initial.id}` : '/catalog', initial
        ? { method: 'PUT', body: JSON.stringify({ ...payload, expected_version: initial.version }) }
        : { method: 'POST', headers: { 'Idempotency-Key': createKey }, body: JSON.stringify({ ...payload, kind }) });
      onSaved(record);
      leave?.();
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  function closeDialog() {
    dialog.current?.close();
    setDestination(null);
  }

  function continueEditing() {
    closeDialog();
    editor.current?.querySelector('input')?.focus();
  }

  function finish(saveChanges: boolean) {
    const leave = destination;
    closeDialog();
    if (!leave) return;
    if (!saveChanges) leave();
    else if (editor.current?.reportValidity()) void save(leave);
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!pending) void save();
  }

  return <form ref={editor} className="panel edit-panel" aria-label={initial ? 'Eintrag bearbeiten' : 'Neuer Eintrag'} onSubmit={submit}>
    <h2>{initial ? 'Eintrag bearbeiten' : `${kindInfo[kind].singular} anlegen`}</h2>
    {error && <Notice message={error} />}
    <div className="form-grid">
      <Field label="Titel"><input required maxLength={300} value={draft.title} onChange={event => field('title', event.target.value)} /></Field>
      <Field label="Kürzel"><input maxLength={80} value={draft.code} onChange={event => field('code', event.target.value)} /></Field>
      <Field label={kind === 'org_unit' ? 'Übergeordnete Organisation' : 'Organisation'}><select value={draft.org_unit_id ?? ''}
        onChange={event => field('org_unit_id', event.target.value || null)}>
        <option value="">Ohne Zuordnung</option>{orgs.filter(org => org.id !== initial?.id).map(org =>
          <option key={org.id} value={org.id}>{recordTitle(org)}</option>)}</select></Field>
      <Field label="Geltungsbereich">
        <ScopeSelect value={draft.scope} onChange={event => field('scope', event.target.value)} /></Field>
      <Field label="Verantwortung"><input maxLength={240} value={draft.owner} onChange={event => field('owner', event.target.value)} /></Field>
      <Field label="Status"><select value={draft.status} onChange={event => field('status', event.target.value as Status)}>
        {Object.entries(statusLabels).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></Field>
    </div>
    <RichTextEditor label="Beschreibung" value={draft.description} document={draft.rich_text.description}
      onChange={(text, document) => longText('description', text, document)} disabled={pending} />
    {kind === 'business_process' ? <BusinessProcessFields fields={attributeFields}
      attributes={draft.attributes} richText={draft.rich_text} disabled={pending}
      onChange={(key, value) => field('attributes', { ...draft.attributes, [key]: value })}
      onRichChange={longText} /> : attributeFields.map(item => {
      const value = draft.attributes[item.key] ?? '';
      const update = (next: string) => field('attributes', { ...draft.attributes, [item.key]: next });
      if (item.type === 'textarea') return <RichTextEditor key={item.key} label={item.label}
        value={value} document={draft.rich_text[item.key]}
        onChange={(value, document) => longText(item.key, value, document)} disabled={pending} compact />;
      return <Field label={item.label} key={item.key}>
        {item.type === 'select' ? <select value={value} onChange={event => update(event.target.value)}>
          <option value="">Keine Angabe</option>
          {item.options?.map(option => <option key={option} value={option}>{option}</option>)}
        </select> : <input type={item.type} value={value} step={item.type === 'number' ? 'any' : undefined}
          maxLength={item.type === 'text' ? 5000 : undefined} onChange={event => update(event.target.value)} />}
      </Field>;
    })}
    <div className="actions"><button type="submit" className="primary" disabled={pending}>{pending ? 'Wird gespeichert …' : 'Speichern'}</button>
      <button type="button" onClick={() => requestLeave(onCancel)} disabled={pending}>Abbrechen</button></div>
    {destination && <dialog ref={dialog} className="panel" style={{ maxWidth: 'min(32rem, calc(100% - 2rem))' }}
      aria-labelledby="catalog-leave-title" aria-describedby="catalog-leave-help"
      onCancel={event => { event.preventDefault(); continueEditing(); }}>
      <h2 id="catalog-leave-title">Ungespeicherte Änderungen</h2>
      <p id="catalog-leave-help">Möchten Sie den Arbeitsstand speichern, bevor Sie die Bearbeitung verlassen?</p>
      <div className="actions">
        <button type="button" className="primary" onClick={() => finish(true)}>Speichern</button>
        <button type="button" onClick={() => finish(false)}>Verwerfen</button>
        <button ref={continueButton} type="button" onClick={continueEditing}>Weiter bearbeiten</button>
      </div>
    </dialog>}
  </form>;
}

function RecordRelations({ api, record, rows, relations, openRecord, write, onChanged }: {
  api: Client; record: CatalogRecord; rows: CatalogRecord[]; relations: LinkKind[]; openRecord: OpenRecord; write: boolean;
  onChanged: () => void;
}) {
  const links = useData(signal => api.json<RecordLink[]>(`/links${query({ record_id: record.id })}`, { signal }), [api, record.id]);
  const types = relations.filter(relation => relation.from_kind === record.kind || relation.to_kind === record.kind);
  const [form, setForm] = useState(false);
  const [linkType, setLinkType] = useState('');
  const [relatedId, setRelatedId] = useState('');
  const [reason, setReason] = useState('');
  const [richReason, setRichReason] = useState<RichTextDocument>();
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);
  const [removal, setRemoval] = useState<RecordLink>();
  const type = types.find(relation => relation.kind === linkType);
  const direction = type?.from_kind === record.kind;
  const targetKind = direction ? type?.to_kind : type?.from_kind;
  const options = rows.filter(candidate => candidate.kind === targetKind && candidate.id !== record.id &&
    (linkType !== 'process_parent' || candidate.scope === record.scope));
  useEffect(() => setRelatedId(''), [targetKind]);

  async function addLink(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!reason.trim()) { setError('Geben Sie eine Begründung ein.'); return; }
    setPending(true);
    setError('');
    try {
      await api.json('/links', { method: 'POST', body: JSON.stringify({
        kind: linkType, from_id: direction ? record.id : relatedId, to_id: direction ? relatedId : record.id,
        rationale: reason.trim(), rationale_rich_text: richReason
      }) });
      setForm(false);
      setReason('');
      setRichReason(undefined);
      links.refresh();
      onChanged();
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setPending(false);
    }
  }

  async function removeLink(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!removal) return;
    const reason = String(new FormData(event.currentTarget).get('reason') ?? '').trim();
    if (!reason) { setError('Geben Sie eine Begründung ein.'); return; }
    setPending(true);
    setError('');
    try {
      await api.json(`/links/${removal.id}`, { method: 'DELETE', body: JSON.stringify({
        expected_version: removal.version, reason,
      }) });
      setRemoval(undefined);
      links.refresh();
      onChanged();
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setPending(false);
    }
  }

  return <section className="detail-section">
    <div className="panel-title"><h3><Link2 size={17} aria-hidden="true" /> Bezüge</h3>
      {write && types.length > 0 && !form && !removal && <button type="button" onClick={() => {
        setError('');
        setForm(true);
      }}><Plus size={16} aria-hidden="true" /> Bezug anlegen</button>}</div>
    {links.loading && <Loading />}
    {links.error && <Notice message={links.error} />}
    {links.data?.length ? <ul className="relations">{links.data.map(link => {
      const related = rows.find(item => item.id === (link.from_id === record.id ? link.to_id : link.from_id));
      return <li key={link.id}><div><span>{relations.find(relation => relation.kind === link.kind)?.label ?? link.kind}</span>
        {related ? <button type="button" className="link-button" onClick={() => openRecord(related.kind, related.id)}>
          {recordTitle(related)} <ArrowRight size={15} aria-hidden="true" /></button> : <strong>Eintrag außerhalb des aktuellen Filters</strong>}
        <RichTextPreview text={link.rationale} document={link.rationale_rich_text} /></div>
        {write && !form && !removal && <button type="button" onClick={() => {
          setError('');
          setRemoval(link);
        }}>Bezug entfernen</button>}</li>;
    })}</ul> : !links.loading && !links.error && <p className="muted">Noch keine fachlichen Bezüge.</p>}
    {form && <form className="inline-form" onSubmit={addLink}>
      {error && <Notice message={error} />}
      <Field label="Beziehung"><select required value={linkType} onChange={event => { setLinkType(event.target.value); setRelatedId(''); }}>
        <option value="">Beziehung auswählen</option>{types.map(item => <option key={item.kind} value={item.kind}>{item.label}</option>)}</select></Field>
      <Field label="Bestehender Eintrag"><select required value={relatedId} disabled={!type} onChange={event => setRelatedId(event.target.value)}>
        <option value="">Eintrag auswählen</option>{options.map(item => <option key={item.id} value={item.id}>{recordTitle(item)}</option>)}</select></Field>
      {targetKind && !options.length && <p className="muted">Im aktuellen Filter ist kein passender Eintrag vorhanden.</p>}
      <RichTextEditor label="Begründung" value={reason} document={richReason} onChange={(text, document) => {
        setReason(text);
        setRichReason(document);
      }} disabled={pending} compact />
      <div className="actions"><button type="submit" className="primary" disabled={pending || !relatedId}>Bezug speichern</button>
        <button type="button" disabled={pending} onClick={() => setForm(false)}>Abbrechen</button></div>
    </form>}
    {removal && <form className="inline-form" onSubmit={removeLink}>
      {error && <Notice message={error} />}
      <h4>{relations.find(relation => relation.kind === removal.kind)?.label ?? 'Fachlicher Bezug'}</h4>
      <Field label="Begründung für das Entfernen"><input name="reason" required maxLength={5000} /></Field>
      <div className="actions"><button type="submit" disabled={pending}>Entfernen bestätigen</button>
        <button type="button" disabled={pending} onClick={() => setRemoval(undefined)}>Abbrechen</button></div>
    </form>}
    {record.updated_at && <small className="muted">Aktualisiert am {dateLabel(record.updated_at)}</small>}
  </section>;
}
