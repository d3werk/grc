import { useState } from 'react';
import type { FormEvent } from 'react';
import { ArrowRight, Database, Layers3, LockKeyhole } from 'lucide-react';
import { client, errorMessage } from './api';
import { signIn } from './sessions';
import type { Session } from './sessions';
import { WorkspaceList } from './TenantSwitcher';
import { Brand, Field, Notice } from './ui';

const publicApi = client(null);

export function Auth({ sessions, onSession, onSwitch }: {
  sessions: Session[];
  onSession: (session: Session) => void;
  onSwitch: (session: Session) => Promise<void>;
}) {
  const [mode, setMode] = useState<'login' | 'setup'>('login');
  const [slug, setSlug] = useState('');
  const [tenant, setTenant] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [bootstrap, setBootstrap] = useState('');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    setPending(true);
    setError('');
    try {
      if (mode === 'setup') {
        await publicApi.json('/tenants', {
          method: 'POST',
          headers: { 'X-Bootstrap-Token': bootstrap },
          body: JSON.stringify({ slug: slug.trim(), name: tenant.trim(), admin_email: email.trim(), admin_password: password })
        });
        setBootstrap('');
        setMode('login');
      }
      onSession(await signIn(slug, email, password));
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
      setPassword('');
      setBootstrap('');
    }
  }

  async function openSaved(session: Session) {
    setPending(true);
    setError('');
    try {
      await onSwitch(session);
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  return <main className="auth">
    <section className="auth-intro"><Brand />
      <div className="auth-copy">
        <div className="auth-kicker"><Layers3 size={16} aria-hidden="true" /> Integrierter GRC-Arbeitsbereich</div>
        <h1>Gemeinsame Daten.<br /><span>Klare Bezüge.</span></h1>
        <p>Organisation, Assets, Risiken und Kontrollen in einem Arbeitsbereich verwalten und dieselben Einträge für Datenschutz und Nachweise verwenden.</p>
        <div className="auth-steps"><span><Database size={19} aria-hidden="true" /> Bestand erfassen</span>
          <ArrowRight size={16} aria-hidden="true" /><span><LockKeyhole size={19} aria-hidden="true" /> Verantwortung zuordnen</span></div>
      </div>
    </section>
    <section className="auth-content" aria-labelledby="auth-title">
      <form className="auth-form" onSubmit={submit}>
        <h2 id="auth-title">{mode === 'setup' ? 'Mandant einrichten' : 'Anmelden'}</h2>
        <p className="muted">{mode === 'setup' ? 'Der Einrichtungsschlüssel wird von der Plattformadministration bereitgestellt.' : 'Öffnen Sie den Arbeitsbereich Ihrer Organisation.'}</p>
        {error && <Notice message={error} />}
        {mode === 'login' && sessions.length > 0 && <WorkspaceList sessions={sessions} disabled={pending} onSelect={session => void openSaved(session)} />}
        {mode === 'setup' && <Field label="Name des Mandanten">
          <input required value={tenant} onChange={event => setTenant(event.target.value)} autoComplete="organization" />
        </Field>}
        <Field label="Mandantenkürzel">
          <input required value={slug} onChange={event => setSlug(event.target.value)} placeholder="z. B. muster-gmbh" spellCheck={false} />
        </Field>
        <Field label={mode === 'setup' ? 'E-Mail der Administration' : 'E-Mail'}>
          <input required type="email" value={email} onChange={event => setEmail(event.target.value)} autoComplete="username" />
        </Field>
        <Field label="Passwort">
          <input required type="password" value={password} onChange={event => setPassword(event.target.value)}
            autoComplete={mode === 'setup' ? 'new-password' : 'current-password'} />
        </Field>
        {mode === 'setup' && <Field label="Einrichtungsschlüssel">
          <input required type="password" value={bootstrap} onChange={event => setBootstrap(event.target.value)} autoComplete="off" />
        </Field>}
        <button type="submit" className="primary" disabled={pending}>
          {pending ? 'Bitte warten …' : mode === 'setup' ? 'Mandant anlegen' : 'Arbeitsbereich öffnen'} <ArrowRight size={17} aria-hidden="true" />
        </button>
        <button type="button" className="link-button" disabled={pending} onClick={() => {
          setMode(mode === 'login' ? 'setup' : 'login');
          setError('');
          setPassword('');
          setBootstrap('');
        }}>{mode === 'login' ? 'Neuen Mandanten einrichten' : 'Zur Anmeldung'}</button>
      </form>
    </section>
  </main>;
}
