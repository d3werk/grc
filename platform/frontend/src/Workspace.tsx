import { useMemo, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import {
  BookOpen, ChartNoAxesCombined, ChevronRight, ClipboardCheck, Filter, Grid2X2, History, LayoutDashboard,
  LogOut, Menu, ShieldCheck, UsersRound, X
} from 'lucide-react';
import { client, readCatalog } from './api';
import type { Kind, KindMetadata, LinkKind } from './api';
import type { Session } from './Auth';
import { Catalog, kindInfo } from './Catalog';
import type { CatalogLeaveGuard } from './Catalog';
import { EvidenceView, RiskMatrix } from './Operations';
import { Dashboard, Reports } from './Reporting';
import { Access } from './Access';
import { AuditLog, Organization } from './Organization';
import { Brand, Empty, Loading, Notice, useData } from './ui';

export const roles = {
  admin: 'Administration', member: 'Bearbeitung', viewer: 'Lesen', advisor: 'Beratung'
};

export const pages = {
  dashboard: { title: 'Übersicht', icon: LayoutDashboard, permission: 'records.read' },
  ...Object.fromEntries(Object.entries(kindInfo).map(([key, item]) =>
    [key, { title: item.title, icon: item.icon, permission: 'records.read' }])) as Record<Kind, {
    title: string; icon: typeof LayoutDashboard; permission: string;
  }>,
  risk_matrix: { title: 'Risikomatrix', icon: Grid2X2, permission: 'reviews.read' },
  evidence: { title: 'Nachweise', icon: ClipboardCheck, permission: 'evidence.read' },
  tom: { title: 'TOM', icon: ShieldCheck, permission: 'records.read' },
  reports: { title: 'Berichte', icon: ChartNoAxesCombined, permission: 'exports.read' },
  people: { title: 'Personen und Verantwortlichkeiten', icon: UsersRound, permission: 'records.read' },
  access: { title: 'Zugriffsverwaltung', icon: UsersRound, permission: 'iam.manage' },
  audit: { title: 'Audit-Protokoll', icon: History, permission: 'audit.read' }
};
export type Page = keyof typeof pages;
type Route = { page: Page; id?: string; create?: boolean; controlId?: string };

const groups: { title: string; items: Page[] }[] = [
  { title: 'Arbeitsbereich', items: ['dashboard', 'reports'] },
  { title: 'Basisregister', items: ['org_unit', 'people', 'asset', 'business_process', 'it_service', 'supplier'] },
  { title: 'Governance und IKS', items: ['risk', 'risk_matrix', 'control', 'measure', 'evidence', 'framework_version', 'requirement'] },
  { title: 'Datenschutz', items: ['processing_activity', 'tom', 'dpia'] },
  { title: 'Administration', items: ['access', 'audit'] }
];

const shortNames: Partial<Record<Page, string>> = {
  people: 'Verantwortlichkeiten', processing_activity: 'Verarbeitungsverzeichnis',
  dpia: 'Folgenabschätzungen', framework_version: 'Regelwerke'
};

export function Workspace({ session, onLogout }: { session: Session; onLogout: () => void }) {
  const api = useMemo(() => client(session.token), [session.token]);
  const permissions = new Set(session.profile.permissions);
  const can = (permission: string) => permissions.has(permission);
  const allowed = (page: Page) => can(pages[page].permission);
  const [route, setRoute] = useState<Route>(() => ({
    page: (Object.keys(pages) as Page[]).find(page => session.profile.permissions.includes(pages[page].permission)) ?? 'dashboard'
  }));
  const [scope, setScope] = useState('');
  const [typedScope, setTypedScope] = useState('');
  const [menu, setMenu] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const catalogLeaveGuard = useRef<CatalogLeaveGuard | null>(null);
  const catalog = useData(signal => !can('records.read')
    ? Promise.resolve({ scope, rows: [], metadata: [], links: [] })
    : Promise.all([
      readCatalog(api, scope, signal),
      api.json<KindMetadata[]>('/catalog/kinds', { signal }),
      api.json<LinkKind[]>('/links/kinds', { signal })
    ]).then(([rows, metadata, links]) => ({ scope, rows, metadata, links })), [api, scope]);
  const currentCatalog = catalog.data?.scope === scope ? catalog.data : undefined;
  const rows = currentCatalog?.rows ?? [];
  const availableScopes = [...new Set(rows.map(row => row.scope).filter(Boolean))].sort();
  const currentLabel = shortNames[route.page] ?? pages[route.page].title;
  const contextLabel = scope || (session.profile.role === 'advisor' ? 'Alle freigegebenen Bereiche' : 'Alle Geltungsbereiche');

  function leaveCatalog(action: () => void) {
    if (catalogLeaveGuard.current) catalogLeaveGuard.current(action);
    else action();
  }

  function navigate(page: Page, id?: string, create?: boolean) {
    if (!allowed(page)) return;
    setMenu(false);
    if (route.page === page && route.id === id && route.create === create && !route.controlId) return;
    leaveCatalog(() => setRoute({ page, id, create }));
  }

  function applyScope(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextScope = typedScope.trim();
    if (nextScope === scope) return;
    leaveCatalog(() => {
      setScope(nextScope);
      setRoute(current => ({ page: current.page }));
    });
  }

  async function logout() {
    setSigningOut(true);
    try {
      await api.json('/auth/logout', { method: 'POST' });
    } finally {
      onLogout();
    }
  }

  function pageContent() {
    if (!allowed(route.page)) return <div className="panel"><Empty title="Kein Arbeitsbereich freigegeben">
      Fragen Sie die Administration nach der passenden Rolle oder einem gültigen Beratungsmandat.</Empty></div>;
    switch (route.page) {
      case 'dashboard': return <Dashboard key={scope} api={api} scope={scope} contextLabel={contextLabel} permissions={permissions} navigate={navigate} />;
      case 'risk_matrix': return <RiskMatrix key={scope} api={api} scope={scope} />;
      case 'evidence': return <EvidenceView key={`${scope}|${route.controlId ?? ''}`} api={api} scope={scope}
        records={rows} write={can('evidence.write')} controlId={route.controlId} />;
      case 'reports': return <Reports key={scope} api={api} scope={scope} catalogRead={can('records.read')} navigate={navigate} />;
      case 'access': return <Access api={api} availableScopes={availableScopes} />;
      case 'audit': return <AuditLog api={api} canReadMembers={can('iam.manage')} />;
      case 'people': return <Organization key={scope} api={api} rows={rows} scope={scope}
        write={can('records.write')} openRecord={navigate} />;
      default: {
        if (catalog.error) return <Notice message={catalog.error} />;
        if (!currentCatalog) return <Loading />;
        const kind = route.page === 'tom' ? 'control' : route.page;
        return <Catalog key={`${route.page}|${scope}|${route.id ?? ''}|${route.create ?? ''}`}
          api={api} kind={kind} scope={scope} rows={rows} relations={currentCatalog.links}
          attributeFields={currentCatalog.metadata.find(item => item.kind === kind)?.attribute_fields ?? []}
          write={can('records.write')} assess={can('reviews.write')} reviewRead={can('reviews.read')}
          selectedId={route.id} create={route.create} openRecord={navigate} reload={catalog.refresh} leaveGuard={catalogLeaveGuard}
          onUpload={can('evidence.write') && allowed('evidence') ? controlId => setRoute({ page: 'evidence', controlId }) : undefined}
          title={route.page === 'tom' ? 'Technische und organisatorische Maßnahmen' : undefined}
          description={route.page === 'tom' ? 'Datenschutzmaßnahmen aus dem gemeinsamen Control-Register mit Bezügen zu Assets und Verarbeitungen.' : undefined} />;
      }
    }
  }

  return <div className="workspace">
    <a className="skip-link" href="#main-content">Zum Inhalt</a>
    {menu && <button className="nav-scrim" type="button" aria-label="Navigation schließen" onClick={() => setMenu(false)} />}
    <aside className={`sidebar ${menu ? 'open' : ''}`} id="workspace-navigation">
      <Brand />
      <div className="tenant-name"><BookOpen size={16} aria-hidden="true" /><strong title={session.profile.tenant_name}>{session.profile.tenant_name}</strong></div>
      <nav aria-label="Arbeitsbereiche">{groups.map(group => {
        const items = group.items.filter(allowed);
        return items.length ? <div className="nav-group" key={group.title}><h2>{group.title}</h2>
          {items.map(page => {
            const Icon = pages[page].icon;
            return <button key={page} type="button" className={route.page === page ? 'current' : ''}
              aria-current={route.page === page ? 'page' : undefined} onClick={() => navigate(page)}>
              <Icon size={18} aria-hidden="true" /> {shortNames[page] ?? pages[page].title}</button>;
          })}</div> : null;
      })}</nav>
      <div className="sidebar-footer"><ShieldCheck size={17} aria-hidden="true" /> {roles[session.profile.role]}</div>
    </aside>
    <div className="workspace-area">
      <header className="topbar">
        <button type="button" className="menu-button" onClick={() => setMenu(!menu)}
          aria-label={menu ? 'Navigation schließen' : 'Navigation öffnen'} aria-controls="workspace-navigation" aria-expanded={menu}>
          {menu ? <X size={20} aria-hidden="true" /> : <Menu size={20} aria-hidden="true" />}</button>
        <div className="breadcrumbs"><span>{session.profile.tenant_name}</span><ChevronRight size={15} aria-hidden="true" /><strong>{currentLabel}</strong></div>
        <div className="user-actions"><span title={session.profile.email}>{session.profile.email}</span>
          <button type="button" disabled={signingOut} onClick={() => leaveCatalog(() => void logout().catch(() => undefined))}>
            <LogOut size={17} aria-hidden="true" /> <span>Abmelden</span></button></div>
      </header>
      <main id="main-content" tabIndex={-1} className="main-content">
        {route.page !== 'access' && route.page !== 'audit' && <form className="context-bar" onSubmit={applyScope}>
          <label><Filter size={17} aria-hidden="true" /><span>Geltungsbereich</span>
            <input list="known-scopes" value={typedScope} maxLength={240} onChange={event => setTypedScope(event.target.value)} placeholder="Alle Bereiche" /></label>
          <datalist id="known-scopes">{availableScopes.map(item => <option key={item} value={item} />)}</datalist>
          <button type="submit">Anwenden</button>
          {route.page !== 'dashboard' && <small className="context-active" aria-live="polite">{contextLabel}</small>}
        </form>}
        {pageContent()}
      </main>
    </div>
  </div>;
}
