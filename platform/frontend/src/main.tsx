import { StrictMode, useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Auth } from './Auth';
import { ApiError } from './api';
import { endSession, readProfile, sessionIsValid } from './sessions';
import type { Session } from './sessions';
import { Workspace } from './Workspace';
import './styles.css';

type Connections = { activeId: string | null; sessions: Session[] };

function saveConnection(connections: Connections, session: Session, activate: boolean): Connections {
  const tenantId = session.profile.tenant_id;
  const index = connections.sessions.findIndex(item => item.profile.tenant_id === tenantId);
  const sessions = index === -1 ? [...connections.sessions, session]
    : connections.sessions.map((item, position) => position === index ? session : item);
  return { activeId: activate ? tenantId : connections.activeId, sessions };
}

function retainConnections(connections: Connections, keep: (session: Session) => boolean): Connections {
  const sessions = connections.sessions.filter(keep);
  const activeId = sessions.some(item => item.profile.tenant_id === connections.activeId) ? connections.activeId : null;
  return { activeId, sessions };
}

function App() {
  const [connections, setConnections] = useState<Connections>({ activeId: null, sessions: [] });
  const activation = useRef(0);
  const { activeId, sessions } = connections;
  const session = sessions.find(item => item.profile.tenant_id === activeId);

  useEffect(() => {
    if (!sessions.length) return;
    const nextExpiry = Math.min(...sessions.map(item => Date.parse(item.expiresAt)));
    const delay = Math.min(2_147_483_647, Math.max(0, nextExpiry - Date.now()));
    const timeout = window.setTimeout(() => setConnections(previous => retainConnections(previous, sessionIsValid)), delay);
    return () => window.clearTimeout(timeout);
  }, [sessions]);

  function openConnection(next: Session) {
    activation.current++;
    const existing = sessions.find(item => item.profile.tenant_id === next.profile.tenant_id);
    if (existing && existing.token !== next.token) void endSession(existing.token);
    setConnections(previous => saveConnection(previous, next, true));
  }

  async function switchConnection(target: Session) {
    const revision = ++activation.current;
    try {
      const profile = await readProfile(target.token);
      if (!sessionIsValid(target) || profile.tenant_id !== target.profile.tenant_id || profile.user_id !== target.profile.user_id) {
        throw new ApiError(401, 'Die Mandantensitzung ist abgelaufen. Melden Sie sich erneut an.');
      }
      if (revision === activation.current) setConnections(previous => saveConnection(previous, { ...target, profile }, true));
    } catch (reason) {
      if (revision === activation.current && reason instanceof ApiError && [401, 403].includes(reason.status)) {
        setConnections(previous => retainConnections(previous, item => item.token !== target.token));
        throw new ApiError(401, 'Die Mandantensitzung ist abgelaufen oder wurde beendet. Verbinden Sie den Mandanten erneut.');
      }
      throw reason;
    }
  }

  function logout() {
    activation.current++;
    setConnections({ activeId: null, sessions: [] });
    for (const item of sessions) void endSession(item.token);
  }

  return session ? <Workspace key={session.profile.tenant_id} session={session} sessions={sessions}
    onConnect={next => setConnections(previous => saveConnection(previous, next, false))}
    onSwitch={switchConnection} onLogout={logout} />
    : <Auth sessions={sessions} onSession={openConnection} onSwitch={switchConnection} />;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
