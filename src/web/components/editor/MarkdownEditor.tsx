'use client';

import { useEffect, useRef } from 'react';

interface MarkdownEditorProps {
  value: string;
  onChange: (value: string) => void;
}

export function MarkdownEditor({ value, onChange }: MarkdownEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const crepeRef = useRef<unknown>(null);
  const onChangeRef = useRef(onChange);

  // Keep onChange ref current without re-initializing editor
  useEffect(() => {
    onChangeRef.current = onChange;
  }, [onChange]);

  useEffect(() => {
    if (!containerRef.current) return;
    let destroyed = false;

    async function init() {
      const { Crepe } = await import('@milkdown/crepe');
      if (destroyed || !containerRef.current) return;
      const crepe = new Crepe({
        root: containerRef.current,
        defaultValue: value,
      });
      await crepe.create();
      if (destroyed) { crepe.destroy(); return; }
      crepeRef.current = crepe;
    }

    init();

    return () => {
      destroyed = true;
      if (crepeRef.current) {
        (crepeRef.current as { destroy(): void }).destroy();
        crepeRef.current = null;
      }
    };
    // Only run on mount; value and onChange changes are handled separately
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Listen for content changes via input event
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;
    function handleInput() {
      if (crepeRef.current) {
        const md = (crepeRef.current as { getMarkdown(): string }).getMarkdown();
        onChangeRef.current(md);
      }
    }
    container.addEventListener('input', handleInput);
    return () => container.removeEventListener('input', handleInput);
  }, []);

  return (
    <div
      ref={containerRef}
      className="min-h-[400px] glass rounded-xl p-1 focus-within:ring-1 focus-within:ring-[var(--color-accent-primary)]/50"
    />
  );
}
