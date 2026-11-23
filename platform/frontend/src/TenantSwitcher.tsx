import { useLayoutEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Building2, ChevronRight, LogIn, Plus } from 'lucide-react';
import { errorMessage } from './api';
import { endSession, signIn } from './sessions';
import type { Session } from './sessions';
import { Field, Loading, Notice } from './ui';
import './TenantSwitcher.css';

export function WorkspaceList({ sessions, activeId, disabled, onSelect }: {
  sessions: Session[];
  activeId?: string;
  disabled?: boolean;
  onSelect: (session: Session) => void;
}) {
  return <ul className="tenant-switch-list" aria-label="Verbundene Mandanten">
    {sessions.map(session => {
      const profile = session.profile;
      const isActive = profile.tenant_id === activeId;
      return <li key={profile.tenant_id}>
        {isActive ? <div className="tenant-switch-row" aria-current="true">
          <Building2 size={19} aria-hidden="true" /><span><strong>{profile.tenant_name}</strong><small>{profile.tenant_slug}</small></span>
          <small className="tenant-current">Aktiv</small>
        </div> : <button type="button" className="tenant-switch-row" disabled={disabled} onClick={() => onSelect(session)}>
          <Building2 size={19} aria-hidden="true" /><span><strong>{profile.tenant_name}</strong><small>{profile.tenant_slug}</small></span>
          <ChevronRight size={17} aria-hidden="true" />
        </button>}
      </li>;
    })}
  </ul>;
}

export function TenantSwitcher({ active, sessions, checking, error, onSelect, onConnect, onClose }: {
  active: Session;
  sessions: Session[];
  checking: boolean;
  error: string;
  onSelect: (session: Session) => void;
  onConnect: (session: Session) => void;
  onClose: () => void;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  const [adding, setAdding] = useState(false);
  const [authenticating, setAuthenticating] = useState(false);
  const [loginError, setLoginError] = useState('');

  useLayoutEffect(() => {
    const node = dialog.current!;
    node.showModal();
    return () => node.close();
  }, []);

  async function authenticate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (authenticating) return;
    const form = event.currentTarget;
    const fields = new FormData(form);
    setAuthenticating(true);
    setLoginError('');
    try {
      const session = await signIn(String(fields.get('slug')), String(fields.get('email')), String(fields.get('password')));
      if (!dialog.current) {
        void endSession(session.token);
        return;
      }
      if (sessions.some(existing => existing.profile.tenant_id === session.profile.tenant_id)) {
        void endSession(session.token);
        setLoginError('Dieser Mandant ist bereits verbunden. Wählen Sie ihn in der Liste aus.');
        setAdding(false);
        return;
      }
      onConnect(session);
      onSelect(session);
    } catch (reason) {
      setLoginError(errorMessage(reason));
    } finally {
      const password = form.elements.namedItem('password');
      if (password instanceof HTMLInputElement) password.value = '';
      setAuthenticating(false);
    }
  }

  const busy = checking || authenticating;
  return <dialog ref={dialog} className="panel tenant-switcher" aria-labelledby="tenant-switch-title"
    onCancel={event => { event.preventDefault(); if (!busy) onClose(); }}>
    <div className="tenant-switch-heading">
      <h2 id="tenant-switch-title">Mandant wechseln</h2>
      <button type="button" disabled={busy} onClick={onClose}>Schließen</button>
    </div>
    {checking ? <Loading /> : <>
      {error && <Notice message={error} />}
      {loginError && <Notice message={loginError} />}
      {adding ? <form className="tenant-switch-form" onSubmit={event => void authenticate(event)}>
        <p className="muted">Melden Sie sich mit einem Konto des gewünschten Mandanten an.</p>
        <Field label="Mandantenkürzel"><input required name="slug" placeholder="z. B. muster-gmbh" spellCheck={false} disabled={busy} /></Field>
        <Field label="E-Mail"><input required name="email" type="email" autoComplete="username" disabled={busy} /></Field>
        <Field label="Passwort"><input required name="password" type="password" autoComplete="current-password" disabled={busy} /></Field>
        <div className="actions">
          <button type="submit" className="primary" disabled={busy}><LogIn size={17} aria-hidden="true" /> {authenticating ? 'Anmeldung läuft …' : 'Mandant verbinden'}</button>
          <button type="button" disabled={busy} onClick={() => { setLoginError(''); setAdding(false); }}>Zur Mandantenauswahl</button>
        </div>
      </form> : <>
        <WorkspaceList sessions={sessions} activeId={active.profile.tenant_id} onSelect={onSelect} />
        <button type="button" className="primary" onClick={() => { setLoginError(''); setAdding(true); }}>
          <Plus size={18} aria-hidden="true" /> Weiteren Mandanten verbinden
        </button>
      </>}
    </>}
  </dialog>;
}
