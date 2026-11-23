import { cloneElement, useEffect, useId, useState } from 'react';
import type { DependencyList, ReactElement, ReactNode } from 'react';
import { AlertCircle, CircleHelp, FolderOpen, LoaderCircle } from 'lucide-react';
import { errorMessage } from './api';

export function Brand() {
  return <div className="brand">
    <span className="brand-mark"><img src="/d3-werk-logo.png" width="854" height="151" alt="" /></span>
    <span>D3 GRC</span>
  </div>;
}

export function Field({ label, children, hint }: {
  label: string;
  children: ReactElement<{ 'aria-labelledby'?: string; 'aria-describedby'?: string }>;
  hint?: string;
}) {
  const id = useId();
  const hintId = hint ? `${id}-hint` : undefined;
  return <label className="field">
    <span id={id}>{label}</span>
    {cloneElement(children, { 'aria-labelledby': id, 'aria-describedby': hintId })}
    {hint && <small id={hintId}>{hint}</small>}
  </label>;
}

export function Help({ label, children }: { label: string; children: string }) {
  const id = useId();
  return <span className="help">
    <button type="button" aria-label={label} aria-describedby={id}
      onKeyDown={event => { if (event.key === 'Escape') event.currentTarget.blur(); }}>
      <CircleHelp size={16} aria-hidden="true" />
    </button>
    <span className="tooltip" id={id} role="tooltip">{children}</span>
  </span>;
}

export function Notice({ message, kind = 'error' }: { message: string; kind?: 'error' | 'success' | 'info' }) {
  return <div className={`notice ${kind}`} role={kind === 'error' ? 'alert' : 'status'}>
    <AlertCircle size={17} aria-hidden="true" /><span>{message}</span>
  </div>;
}

export function Loading() {
  return <div className="loading" role="status"><LoaderCircle size={19} aria-hidden="true" /> Daten werden geladen …</div>;
}

export function Empty({ title, children, action }: { title: string; children?: ReactNode; action?: ReactNode }) {
  return <div className="empty"><span className="empty-icon"><FolderOpen size={23} aria-hidden="true" /></span>
    <h3>{title}</h3>{children && <p>{children}</p>}{action}</div>;
}

export function useData<T>(load: (signal: AbortSignal) => Promise<T>, dependencies: DependencyList) {
  const [data, setData] = useState<T>();
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError('');
    load(controller.signal).then(value => {
      if (!controller.signal.aborted) setData(value);
    }).catch(reason => {
      if (!controller.signal.aborted) {
        setData(undefined);
        setError(errorMessage(reason));
      }
    }).finally(() => {
      if (!controller.signal.aborted) setLoading(false);
    });
    return () => controller.abort();
  }, [...dependencies, revision]);
  return { data, error, loading, refresh: () => setRevision(value => value + 1) };
}

export function dateLabel(value: string | null | undefined) {
  if (!value) return '–';
  return new Intl.DateTimeFormat('de-DE', { dateStyle: 'medium', timeZone: 'UTC' }).format(new Date(value));
}

export function todayIso() {
  const date = new Date();
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
}
