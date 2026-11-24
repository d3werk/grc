import { createContext, useContext, useState } from 'react';
import type { ComponentPropsWithoutRef, FormEvent, ReactNode } from 'react';
import type { Client } from './api';
import { errorMessage } from './api';
import { Field, Notice } from './ui';

type FilterPermission = 'records.read' | 'reviews.read' | 'evidence.read' | 'exports.read';

export interface ScopeOptions {
  scopes: string[];
  read_scopes: Record<FilterPermission, string[] | null>;
  catalog_write: string[] | null;
  evidence_write: string[] | null;
  can_create: boolean;
}

const ScopeOptionsContext = createContext<ScopeOptions | undefined>(undefined);

export function ScopeOptionsProvider({ data, children }: { data: ScopeOptions | undefined; children: ReactNode }) {
  return <ScopeOptionsContext.Provider value={data}>{children}</ScopeOptionsContext.Provider>;
}

export function filterScopes(data: ScopeOptions | undefined, permission: string) {
  if (!data) return [];
  const grant = data.read_scopes[permission as FilterPermission];
  return grant ?? data.scopes;
}

type ScopeSelectProps = Omit<ComponentPropsWithoutRef<'select'>, 'children'> & {
  mode?: 'catalog' | 'evidence' | 'mandate' | 'filter';
  emptyLabel?: string;
  permission?: string;
};

export function ScopeSelect({ mode = 'catalog', emptyLabel = 'Ohne Geltungsbereich', permission = 'records.read', ...props }: ScopeSelectProps) {
  const data = useContext(ScopeOptionsContext);
  let options = data?.scopes ?? [];
  let emptyDisabled = false;
  if (mode === 'catalog' || mode === 'evidence') {
    const grant = mode === 'catalog' ? data?.catalog_write : data?.evidence_write;
    options = grant ?? options;
    emptyDisabled = grant != null && !grant.includes('');
  } else if (mode === 'filter') {
    options = filterScopes(data, permission);
  }
  const current = props.value ?? props.defaultValue;
  const names = [...new Set(options.filter(Boolean))];
  if (typeof current === 'string' && current && mode !== 'filter' && !names.includes(current)) names.push(current);
  names.sort((left, right) => left.localeCompare(right, 'de'));
  return <select {...props} required={props.required || emptyDisabled}>
    <option value="" disabled={props.required || emptyDisabled}>{emptyLabel}</option>
    {names.map(name => <option key={name} value={name}>{name}</option>)}
  </select>;
}

export function ScopeCreator({ api, onSaved, onCancel }: { api: Client; onSaved: () => void; onCancel: () => void }) {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const scope = String(new FormData(event.currentTarget).get('scope') ?? '').trim();
    if (!scope) {
      setError('Geben Sie einen Namen ein.');
      return;
    }
    setPending(true);
    setError('');
    try {
      await api.json('/scopes', { method: 'POST', body: JSON.stringify({ scope }) });
      onSaved();
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setPending(false);
    }
  }

  return <form className="panel scope-creator" onSubmit={save} aria-label="Geltungsbereich anlegen">
    {error && <Notice message={error} />}
    <Field label="Name des Geltungsbereichs"><input name="scope" autoFocus maxLength={240} required /></Field>
    <div className="actions"><button type="submit" className="primary" disabled={pending}>Geltungsbereich speichern</button>
      <button type="button" disabled={pending} onClick={onCancel}>Abbrechen</button></div>
  </form>;
}
