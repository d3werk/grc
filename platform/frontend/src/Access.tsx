import { useState } from 'react';
import type { FormEvent } from 'react';
import { Plus, UsersRound } from 'lucide-react';
import type { Client, Role } from './api';
import { errorMessage } from './api';
import { dateLabel, Empty, Field, Loading, Notice, useData } from './ui';

interface Member { id: string; email: string; role: Role; active: boolean }
interface Mandate { id: string; advisor_id: string; scope: string; permissions: string[]; expires_at: string; revoked_at: string | null }

const roleLabel: Record<Role, string> = { admin: 'Administration', member: 'Bearbeitung', viewer: 'Lesen', advisor: 'Beratung' };
const grantLabel = {
  'records.read': 'Register lesen', 'records.write': 'Register bearbeiten',
  'reviews.read': 'Bewertungen lesen', 'reviews.write': 'Bewertungen erfassen',
  'evidence.read': 'Nachweise lesen', 'evidence.write': 'Nachweise erfassen',
  'exports.read': 'Berichte abrufen'
};

export function Access({ api, availableScopes }: { api: Client; availableScopes: string[] }) {
  const members = useData(signal => api.json<Member[]>('/members', { signal }), [api]);
  const mandates = useData(signal => api.json<Mandate[]>('/advisor-mandates', { signal }), [api]);
  const [form, setForm] = useState<'member' | 'mandate' | null>(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const advisors = members.data?.filter(item => item.role === 'advisor' && item.active) ?? [];

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const values = new FormData(event.currentTarget);
    setPending(true);
    setError('');
    try {
      if (form === 'member') {
        await api.json('/members', { method: 'POST', body: JSON.stringify({
          email: values.get('email'), password: values.get('password'), role: values.get('role')
        }) });
        members.refresh();
      } else {
        await api.json('/advisor-mandates', { method: 'POST', body: JSON.stringify({
          advisor_id: values.get('advisor_id'), scope: String(values.get('scope')).trim(),
          permissions: values.getAll('permissions'), expires_at: new Date(String(values.get('expires_at'))).toISOString()
        }) });
        mandates.refresh();
      }
      setForm(null);
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  async function revoke(mandate: Mandate) {
    if (!window.confirm(`Beratungsmandat im Bereich „${mandate.scope}“ entziehen?`)) return;
    setPending(true);
    setError('');
    try {
      await api.json(`/advisor-mandates/${mandate.id}/revoke`, { method: 'POST' });
      mandates.refresh();
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  return <section className="page">
    <header className="page-heading"><div className="heading-text"><span className="glyph"><UsersRound aria-hidden="true" /></span>
      <div><h1>Zugriffsverwaltung</h1><p>Mitgliedschaften und bereichsgebundene Beratungsmandate verwalten.</p></div></div></header>
    {error && <Notice message={error} />}
    {form && <form className="panel edit-panel" onSubmit={save}>
      <h2>{form === 'member' ? 'Mitglied hinzufügen' : 'Beratungsmandat erteilen'}</h2>
      {form === 'member' ? <div className="form-grid">
        <Field label="E-Mail"><input type="email" name="email" required /></Field>
        <Field label="Rolle"><select name="role" defaultValue="member">{Object.entries(roleLabel).map(([key, value]) =>
          <option key={key} value={key}>{value}</option>)}</select></Field>
        <Field label="Initiales Passwort"><input type="password" name="password" autoComplete="new-password" minLength={12} required /></Field>
      </div> : <>
        <div className="form-grid">
          <Field label="Beratungsmitglied"><select name="advisor_id" defaultValue="" required>
            <option value="">Mitglied auswählen</option>{advisors.map(item => <option key={item.id} value={item.id}>{item.email}</option>)}</select></Field>
          <Field label="Geltungsbereich"><input name="scope" list="mandate-scopes" maxLength={240} required /></Field>
          <datalist id="mandate-scopes">{availableScopes.map(scope => <option value={scope} key={scope} />)}</datalist>
          <Field label="Gültig bis"><input type="datetime-local" name="expires_at" required /></Field>
        </div>
        <fieldset className="permissions"><legend>Freigaben</legend>
          {Object.entries(grantLabel).map(([value, label]) => <label key={value} className="check">
            <input type="checkbox" name="permissions" value={value} defaultChecked={value === 'records.read'} /> {label}</label>)}
        </fieldset>
      </>}
      <div className="actions"><button type="submit" className="primary" disabled={pending}>Speichern</button>
        <button type="button" disabled={pending} onClick={() => setForm(null)}>Abbrechen</button></div>
    </form>}
    <section className="panel"><div className="panel-title"><h2>Mitglieder</h2>
      <button type="button" onClick={() => setForm('member')}><Plus size={16} aria-hidden="true" /> Mitglied hinzufügen</button></div>
      {members.loading ? <Loading /> : members.error ? <Notice message={members.error} /> : members.data &&
        <div className="table-scroll"><table><thead><tr><th scope="col">E-Mail</th><th scope="col">Rolle</th><th scope="col">Status</th></tr></thead>
          <tbody>{members.data.map(item => <tr key={item.id}><th scope="row">{item.email}</th><td>{roleLabel[item.role]}</td>
            <td>{item.active ? 'Aktiv' : 'Inaktiv'}</td></tr>)}</tbody></table></div>}
    </section>
    <section className="panel"><div className="panel-title"><h2>Beratungsmandate</h2>
      <button type="button" disabled={advisors.length === 0} onClick={() => setForm('mandate')}><Plus size={16} aria-hidden="true" /> Mandat erteilen</button></div>
      {mandates.loading ? <Loading /> : mandates.error ? <Notice message={mandates.error} /> : mandates.data?.length
        ? <div className="table-scroll"><table><thead><tr><th scope="col">Beratung</th><th scope="col">Bereich</th><th scope="col">Freigaben</th>
          <th scope="col">Frist</th><th scope="col">Status oder Aktion</th></tr></thead><tbody>{mandates.data.map(item => {
          const expired = new Date(item.expires_at).getTime() <= Date.now();
          return <tr key={item.id}><th scope="row">{members.data?.find(member => member.id === item.advisor_id)?.email ?? 'Beratung'}</th>
            <td>{item.scope}</td><td>{item.permissions.map(value => grantLabel[value as keyof typeof grantLabel] ?? value).join(', ')}</td>
            <td>{dateLabel(item.expires_at)}</td><td>{item.revoked_at ? 'Entzogen' : expired ? 'Abgelaufen' :
              <button type="button" disabled={pending} onClick={() => void revoke(item)}>Entziehen</button>}</td></tr>;
        })}</tbody></table></div> : <Empty title="Keine Mandate">Erstellen Sie ein Beratungsmitglied und vergeben Sie bei Bedarf ein befristetes Mandat.</Empty>}
    </section>
  </section>;
}
