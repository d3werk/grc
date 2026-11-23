import { StrictMode, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Auth } from './Auth';
import type { Session } from './Auth';
import { Workspace } from './Workspace';
import './styles.css';

function App() {
  const [session, setSession] = useState<Session | null>(null);
  useEffect(() => {
    if (!session) return;
    const delay = Math.max(0, new Date(session.expiresAt).getTime() - Date.now());
    const timeout = window.setTimeout(() => setSession(null), delay);
    return () => window.clearTimeout(timeout);
  }, [session]);
  return session ? <Workspace session={session} onLogout={() => setSession(null)} /> : <Auth onSession={setSession} />;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
