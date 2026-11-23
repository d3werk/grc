import { lazy, Suspense } from 'react';
import type { ComponentProps, ReactNode } from 'react';
import './rich-text.css';

type Mark = { type: 'bold' | 'italic' };
export type Inline = { type: 'text'; text: string; marks?: Mark[] } | { type: 'hardBreak' };
export type Paragraph = { type: 'paragraph'; content?: Inline[] };
export type Heading = { type: 'heading'; attrs: { level: 2 | 3 }; content?: Inline[] };
type ListItemNode = { type: 'listItem'; content: Paragraph[] };
type Block = Paragraph | Heading | { type: 'bulletList'; content: ListItemNode[] }
  | { type: 'orderedList'; attrs?: { start: number }; content: ListItemNode[] };
export type RichTextDocument = { type: 'doc'; content: Block[] };

const Editor = lazy(() => import('./RichTextEditor'));

export function RichTextEditor(props: ComponentProps<typeof Editor>) {
  return <Suspense fallback={<p className="muted" role="status">Texteditor für {props.label} wird geladen …</p>}>
    <Editor {...props} />
  </Suspense>;
}

export function formattingKey(documents: Record<string, RichTextDocument>) {
  return JSON.stringify(documents, (_, value: unknown) => {
    if (value === null) return undefined;
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      return Object.fromEntries(Object.entries(value).sort(([a], [b]) => a.localeCompare(b)));
    }
    return value;
  });
}

function inlineContent(block: Paragraph | Heading) {
  return block.content?.map((node, index) => {
    if (node.type === 'hardBreak') return <br key={index} />;
    let content: ReactNode = node.text;
    for (const mark of node.marks ?? []) {
      if (mark.type === 'bold') content = <strong>{content}</strong>;
      if (mark.type === 'italic') content = <em>{content}</em>;
    }
    return <span key={index}>{content}</span>;
  });
}

export function RichTextPreview({ text, document }: { text: string; document?: RichTextDocument | null }) {
  return <div className="rich-text-preview">
    {document ? document.content.map((block, index) => {
      if (block.type === 'paragraph') return <p key={index}>{inlineContent(block)}</p>;
      if (block.type === 'heading') return block.attrs.level === 2
        ? <h2 key={index}>{inlineContent(block)}</h2> : <h3 key={index}>{inlineContent(block)}</h3>;
      const items = block.content.map((item, itemIndex) => <li key={itemIndex}>
        {item.content.map((paragraph, i) => <p key={i}>{inlineContent(paragraph)}</p>)}
      </li>);
      return block.type === 'orderedList' ? <ol key={index} start={block.attrs?.start}>{items}</ol> : <ul key={index}>{items}</ul>;
    }) : <p>{text}</p>}
  </div>;
}
