export type Role = 'admin' | 'member' | 'viewer' | 'advisor';

export interface SessionProfile {
  tenant_id: string;
  tenant_slug: string;
  tenant_name: string;
  user_id: string;
  email: string;
  role: Role;
  permissions: string[];
}

export interface LoginResponse {
  access_token: string;
  expires_at: string;
}

export type Kind = 'org_unit' | 'asset' | 'it_service' | 'business_process' | 'risk' | 'control'
  | 'measure' | 'framework_version' | 'requirement' | 'processing_activity' | 'supplier' | 'dpia';
export type Status = 'draft' | 'active' | 'in_review' | 'completed' | 'archived';
export interface AttributeField {
  key: string;
  label: string;
  type: 'text' | 'textarea' | 'date';
}

export interface KindMetadata {
  kind: Kind;
  label: string;
  attribute_fields: AttributeField[];
}

export interface CatalogRecord {
  id: string;
  kind: Kind;
  code: string;
  title: string;
  description: string;
  status: Status;
  owner: string;
  org_unit_id: string | null;
  scope: string;
  version: number;
  attributes: Record<string, string>;
  created_at: string;
  updated_at: string;
}

export interface LinkKind {
  kind: string;
  from_kind: Kind;
  to_kind: Kind;
  label: string;
}

export interface RecordLink {
  id: string;
  kind: string;
  from_id: string;
  to_id: string;
  rationale: string;
}

export interface AssessmentInput {
  risk_version: number;
  assessed_on: string;
  inherent_likelihood: number;
  inherent_impact: number;
  residual_likelihood: number | null;
  residual_impact: number | null;
  response: string;
  rationale: string;
}

export interface Assessment extends AssessmentInput {
  id: string;
  inherent_score: number;
  residual_score: number | null;
}

export interface Evidence {
  id: string;
  title: string;
  origin: string;
  scope: string;
  kind: 'file' | 'external';
  url: string | null;
  filename: string | null;
  size_bytes: number | null;
  sha256: string | null;
  collected_on: string;
  valid_until: string | null;
  linked_record_ids: string[];
}

export interface DashboardData {
  scope: string | null;
  as_of: string;
  record_counts: Partial<Record<Kind, number>>;
  status_counts: Partial<Record<Kind, Partial<Record<Status, number>>>>;
  risk: { total: number; assessed: number; unassessed: number; stale: number } | null;
  evidence: { total: number; files: number; external: number; expired: number; controls_without_current: number } | null;
}

export interface MatrixData {
  stage: 'inherent' | 'residual';
  cells: { likelihood: number; impact: number; count: number }[];
  total: number;
  unassessed: number;
  stale: number;
}

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
  }
}

async function request(path: string, token: string | null, options: RequestInit = {}) {
  const headers = new Headers(options.headers);
  if (token) headers.set('Authorization', `Bearer ${token}`);
  if (typeof options.body === 'string') headers.set('Content-Type', 'application/json');
  const response = await fetch(`/api/v1${path}`, { ...options, headers, credentials: 'omit' });
  if (response.ok) return response;
  const statusMessages: Record<number, string> = {
    401: 'Anmeldung oder Sitzung konnte nicht bestätigt werden.',
    403: 'Für diese Aktion oder diesen Geltungsbereich fehlt die Berechtigung.',
    404: 'Der Eintrag ist in diesem Arbeitsbereich nicht verfügbar.',
    409: 'Der Arbeitsstand hat sich geändert oder ein Eintrag besteht bereits.',
    422: 'Eingaben prüfen und erneut speichern.',
    429: 'Zu viele Anfragen. Bitte später erneut versuchen.'
  };
  let message = statusMessages[response.status] ?? `Anfrage fehlgeschlagen (HTTP ${response.status}).`;
  if (response.status >= 400 && response.status < 500 && ![401, 403, 404].includes(response.status)) {
    const data: unknown = await response.json().catch(() => undefined);
    if (data && typeof data === 'object' && 'detail' in data && typeof data.detail === 'string' && data.detail.length <= 300) {
      message = data.detail;
    }
  }
  throw new ApiError(response.status, message);
}

export function client(token: string | null) {
  return {
    async json<T>(path: string, options?: RequestInit): Promise<T> {
      const response = await request(path, token, options);
      return response.status === 204 ? undefined as T : await response.json() as T;
    },
    async file(path: string): Promise<Blob> {
      return (await request(path, token)).blob();
    }
  };
}

export type Client = ReturnType<typeof client>;

export function query(options: Record<string, string | number | undefined>) {
  const params = new URLSearchParams();
  Object.entries(options).forEach(([key, value]) => {
    if (value !== undefined && value !== '') params.set(key, String(value));
  });
  const text = params.toString();
  return text ? `?${text}` : '';
}

export async function readCatalog(api: Client, scope: string, signal?: AbortSignal) {
  const result: CatalogRecord[] = [];
  const limit = 100;
  for (let offset = 0; ; offset += limit) {
    const page = await api.json<CatalogRecord[]>(`/catalog${query({ scope, limit, offset })}`, { signal });
    result.push(...page);
    if (page.length < limit) return result;
  }
}

export function errorMessage(error: unknown) {
  if (error instanceof ApiError) return error.message;
  return 'Die Anfrage konnte nicht abgeschlossen werden. Verbindung prüfen und erneut versuchen.';
}

export function download(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = name;
  anchor.click();
  window.setTimeout(() => URL.revokeObjectURL(url), 1_000);
}
