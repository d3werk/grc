import { useId, useRef, useState } from 'react';
import { ArrowDown, ArrowUp, ChevronRight, GitBranch, List, Plus, Search, Trash2 } from 'lucide-react';
import type { AttributeField, CatalogRecord, Client, RecordLink } from './api';
import { query } from './api';
import { dateLabel, Empty, Field, Loading, Notice, useData } from './ui';
import { RichTextEditor, RichTextPreview } from './RichText';
import type { RichTextDocument } from './RichText';
import './business-processes.css';

const processTypes = ['Führungsprozess', 'Kernprozess', 'Unterstützungsprozess'];
const criticalityLevels = ['Niedrig', 'Mittel', 'Hoch', 'Kritisch'];

type Step = { title: string; role: string; instruction: string };
type StepDraft = Step & { id: number };

function processSteps(value: string | undefined): Step[] {
  if (!value) return [];
  try {
    const parsed: unknown = JSON.parse(value);
    if (!Array.isArray(parsed)) return [];
    return parsed.flatMap(item => typeof item?.title === 'string' ? [{
      title: item.title,
      role: typeof item.role === 'string' ? item.role : '',
      instruction: typeof item.instruction === 'string' ? item.instruction : ''
    }] : []);
  } catch {
    return [];
  }
}

async function readParentLinks(api: Client, signal: AbortSignal) {
  const links: RecordLink[] = [];
  const limit = 200;
  for (let offset = 0; ; offset += limit) {
    const page = await api.json<RecordLink[]>(`/links${query({ kind: 'process_parent', limit, offset })}`, { signal });
    links.push(...page);
    if (page.length < limit) return links;
  }
}

export function BusinessProcessRegister({ api, rows, activeId, revision, onSelect, statusLabels }: {
  api: Client; rows: CatalogRecord[]; activeId?: string; revision: number; onSelect: (id: string) => void;
  statusLabels: Record<CatalogRecord['status'], string>;
}) {
  const [view, setView] = useState<'list' | 'map'>('list');
  const [search, setSearch] = useState('');
  const [filterType, setFilterType] = useState('');
  const [filterStatus, setFilterStatus] = useState('');
  const heading = useId();
  const parentLinks = useData(signal => view === 'map' ? readParentLinks(api, signal) : Promise.resolve([]),
    [api, view, revision]);
  const searchTerm = search.toLocaleLowerCase('de').trim();
  const processes = rows.filter(row => row.kind === 'business_process');
  const visible = processes.filter(row => (!filterType || row.attributes.process_type === filterType)
    && (!filterStatus || row.status === filterStatus)
    && [row.title, row.code, row.owner, row.description, ...Object.values(row.attributes)]
      .some(value => value.toLocaleLowerCase('de').includes(searchTerm)));

  return <section className="panel register-list process-register" aria-labelledby={heading}>
    <div className="panel-title"><h2 id={heading}>Prozesse</h2><span>{visible.length} von {processes.length}</span></div>
    <div className="process-view" role="group" aria-label="Prozessansicht auswählen">
      <button type="button" aria-pressed={view === 'list'} onClick={() => setView('list')}><List size={17} aria-hidden="true" /> Liste</button>
      <button type="button" aria-pressed={view === 'map'} onClick={() => setView('map')}><GitBranch size={17} aria-hidden="true" /> Prozesslandkarte</button>
    </div>
    <label className="searchbox"><Search size={17} aria-hidden="true" />
      <input aria-label="Prozesse durchsuchen" value={search} onChange={event => setSearch(event.target.value)} placeholder="Titel, Kürzel oder Rolle" /></label>
    <div className="form-grid process-filters">
      <Field label="Prozessart filtern"><select value={filterType} onChange={event => setFilterType(event.target.value)}>
        <option value="">Alle Prozessarten</option>{processTypes.map(type => <option key={type}>{type}</option>)}</select></Field>
      <Field label="Prozessstatus filtern"><select value={filterStatus} onChange={event => setFilterStatus(event.target.value)}>
        <option value="">Alle Status</option>{Object.entries(statusLabels).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></Field>
    </div>
    {!visible.length ? <Empty title={processes.length ? 'Keine passenden Prozesse' : 'Noch keine Geschäftsprozesse'}>
      {processes.length ? 'Ändern Sie die Suche oder die Filter.' : 'Legen Sie einen Geschäftsprozess für den Arbeitsbereich an.'}</Empty>
      : view === 'list' ? <div className="table-scroll"><table>
        <thead><tr><th scope="col">Prozess</th><th scope="col">Status</th></tr></thead>
        <tbody>{visible.map(row => <tr key={row.id} className={row.id === activeId ? 'selected' : ''}>
          <td><button type="button" className="row-button" aria-pressed={row.id === activeId} onClick={() => onSelect(row.id)}>
            <strong>{row.title}</strong><small>{[row.code, row.attributes.process_type, row.owner].filter(Boolean).join(' · ') || '–'}</small>
          </button></td><td><span className={`status ${row.status}`}>{statusLabels[row.status]}</span></td>
        </tr>)}</tbody>
      </table></div> : parentLinks.loading ? <Loading /> : parentLinks.error ? <Notice message={parentLinks.error} />
        : <ProcessMap processes={processes} visible={visible} links={parentLinks.data ?? []} activeId={activeId} onSelect={onSelect} />}
  </section>;
}

function ProcessMap({ processes, visible, links, activeId, onSelect }: {
  processes: CatalogRecord[]; visible: CatalogRecord[]; links: RecordLink[]; activeId?: string; onSelect: (id: string) => void;
}) {
  const known = new Map(processes.map(process => [process.id, process]));
  const parents = new Map(links.filter(link => known.has(link.from_id) && known.has(link.to_id))
    .map(link => [link.from_id, link.to_id]));
  const matching = new Set(visible.map(process => process.id));
  const included = new Set(matching);
  for (const id of matching) {
    for (let ancestor = parents.get(id); ancestor && !included.has(ancestor); ancestor = parents.get(ancestor)) {
      included.add(ancestor);
    }
  }
  const children = new Map<string, CatalogRecord[]>();
  const roots: CatalogRecord[] = [];
  for (const process of processes) {
    if (!included.has(process.id)) continue;
    const parent = parents.get(process.id);
    if (parent && included.has(parent)) {
      const descendants = children.get(parent) ?? [];
      descendants.push(process);
      children.set(parent, descendants);
    } else roots.push(process);
  }
  const groups = [...processTypes, 'Ohne Prozessart'];
  function renderNodes(nodes: CatalogRecord[]) {
    return <ul className="process-tree">{nodes.map(node => <li key={node.id}>
      <button type="button" className={`process-node${matching.has(node.id) ? '' : ' process-ancestor'}`}
        aria-pressed={node.id === activeId} onClick={() => onSelect(node.id)}>
        <ChevronRight size={16} aria-hidden="true" /><span><strong>{node.title}</strong>
          {(node.code || node.scope) && <small>{[node.code, node.scope].filter(Boolean).join(' · ')}</small>}</span></button>
      {children.has(node.id) && renderNodes(children.get(node.id) ?? [])}
    </li>)}</ul>;
  }
  return <div className="process-map">{groups.map(group => {
    const nodes = roots.filter(row => (processTypes.includes(row.attributes.process_type) ? row.attributes.process_type : 'Ohne Prozessart') === group);
    return nodes.length ? <section key={group}><h3>{group}</h3>{renderNodes(nodes)}</section> : null;
  })}</div>;
}

export function BusinessProcessFacts({ record, fields }: { record: CatalogRecord; fields: AttributeField[] }) {
  const steps = processSteps(record.attributes.process_steps);
  const basic = fields.filter(({ key }) => !['process_steps', 'review_due', 'review_notes'].includes(key) && record.attributes[key]);
  const due = record.attributes.review_due;
  return <>
    {basic.length > 0 && <dl className="record-meta facts">{basic.map(({ key, label }) => <div key={key}><dt>{label}</dt><dd>
      <RichTextPreview text={record.attributes[key]} document={record.rich_text?.[key]} /></dd></div>)}</dl>}
    <section className="detail-section process-steps"><h3>Prozessablauf</h3>
      {steps.length ? <ol>{steps.map((step, index) => <li key={index}>
        <div><strong>{step.title}</strong>{step.role && <small>{step.role}</small>}
          {step.instruction && <p>{step.instruction}</p>}</div>
      </li>)}</ol> : <p className="muted">Für diesen Prozess sind noch keine Schritte erfasst.</p>}
    </section>
    <section className="detail-section"><h3>Überprüfung</h3>
      <dl className="record-meta"><div><dt>Nächste Überprüfung</dt><dd>{dateLabel(due)}</dd></div></dl>
      {record.attributes.review_notes && <RichTextPreview text={record.attributes.review_notes} document={record.rich_text?.review_notes} />}
    </section>
  </>;
}

export function BusinessProcessFields({ fields, attributes, richText, onChange, onRichChange, disabled }: {
  fields: AttributeField[]; attributes: Record<string, string>; richText: Record<string, RichTextDocument>;
  onChange: (key: string, value: string) => void;
  onRichChange: (key: string, value: string, document?: RichTextDocument) => void;
  disabled: boolean;
}) {
  function renderField({ key, label, type }: AttributeField) {
    const value = attributes[key] ?? '';
    const choices = key === 'process_type' ? processTypes : key === 'criticality' ? criticalityLevels : null;
    if (choices) return <Field key={key} label={label}><select value={value} disabled={disabled}
      onChange={event => onChange(key, event.target.value)}>
      <option value="">Ohne Angabe</option>{value && !choices.includes(value) && <option>{value}</option>}
      {choices.map(choice => <option key={choice}>{choice}</option>)}</select></Field>;
    if (type === 'textarea') return <RichTextEditor key={key} label={label} value={value} document={richText[key]}
      onChange={(text, document) => onRichChange(key, text, document)} disabled={disabled} compact />;
    return <Field key={key} label={label}><input type={type} value={value} disabled={disabled} maxLength={5000}
      onChange={event => onChange(key, event.target.value)} /></Field>;
  }

  const setup = fields.filter(field => ['process_type', 'criticality', 'purpose', 'recovery_target'].includes(field.key));
  const flow = fields.filter(field => ['trigger', 'inputs', 'outputs'].includes(field.key));
  const review = fields.filter(field => ['review_due', 'review_notes'].includes(field.key));
  return <>
    <fieldset className="process-fieldset"><legend>Einordnung</legend><div className="form-grid">{setup.map(renderField)}</div></fieldset>
    <fieldset className="process-fieldset"><legend>Auslöser und Ergebnisse</legend><div className="form-grid">{flow.map(renderField)}</div></fieldset>
    <fieldset className="process-fieldset"><legend>Prozessschritte</legend>
      <ProcessStepEditor initial={attributes.process_steps} disabled={disabled} onChange={value => onChange('process_steps', value)} />
    </fieldset>
    <fieldset className="process-fieldset"><legend>Überprüfung</legend><div className="form-grid">{review.map(renderField)}</div></fieldset>
  </>;
}

function ProcessStepEditor({ initial, disabled, onChange }: { initial?: string; disabled: boolean; onChange: (value: string) => void }) {
  const nextId = useRef(0);
  const [steps, setSteps] = useState<StepDraft[]>(() => processSteps(initial).map((step, id) => ({ ...step, id })));
  const added = useRef(steps.length);
  const [focusId, setFocusId] = useState<number>();

  function update(draft: StepDraft[]) {
    setSteps(draft);
    const serialized = draft.map(({ title, role, instruction }) => ({ title, role: role || undefined, instruction: instruction || undefined }));
    onChange(serialized.length ? JSON.stringify(serialized) : '');
  }

  function move(index: number, offset: number) {
    const ordered = [...steps];
    [ordered[index], ordered[index + offset]] = [ordered[index + offset], ordered[index]];
    update(ordered);
  }

  function add() {
    const id = added.current + nextId.current++;
    update([...steps, { id, title: '', role: '', instruction: '' }]);
    setFocusId(id);
  }

  function edit(id: number, key: keyof Step, value: string) {
    update(steps.map(step => step.id === id ? { ...step, [key]: value } : step));
  }

  return <>
    {steps.length > 0 && <ol className="process-step-editor">{steps.map((step, index) => <li key={step.id}>
      <div className="process-step-heading"><strong>Schritt {index + 1}</strong><div className="actions">
        <button type="button" aria-label={`Schritt ${index + 1} nach oben verschieben`} disabled={disabled || index === 0}
          onClick={() => move(index, -1)}><ArrowUp size={16} aria-hidden="true" /></button>
        <button type="button" aria-label={`Schritt ${index + 1} nach unten verschieben`} disabled={disabled || index === steps.length - 1}
          onClick={() => move(index, 1)}><ArrowDown size={16} aria-hidden="true" /></button>
        <button type="button" aria-label={`Schritt ${index + 1} entfernen`} disabled={disabled}
          onClick={() => update(steps.filter(item => item.id !== step.id))}><Trash2 size={16} aria-hidden="true" /></button>
      </div></div>
      <div className="form-grid">
        <Field label={`Tätigkeit · Schritt ${index + 1}`}><input required autoFocus={focusId === step.id} maxLength={160}
          disabled={disabled} value={step.title} onChange={event => edit(step.id, 'title', event.target.value)} /></Field>
        <Field label={`Rolle · Schritt ${index + 1}`}><input maxLength={160} disabled={disabled} value={step.role}
          onChange={event => edit(step.id, 'role', event.target.value)} /></Field>
      </div>
      <Field label={`Ausführung · Schritt ${index + 1}`}><textarea maxLength={600} rows={2} disabled={disabled} value={step.instruction}
        onChange={event => edit(step.id, 'instruction', event.target.value)} /></Field>
    </li>)}</ol>}
    <button type="button" disabled={disabled || steps.length >= 30} onClick={add}><Plus size={16} aria-hidden="true" /> Schritt hinzufügen</button>
  </>;
}
