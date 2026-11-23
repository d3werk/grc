import { client } from './api';
import type { LoginResponse, SessionProfile } from './api';

export interface Session {
  token: string;
  expiresAt: string;
  profile: SessionProfile;
}

export function sessionIsValid(session: Session) {
  return Date.parse(session.expiresAt) > Date.now();
}

export async function readProfile(token: string) {
  return client(token).json<SessionProfile>('/auth/me', { signal: AbortSignal.timeout(12_000) });
}

export async function endSession(token: string) {
  try {
    await client(token).json('/auth/logout', { method: 'POST', signal: AbortSignal.timeout(5_000) });
  } catch {
    // The browser already removed this session. The server enforces its own expiry.
  }
}

export async function signIn(slug: string, email: string, password: string): Promise<Session> {
  const token = await client(null).json<LoginResponse>('/auth/login', {
    method: 'POST',
    signal: AbortSignal.timeout(15_000),
    body: JSON.stringify({ tenant_slug: slug.trim(), email: email.trim(), password })
  });
  try {
    return { token: token.access_token, expiresAt: token.expires_at, profile: await readProfile(token.access_token) };
  } catch (reason) {
    void endSession(token.access_token);
    throw reason;
  }
}
