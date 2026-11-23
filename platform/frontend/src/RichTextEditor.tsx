import { useEffect, useId } from 'react';
import { EditorContent, useEditor, useEditorState } from '@tiptap/react';
import { ListItem } from '@tiptap/extension-list';
import StarterKit from '@tiptap/starter-kit';
import { Bold, Heading2, Heading3, Italic, List, ListOrdered, Pilcrow, Redo2, Undo2 } from 'lucide-react';
import type { Heading, Inline, Paragraph, RichTextDocument } from './RichText';

const extensions = [
  StarterKit.configure({
    blockquote: false, code: false, codeBlock: false, horizontalRule: false, link: false,
    listItem: false, strike: false, underline: false, trailingNode: false, heading: { levels: [2, 3] }
  }),
  ListItem.extend({ content: 'paragraph+' })
];

function fromPlainText(text: string): RichTextDocument {
  const content = text.replace(/\r\n?/g, '\n').split('\n').flatMap<Inline>((line, index) => [
    ...(index ? [{ type: 'hardBreak' as const }] : []),
    ...(line ? [{ type: 'text' as const, text: line }] : [])
  ]);
  return { type: 'doc', content: [{ type: 'paragraph', content }] };
}

function plainLine(block: Paragraph | Heading) {
  return (block.content ?? []).map(node => node.type === 'text' ? node.text : '\n').join('');
}

function plainText(document: RichTextDocument) {
  return document.content.flatMap(block => {
    if (block.type === 'paragraph' || block.type === 'heading') return plainLine(block);
    return block.content.map((item, index) => {
      const line = item.content.map(plainLine).join('\n');
      if (!line.trim()) return '';
      const prefix = block.type === 'orderedList' ? `${(block.attrs?.start ?? 1) + index}. ` : '• ';
      return prefix + line;
    }).filter(Boolean);
  }).join('\n');
}

function hasFormatting(document: RichTextDocument) {
  return document.content.some(block => block.type !== 'paragraph'
    || block.content?.some(node => node.type === 'text' && node.marks?.length));
}

export default function RichTextEditor({ label, value, document, onChange, disabled = false, compact = false }: {
  label: string;
  value: string;
  document?: RichTextDocument | null;
  onChange: (text: string, rich?: RichTextDocument) => void;
  disabled?: boolean;
  compact?: boolean;
}) {
  const id = useId();
  const editor = useEditor({
    extensions,
    editable: !disabled,
    content: document ?? fromPlainText(value),
    editorProps: {
      attributes: { role: 'textbox', 'aria-multiline': 'true', 'aria-labelledby': id, class: 'rich-text-input' }
    },
    onUpdate: ({ editor: current }) => {
      const next = current.getJSON() as RichTextDocument;
      const text = plainText(next);
      const rich = text.trim() && hasFormatting(next) ? next : undefined;
      onChange(rich ? text.trim() : text, rich);
    }
  });
  useEffect(() => { editor?.setEditable(!disabled, false); }, [disabled, editor]);

  const state = useEditorState({
    editor,
    selector: ({ editor: current }) => current && ({
      bold: current.isActive('bold'), italic: current.isActive('italic'),
      paragraph: current.isActive('paragraph') && !current.isActive('bulletList') && !current.isActive('orderedList'),
      heading2: current.isActive('heading', { level: 2 }), heading3: current.isActive('heading', { level: 3 }),
      bulletList: current.isActive('bulletList'), orderedList: current.isActive('orderedList'),
      undo: current.can().undo(), redo: current.can().redo()
    })
  });

  const buttons = [
    { label: 'Fett', icon: Bold, active: state?.bold, run: () => editor?.chain().focus().toggleBold().run() },
    { label: 'Kursiv', icon: Italic, active: state?.italic, run: () => editor?.chain().focus().toggleItalic().run() },
    { label: 'Absatz', icon: Pilcrow, active: state?.paragraph, run: () => editor?.chain().focus().setParagraph().run() },
    { label: 'Überschrift', icon: Heading2, active: state?.heading2, run: () => editor?.chain().focus().toggleHeading({ level: 2 }).run() },
    { label: 'Zwischenüberschrift', icon: Heading3, active: state?.heading3, run: () => editor?.chain().focus().toggleHeading({ level: 3 }).run() },
    { label: 'Aufzählung', icon: List, active: state?.bulletList, run: () => editor?.chain().focus().toggleBulletList().run() },
    { label: 'Nummerierte Liste', icon: ListOrdered, active: state?.orderedList, run: () => editor?.chain().focus().toggleOrderedList().run() },
    { label: 'Rückgängig', icon: Undo2, blocked: !state?.undo, run: () => editor?.chain().focus().undo().run() },
    { label: 'Wiederholen', icon: Redo2, blocked: !state?.redo, run: () => editor?.chain().focus().redo().run() }
  ];

  return <div className={`field rich-text-field${compact ? ' compact' : ''}`}>
    <span id={id}>{label}</span>
    <div className="rich-text-box">
      <div className="rich-text-toolbar" role="group" aria-label={`Formatierung für ${label}`}>
        {buttons.map(({ label: action, icon: Icon, active, blocked, run }) =>
          <button key={action} type="button" aria-label={action} title={action} aria-pressed={active}
            disabled={disabled || !editor || blocked} onMouseDown={event => event.preventDefault()} onClick={run}>
            <Icon size={16} aria-hidden="true" />
          </button>)}
      </div>
      <EditorContent editor={editor} />
    </div>
  </div>;
}
