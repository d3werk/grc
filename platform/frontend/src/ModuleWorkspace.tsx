import type { ComponentType } from 'react';
import type { CatalogRecord, Kind } from './api';
import type { OpenRecord } from './Catalog';

export type ModuleOverviewProps = { rows: CatalogRecord[]; write: boolean; openRecord: OpenRecord };
export type CatalogColumn = { key: string; label: string };
export type ModuleDefinition = {
  title: string;
  description: string;
  sections: { kind: Kind; label: string; description: string; columns?: CatalogColumn[] }[];
  overview?: ComponentType<ModuleOverviewProps>;
};

export function ModuleNavigation({ module, activeKind, openOverview, openRecord }: {
  module: ModuleDefinition;
  activeKind?: Kind;
  openOverview: () => void;
  openRecord: OpenRecord;
}) {
  return <nav className="module-navigation" aria-label={`Ansichten für ${module.title}`}>
    <button type="button" aria-current={activeKind ? undefined : 'page'} onClick={openOverview}>Modulübersicht</button>
    {module.sections.map(section => <button key={section.kind} type="button"
      aria-current={activeKind === section.kind ? 'page' : undefined} onClick={() => openRecord(section.kind)}>
      {section.label}
    </button>)}
  </nav>;
}

export function ModuleOverview({ module, ...data }: ModuleOverviewProps & { module: ModuleDefinition }) {
  const Overview = module.overview;
  return <section className="page">
    <header className="page-heading"><div><h1>{module.title}</h1><p>{module.description}</p></div></header>
    <div className="module-sections">{module.sections.map(section => {
      const count = data.rows.filter(row => row.kind === section.kind).length;
      return <section className="panel module-section" key={section.kind}>
        <div className="panel-title"><h2>{section.label}</h2><span aria-label={`${count} Einträge`}>{count}</span></div>
        <p>{section.description}</p>
        <button type="button" onClick={() => data.openRecord(section.kind)}>{section.label} öffnen</button>
      </section>;
    })}</div>
    {Overview && <Overview {...data} />}
  </section>;
}
