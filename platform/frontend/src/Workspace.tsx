import { useMemo, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Activity, BookOpen, Bot, ChartNoAxesCombined, ChevronRight, ClipboardCheck, FileSignature, Filter, Grid2X2, HeartPulse, History, LayoutDashboard,
  LogOut, Menu, Repeat2, ShieldCheck, UsersRound, X
} from 'lucide-react';
import { client, errorMessage, readCatalog } from './api';
import type { Kind, KindMetadata, LinkKind } from './api';
import type { Session } from './sessions';
import { Catalog, kindInfo } from './Catalog';
import type { CatalogLeaveGuard } from './Catalog';
import { EvidenceView, RiskMatrix } from './Operations';
import { Dashboard, Reports } from './Reporting';
import { Access } from './Access';
import { AuditLog, Organization } from './Organization';
import { TenantSwitcher } from './TenantSwitcher';
import { ModuleNavigation, ModuleOverview } from './ModuleWorkspace';
import { internalAuditModule } from './InternalAudit';
import { continuityModule, resilienceModule } from './Resilience';
import { aiModule } from './AiGovernance';
import { contractModule } from './Contracts';
import { filterScopes, ScopeCreator, ScopeOptionsProvider, ScopeSelect } from './ScopeSelect';
import type { ScopeOptions } from './ScopeSelect';
import { Brand, Empty, Loading, Notice, useData } from './ui';

export const roles = {
  admin: 'Administration', member: 'Bearbeitung', viewer: 'Lesen', advisor: 'Beratung'
};

const modules = {
  internal_audits: internalAuditModule,
  operational_resilience: resilienceModule,
  business_continuity: continuityModule,
  ai_grc: aiModule,
  contracts: contractModule,
};
type ModulePage = keyof typeof modules;
const modulePages = Object.keys(modules) as ModulePage[];
const moduleByKind = new Map<Kind, ModulePage>(
  modulePages.flatMap(page => modules[page].sections.map(section => [section.kind, page] as const))
);

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
  internal_audits: { title: internalAuditModule.title, icon: ClipboardCheck, permission: 'records.read' },
  operational_resilience: { title: resilienceModule.title, icon: Activity, permission: 'records.read' },
  business_continuity: { title: continuityModule.title, icon: HeartPulse, permission: 'records.read' },
  ai_grc: { title: aiModule.title, icon: Bot, permission: 'records.read' },
  contracts: { title: contractModule.title, icon: FileSignature, permission: 'records.read' },
  access: { title: 'Zugriffsverwaltung', icon: UsersRound, permission: 'iam.manage' },
  audit: { title: 'Audit-Protokoll', icon: History, permission: 'audit.read' }
};
export type Page = keyof typeof pages;
type Route = { page: Page; id?: string; create?: boolean; evidenceTarget?: { kind: Kind; id: string; upload: boolean } };

const groups: { title: string; items: Page[] }[] = [
  { title: 'Arbeitsbereich', items: ['dashboard', 'reports'] },
  { title: 'Basisregister', items: ['org_unit', 'people', 'asset', 'business_process', 'it_service', 'supplier'] },
  { title: 'Governance und IKS', items: ['risk', 'risk_matrix', 'control', 'measure', 'evidence', 'framework_version', 'requirement'] },
  { title: 'Fachmodule', items: modulePages },
  { title: 'Datenschutz', items: ['processing_activity', 'tom', 'dpia'] },
  { title: 'Administration', items: ['access', 'audit'] }
];

const shortNames: Partial<Record<Page, string>> = {
  people: 'Verantwortlichkeiten', processing_activity: 'Verarbeitungsverzeichnis',
  dpia: 'Folgenabschätzungen', framework_version: 'Regelwerke', business_process: 'Prozessmanagement'
};

export function Workspace({ session, sessions, onConnect, onSwitch, onLogout }: {
  session: Session;
  sessions: Session[];
  onConnect: (session: Session) => void;
  onSwitch: (session: Session) => Promise<void>;
  onLogout: () => void;
}) {
  const api = useMemo(() => client(session.token), [session.token]);
  const permissions = new Set(session.profile.permissions);
  const can = (permission: string) => permissions.has(permission);
  const allowed = (page: Page) => can(pages[page].permission);
  const [route, setRoute] = useState<Route>(() => ({
    page: (Object.keys(pages) as Page[]).find(page => session.profile.permissions.includes(pages[page].permission)) ?? 'dashboard'
  }));
  const [scope, setScope] = useState('');
  const [pendingScope, setPendingScope] = useState('');
  const [creatingScope, setCreatingScope] = useState(false);
  const [menu, setMenu] = useState(false);
  const [switching, setSwitching] = useState<{ checking: boolean; error: string } | null>(null);
  const catalogLeaveGuard = useRef<CatalogLeaveGuard | null>(null);
  const scopeOptions = useData(signal => api.json<ScopeOptions>('/scopes', { signal }), [api]);
  const catalogScopeAvailable = !scope || !scopeOptions.data || filterScopes(scopeOptions.data, 'records.read').includes(scope);
  const catalog = useData(signal => !can('records.read') || !catalogScopeAvailable
    ? Promise.resolve({ scope, rows: [], metadata: [], links: [] })
    : Promise.all([
      readCatalog(api, scope, signal),
      api.json<KindMetadata[]>('/catalog/kinds', { signal }),
      api.json<LinkKind[]>('/links/kinds', { signal })
    ]).then(([rows, metadata, links]) => ({ scope, rows, metadata, links })), [api, scope, catalogScopeAvailable]);
  const currentCatalog = catalog.data?.scope === scope ? catalog.data : undefined;
  const rows = currentCatalog?.rows ?? [];
  const currentLabel = shortNames[route.page] ?? pages[route.page].title;
  const modulePage = modulePages.find(page => page === route.page) ?? moduleByKind.get(route.page as Kind);
  const currentModule = modulePage ? modules[modulePage] : undefined;
  const activeModuleSection = currentModule?.sections.find(section => section.kind === route.page);
  const contextLabel = scope || (session.profile.role === 'advisor' ? 'Alle freigegebenen Bereiche' : 'Alle Geltungsbereiche');

  function leaveCatalog(action: () => void) {
    if (catalogLeaveGuard.current) catalogLeaveGuard.current(action);
    else action();
  }

  function navigate(page: Page, id?: string, create?: boolean) {
    if (!allowed(page)) return;
    setMenu(false);
    if (route.page === page && route.id === id && route.create === create && !route.evidenceTarget) return;
    leaveCatalog(() => {
      if (scope && scopeOptions.data && !filterScopes(scopeOptions.data, pages[page].permission).includes(scope)) {
        setScope('');
        setPendingScope('');
      }
      setCreatingScope(false);
      setRoute({ page, id, create });
    });
  }

  function applyScope(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextScope = pendingScope;
    if (nextScope === scope) return;
    leaveCatalog(() => {
      setScope(nextScope);
      setRoute(current => ({ page: current.page }));
    });
  }

  function switchTenant(target: Session) {
    setSwitching(null);
    leaveCatalog(() => {
      setSwitching({ checking: true, error: '' });
      void onSwitch(target).catch(reason => setSwitching({ checking: false, error: errorMessage(reason) }));
    });
  }

  function pageContent() {
    if (!allowed(route.page)) return <div className="panel"><Empty title="Kein Arbeitsbereich freigegeben">
      Fragen Sie die Administration nach der passenden Rolle oder einem gültigen Beratungsmandat.</Empty></div>;
    switch (route.page) {
      case 'dashboard': return <Dashboard key={scope} api={api} scope={scope} contextLabel={contextLabel} permissions={permissions} navigate={navigate} />;
      case 'risk_matrix': return <RiskMatrix key={scope} api={api} scope={scope} />;
      case 'evidence': {
        const target = route.evidenceTarget;
        return <EvidenceView key={`${scope}|${target?.id ?? ''}|${target?.upload ?? ''}`} api={api} scope={scope}
          records={rows} write={can('evidence.write')} targetId={target?.id} startUpload={target?.upload}
          onBack={target ? () => navigate(target.kind, target.id) : undefined} />;
      }
      case 'reports': return <Reports key={scope} api={api} scope={scope} catalogRead={can('records.read')} navigate={navigate} />;
      case 'access': return <Access api={api} />;
      case 'audit': return <AuditLog api={api} canReadMembers={can('iam.manage')} />;
      case 'people': return <Organization key={scope} api={api} rows={rows} scope={scope}
        write={can('records.write')} openRecord={navigate} />;
      case 'internal_audits':
      case 'operational_resilience':
      case 'business_continuity':
      case 'ai_grc':
      case 'contracts':
        if (catalog.error) return <Notice message={catalog.error} />;
        if (!currentCatalog) return <Loading />;
        return <ModuleOverview key={`${route.page}|${scope}`} module={modules[route.page]}
          rows={rows} write={can('records.write')} openRecord={navigate} />;
      default: {
        if (catalog.error) return <Notice message={catalog.error} />;
        if (!currentCatalog) return <Loading />;
        const kind = route.page === 'tom' ? 'control' : route.page;
        return <Catalog key={`${route.page}|${scope}|${route.id ?? ''}|${route.create ?? ''}`}
          api={api} kind={kind} scope={scope} rows={rows} relations={currentCatalog.links}
          attributeFields={currentCatalog.metadata.find(item => item.kind === kind)?.attribute_fields ?? []}
          write={can('records.write')} assess={can('reviews.write')} reviewRead={can('reviews.read')}
          selectedId={route.id} create={route.create} openRecord={navigate} reload={() => {
            catalog.refresh();
            scopeOptions.refresh();
          }} leaveGuard={catalogLeaveGuard} columns={activeModuleSection?.columns ??
            (route.page === 'control' ? [{ key: 'next_test_due', label: 'Nächste Prüfung' }] : undefined)}
          onEvidence={allowed('evidence') ? (record, upload) => setRoute({
            page: 'evidence', evidenceTarget: { kind, id: record.id, upload }
          }) : undefined}
          canUpload={can('evidence.write')}
          title={route.page === 'tom' ? 'Technische und organisatorische Maßnahmen' : undefined}
          description={route.page === 'tom' ? 'Datenschutzmaßnahmen aus dem gemeinsamen Control-Register mit Bezügen zu Assets und Verarbeitungen.' : undefined} />;
      }
    }
  }

  return <ScopeOptionsProvider data={scopeOptions.data}><div className="workspace">
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
            const active = route.page === page || modulePage === page;
            return <button key={page} type="button" className={active ? 'current' : ''}
              aria-current={active ? 'page' : undefined} onClick={() => navigate(page)}>
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
          <button type="button" className="tenant-switch-trigger" aria-label="Mandant wechseln" aria-haspopup="dialog"
            title={`Mandant wechseln · ${session.profile.tenant_name}`} onClick={() => setSwitching({ checking: false, error: '' })}>
            <Repeat2 size={18} aria-hidden="true" /><span className="tenant-switch-label">Mandant wechseln</span>
            <span className="tenant-switch-mobile-label">Mandant</span></button>
          <button type="button" aria-label="Abmelden" title="Von allen verbundenen Mandanten abmelden" onClick={() => leaveCatalog(onLogout)}>
            <LogOut size={17} aria-hidden="true" /> <span className="sign-out-label">Abmelden</span></button></div>
      </header>
      {switching && <TenantSwitcher active={session} sessions={sessions} {...switching}
        onSelect={switchTenant} onConnect={onConnect} onClose={() => setSwitching(null)} />}
      <main id="main-content" tabIndex={-1} className="main-content">
        {route.page !== 'access' && route.page !== 'audit' && <form className="context-bar" onSubmit={applyScope}>
          <label><Filter size={17} aria-hidden="true" /><span id="workspace-scope-label">Geltungsbereich</span>
            <ScopeSelect mode="filter" aria-labelledby="workspace-scope-label" permission={pages[route.page].permission} value={pendingScope}
              emptyLabel={session.profile.role === 'advisor' ? 'Alle freigegebenen Bereiche' : 'Alle Geltungsbereiche'}
              onChange={event => setPendingScope(event.target.value)} /></label>
          <button type="submit" disabled={pendingScope === scope}>Anwenden</button>
          {scopeOptions.data?.can_create && <button type="button" aria-expanded={creatingScope}
            onClick={() => setCreatingScope(value => !value)}>Geltungsbereich anlegen</button>}
          {route.page !== 'dashboard' && <small className="context-active" aria-live="polite">{contextLabel}</small>}
        </form>}
        {scopeOptions.error && <Notice message={scopeOptions.error} />}
        {creatingScope && <ScopeCreator api={api} onCancel={() => setCreatingScope(false)} onSaved={() => {
          setCreatingScope(false);
          scopeOptions.refresh();
        }} />}
        {currentModule && modulePage && <ModuleNavigation module={currentModule} activeKind={activeModuleSection?.kind}
          openOverview={() => navigate(modulePage)} openRecord={navigate} />}
        {pageContent()}
      </main>
    </div>
  </div></ScopeOptionsProvider>;
}
