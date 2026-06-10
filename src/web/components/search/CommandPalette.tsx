'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import {
  Command,
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';
import { FileText } from 'lucide-react';
import type { CheatsheetSummary } from '@/lib/types';

interface CommandPaletteProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CommandPalette({ open, onOpenChange }: CommandPaletteProps) {
  const router = useRouter();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<CheatsheetSummary[]>([]);
  const [loading, setLoading] = useState(false);

  // Global Ctrl+K shortcut
  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'k' && (e.ctrlKey || e.metaKey)) {
        e.preventDefault();
        onOpenChange(!open);
      }
    }
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [open, onOpenChange]);

  const search = useCallback(async (q: string) => {
    setLoading(true);
    try {
      const qs = q.trim() ? `?q=${encodeURIComponent(q)}` : '';
      const res = await fetch(`/api/search${qs}`);
      const body = await res.json();
      setResults(body.data ?? []);
    } catch {
      setResults([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!open) {
      setQuery('');
      setResults([]);
      return;
    }
    const timer = setTimeout(() => search(query), 200);
    return () => clearTimeout(timer);
  }, [query, open, search]);

  function navigate(sheet: CheatsheetSummary) {
    onOpenChange(false);
    router.push(`/sheets/${sheet.categorySlug}/${sheet.slug}`);
  }

  return (
    <CommandDialog open={open} onOpenChange={onOpenChange}>
      <Command shouldFilter={false}>
        <CommandInput
          placeholder="Search cheatsheets…"
          value={query}
          onValueChange={setQuery}
        />
        <CommandList>
          {loading && <CommandEmpty>Searching…</CommandEmpty>}
          {!loading && results.length === 0 && query.trim() && (
            <CommandEmpty>No results for &ldquo;{query}&rdquo;</CommandEmpty>
          )}
          {!loading && results.length === 0 && !query.trim() && (
            <CommandEmpty>Type to search…</CommandEmpty>
          )}
          {results.length > 0 && (
            <CommandGroup heading="Cheatsheets">
              {results.map((sheet) => (
                <CommandItem
                  key={sheet.id}
                  value={sheet.title}
                  onSelect={() => navigate(sheet)}
                  className="gap-2 cursor-pointer"
                >
                  <FileText className="h-4 w-4 shrink-0" />
                  <span>{sheet.title}</span>
                  <span className="ml-auto text-xs text-on-surface-muted">
                    {sheet.categorySlug}
                  </span>
                </CommandItem>
              ))}
            </CommandGroup>
          )}
        </CommandList>
      </Command>
    </CommandDialog>
  );
}
