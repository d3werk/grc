import { useState } from 'react';
import type { FormEvent } from 'react';
import { History, Plus, RefreshCw, UsersRound } from 'lucide-react';
import type { CatalogRecord, Client, Kind } from './api';
import { errorMessage } from './api';
import { kindInfo } from './Catalog';
import { ScopeSelect } from './ScopeSelect';
import { dateLabel, Empty, Field, Loading, Notice, useData } from './ui';

interface Person {
  id: string;
  display_name: string;
  email: string;
  scope: string;
  active: boolean;
  version: number;
}

interface Responsibility {
  id: string;
  person_id: string;
  display_name: string;
  target_id: string;
  target_kind: Kind;
  role: BusinessRole;
  scope: string;
  revoked_at: string | null;
}

const businessRoles = {
  management: { label: 'Geschäftsführung', targets: ['org_unit'] },
  it_lead: { label: 'IT-Leitung', targets: ['org_unit'] },
  privacy_lead: { label: 'Datenschutz', targets: ['org_unit'] },
  security_lead: { label: 'Informationssicherheit', targets: ['org_unit'] },
  asset_owner: { label: 'Asset-Verantwortung', targets: ['asset', 'it_service'] },
  risk_owner: { label: 'Risikoverantwortung', targets: ['risk'] },
  control_owner: { label: 'Control-Verantwortung', targets: ['control'] },
  action_owner: { label: 'Maßnahmenverantwortung', targets: ['measure'] },
  process_owner: { label: 'Prozessverantwortung', targets: ['business_process', 'processing_activity'] }
} as const satisfies Record<string, { label: string; targets: readonly Kind[] }>;
type BusinessRole = keyof typeof businessRoles;
type Edit = { type: 'person'; person?: Person } | { type: 'assignment' };

export function Organization({ api, rows, scope, write, openRecord }: {
  api: Client; rows: CatalogRecord[]; scope: string; write: boolean;
  openRecord: (kind: Kind, id: string) => void;
}) {
  const [includeRevoked, setIncludeRevoked] = useState(false);
  const people = useData(signal => api.json<Person[]>('/organization/people', { signal }), [api]);
  const assignments = useData(signal => api.json<Responsibility[]>(
    `/organization/responsibilities?include_revoked=${includeRevoked}`, { signal }
  ), [api, includeRevoked]);
  const [edit, setEdit] = useState<Edit | null>(null);
  const [role, setRole] = useState<BusinessRole | ''>('');
  const [confirmRevoke, setConfirmRevoke] = useState<string>();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');

  const visiblePeople = (people.data ?? []).filter(person => !scope || person.scope === scope);
  const activePeople = (people.data ?? []).filter(person => person.active);
  const visibleAssignments = (assignments.data ?? []).filter(item => !scope || item.scope === scope);
  const recordById = new Map(rows.map(row => [row.id, row]));
  const targets = role ? rows.filter(row => businessRoles[role].targets.some(kind => kind === row.kind)) : [];

  function openEdit(value: Edit | null) {
    setRole('');
    setError('');
    setStatus('');
    setConfirmRevoke(undefined);
    setEdit(value);
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!edit) return;
    const values = new FormData(event.currentTarget);
    setPending(true);
    setError('');
    setStatus('');
    try {
      if (edit.type === 'person') {
        const person = edit.person;
        const body = {
          display_name: String(values.get('display_name') ?? '').trim(),
          email: String(values.get('email') ?? '').trim(),
          ...(person ? { active: values.has('active'), expected_version: person.version }
            : { scope: String(values.get('scope') ?? '') })
        };
        await api.json(person ? `/organization/people/${person.id}` : '/organization/people', {
          method: person ? 'PUT' : 'POST', body: JSON.stringify(body)
        });
        people.refresh();
        assignments.refresh();
        setStatus(person ? 'Person aktualisiert.' : 'Person angelegt. Sie können ihr jetzt Aufgaben zuordnen.');
      } else {
        await api.json('/organization/responsibilities', { method: 'POST', body: JSON.stringify({
          person_id: values.get('person_id'), target_id: values.get('target_id'), role
        }) });
        assignments.refresh();
        setStatus('Verantwortlichkeit gespeichert.');
      }
      setEdit(null);
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  async function revoke(item: Responsibility) {
    setPending(true);
    setError('');
    setStatus('');
    try {
      await api.json(`/organization/responsibilities/${item.id}/revoke`, { method: 'POST' });
      setConfirmRevoke(undefined);
      assignments.refresh();
      setStatus('Verantwortlichkeit beendet. Der Zuordnungsverlauf bleibt sichtbar.');
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  return <section className="page">
    <header className="page-heading"><div className="heading-text"><span className="glyph"><UsersRound aria-hidden="true" /></span>
      <div><h1>Personen und Verantwortlichkeiten</h1><p>Personen zentral pflegen und ihnen Aufgaben im Unternehmen zuordnen.</p></div>
    </div></header>
    {error && <Notice message={error} />}
    {status && <Notice message={status} kind="success" />}
    {edit && <form className="panel edit-panel" onSubmit={save} aria-labelledby="responsibility-edit-title">
      <h2 id="responsibility-edit-title">{edit.type === 'person'
        ? edit.person ? 'Person bearbeiten' : 'Person erfassen' : 'Verantwortlichkeit zuordnen'}</h2>
      {edit.type === 'person' ? <div className="form-grid">
        <Field label="Name"><input autoFocus name="display_name" defaultValue={edit.person?.display_name} maxLength={240} required /></Field>
        <Field label="E-Mail (optional)"><input type="email" name="email" defaultValue={edit.person?.email} maxLength={254} /></Field>
        {edit.person ? <label className="check"><input type="checkbox" name="active" defaultChecked={edit.person.active} /> Aktiv</label>
          : <Field label="Geltungsbereich">
            <ScopeSelect name="scope" defaultValue={scope} emptyLabel="Zentral" /></Field>}
      </div> : <div className="form-grid">
        <Field label="Person"><select autoFocus name="person_id" defaultValue="" required>
          <option value="">Person auswählen</option>{activePeople.map(person =>
            <option key={person.id} value={person.id}>{person.display_name}{person.scope && ` · ${person.scope}`}</option>)}
        </select></Field>
        <Field label="Fachrolle" hint="Fachrollen beschreiben Aufgaben. Kontozugriffe werden in der Zugriffsverwaltung vergeben.">
          <select value={role} onChange={event => setRole(event.target.value as BusinessRole | '')} required>
            <option value="">Fachrolle auswählen</option>{Object.entries(businessRoles).map(([key, value]) =>
              <option key={key} value={key}>{value.label}</option>)}
          </select></Field>
        <Field label="Eintrag" hint={role && !targets.length ? 'Für diese Rolle ist im aktuellen Bereich kein passender Eintrag vorhanden.' : undefined}>
          <select key={role} name="target_id" defaultValue="" disabled={!targets.length} required>
            <option value="">Eintrag auswählen</option>{targets.map(row => <option key={row.id} value={row.id}>
              {row.title} · {kindInfo[row.kind].singular}{row.scope && ` · ${row.scope}`}</option>)}
          </select></Field>
      </div>}
      <div className="actions"><button type="submit" className="primary" disabled={pending || (edit.type === 'assignment' && !targets.length)}>
        {pending ? 'Wird gespeichert …' : 'Speichern'}</button>
        <button type="button" disabled={pending} onClick={() => openEdit(null)}>Abbrechen</button></div>
    </form>}

    <section className="panel" aria-labelledby="people-title">
      <div className="panel-title"><h2 id="people-title">Personen</h2>{write &&
        <button type="button" disabled={pending || !!edit} onClick={() => openEdit({ type: 'person' })}>
          <Plus size={16} aria-hidden="true" /> Person erfassen</button>}</div>
      {people.loading ? <Loading /> : people.error ? <Notice message={people.error} /> : visiblePeople.length
        ? <div className="table-scroll"><table><thead><tr><th scope="col">Name</th><th scope="col">E-Mail</th>
          <th scope="col">Bereich</th><th scope="col">Status</th>{write && <th scope="col">Aktion</th>}</tr></thead>
          <tbody>{visiblePeople.map(person => <tr key={person.id}>
            <th scope="row">{person.display_name}</th><td>{person.email || '–'}</td><td>{person.scope || 'Zentral'}</td>
            <td><span className={`status ${person.active ? 'active' : 'archived'}`}>{person.active ? 'Aktiv' : 'Inaktiv'}</span></td>
            {write && <td><button type="button" disabled={pending || !!edit} aria-label={`${person.display_name} bearbeiten`}
              onClick={() => openEdit({ type: 'person', person })}>Bearbeiten</button></td>}
          </tr>)}</tbody></table></div>
        : <Empty title="Keine Personen in diesem Bereich" />}
    </section>

    <section className="panel" aria-labelledby="responsibilities-title">
      <div className="panel-title"><h2 id="responsibilities-title">Verantwortlichkeiten</h2>{write &&
        <button type="button" disabled={pending || !!edit || !activePeople.length || !rows.length}
          onClick={() => openEdit({ type: 'assignment' })}><Plus size={16} aria-hidden="true" /> Verantwortung zuordnen</button>}</div>
      <label className="check"><input type="checkbox" checked={includeRevoked}
        onChange={event => setIncludeRevoked(event.target.checked)} /> Beendete Zuordnungen anzeigen</label>
      {assignments.loading ? <Loading /> : assignments.error ? <Notice message={assignments.error} /> : visibleAssignments.length
        ? <div className="table-scroll"><table><thead><tr><th scope="col">Person</th><th scope="col">Fachrolle</th>
          <th scope="col">Eintrag</th><th scope="col">Bereich</th><th scope="col">Status</th>{write && <th scope="col">Aktion</th>}</tr></thead>
          <tbody>{visibleAssignments.map(item => {
            const record = recordById.get(item.target_id);
            return <tr key={item.id}><th scope="row">{item.display_name}</th><td>{businessRoles[item.role].label}</td>
              <td>{record ? <button className="link-button" type="button" onClick={() => openRecord(record.kind, record.id)}>
                {record.title}</button> : kindInfo[item.target_kind].singular}</td>
              <td>{item.scope || 'Zentral'}</td>
              <td>{item.revoked_at ? <time dateTime={item.revoked_at}>Beendet am {dateLabel(item.revoked_at)}</time> : 'Aktiv'}</td>
              {write && <td>{item.revoked_at ? '–' : confirmRevoke === item.id
                ? <div className="actions"><button type="button" disabled={pending} onClick={() => void revoke(item)}>Beenden bestätigen</button>
                  <button type="button" disabled={pending} onClick={() => setConfirmRevoke(undefined)}>Abbrechen</button></div>
                : <button type="button" disabled={pending || !!edit}
                  aria-label={`${businessRoles[item.role].label} für ${item.display_name} beenden`}
                  onClick={() => setConfirmRevoke(item.id)}>Beenden</button>}</td>}
            </tr>;
          })}</tbody></table></div>
        : <Empty title="Keine Verantwortlichkeiten in diesem Bereich">Ordnen Sie einer aktiven Person eine Rolle für eine Organisationseinheit, ein Risiko oder einen anderen Eintrag zu.</Empty>}
    </section>
  </section>;
}

interface AuditEvent {
  id: string;
  actor_id: string | null;
  action: string;
  entity_type: string;
  entity_id: string;
  details: Record<string, unknown>;
  scope: string;
  created_at: string;
}

const auditActions: Record<string, string> = {
  'tenant.created': 'Mandant angelegt', 'member.created': 'Mitglied angelegt',
  'advisor_mandate.created': 'Beratungsmandat erteilt', 'advisor_mandate.revoked': 'Beratungsmandat entzogen',
  'catalog.created': 'Registereintrag angelegt', 'catalog.updated': 'Registereintrag geändert',
  'catalog.link_created': 'Bezug hinzugefügt', 'catalog.link_updated': 'Bezug geändert', 'catalog.link_removed': 'Bezug entfernt',
  'risk.assessed': 'Risiko bewertet', 'evidence.created': 'Nachweis angelegt', 'evidence.downloaded': 'Nachweis heruntergeladen',
  'evidence.linked': 'Nachweis zugeordnet', 'evidence.unlinked': 'Nachweiszuordnung entfernt',
  'report.exported': 'Bericht abgerufen', 'person.created': 'Person angelegt', 'person.updated': 'Person geändert',
  'responsibility.assigned': 'Verantwortlichkeit zugeordnet', 'responsibility.revoked': 'Verantwortlichkeit beendet',
  'scope.created': 'Geltungsbereich angelegt'
};
const entityLabels: Record<string, string> = {
  tenant: 'Mandant', member: 'Mitglied', advisor_mandate: 'Beratungsmandat', catalog_record: 'Registereintrag',
  catalog_link: 'Bezug', risk_assessment: 'Risikobewertung', evidence: 'Nachweis', management_report: 'Managementbericht',
  privacy_report: 'Datenschutzbericht', person: 'Person', responsibility: 'Verantwortlichkeit', scope: 'Geltungsbereich'
};
const detailLabels: Record<string, string> = {
  kind: 'Typ', version: 'Version', fields: 'Geänderte Felder', reason: 'Begründung', record_id: 'Eintrags-ID',
  role: 'Rolle', scope: 'Bereich', slug: 'Mandantenkennung', permissions: 'Berechtigungen', format: 'Format',
  person_id: 'Personen-ID', target_id: 'Ziel-ID', risk_id: 'Risiko-ID', risk_version: 'Risikoversionsnummer'
};
const utcDateTime = new Intl.DateTimeFormat('de-DE', { dateStyle: 'short', timeStyle: 'medium', timeZone: 'UTC' });

function auditDate(value: string) {
  return utcDateTime.format(new Date(value));
}

export function AuditLog({ api, canReadMembers }: { api: Client; canReadMembers: boolean }) {
  const [limit, setLimit] = useState(50);
  const events = useData(signal => api.json<AuditEvent[]>(`/audit-events?limit=${limit}`, { signal }), [api, limit]);
  const members = useData(signal => canReadMembers ? api.json<{ id: string; email: string }[]>('/members', { signal })
    : Promise.resolve([]), [api, canReadMembers]);
  const names = new Map(members.data?.map(member => [member.id, member.email]));

  return <section className="page">
    <header className="page-heading"><div className="heading-text"><span className="glyph"><History aria-hidden="true" /></span>
      <div><h1>Audit-Protokoll</h1><p>Protokollierte Änderungen und Berichtsabrufe dieses Mandanten.</p></div></div>
      <button type="button" onClick={events.refresh} disabled={events.loading}><RefreshCw size={16} aria-hidden="true" /> Aktualisieren</button>
    </header>
    <section className="panel" aria-labelledby="audit-events-title">
      <div className="panel-title"><h2 id="audit-events-title">Letzte Ereignisse</h2>
        <Field label="Anzahl"><select value={limit} onChange={event => setLimit(Number(event.target.value))}>
          <option value={50}>50</option><option value={100}>100</option><option value={200}>200</option>
        </select></Field></div>
      {events.loading ? <Loading /> : events.error ? <Notice message={events.error} /> : events.data?.length
        ? <div className="table-scroll"><table><thead><tr><th scope="col">Zeitpunkt (UTC)</th><th scope="col">Ereignis</th>
          <th scope="col">Ausgeführt von</th><th scope="col">Bereich</th><th scope="col">Eintrag</th></tr></thead>
          <tbody>{events.data.map(event => <tr key={event.id}>
            <td><time dateTime={event.created_at}>{auditDate(event.created_at)}</time></td>
            <th scope="row">{auditActions[event.action] ?? event.action}</th>
            <td>{event.actor_id ? names.get(event.actor_id) ?? event.actor_id : 'System'}</td><td>{event.scope || 'Zentral'}</td>
            <td><details><summary>{entityLabels[event.entity_type] ?? event.entity_type}</summary><dl>
              <div><dt>ID</dt><dd>{event.entity_id}</dd></div>{Object.entries(event.details).map(([key, value]) =>
                <div key={key}><dt>{detailLabels[key] ?? key}</dt><dd>{Array.isArray(value) ? value.join(', ') : String(value ?? '–')}</dd></div>)}
            </dl></details></td>
          </tr>)}</tbody></table></div>
        : <Empty title="Keine Ereignisse verfügbar" />}
    </section>
  </section>;
}
