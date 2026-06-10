'use client';

import { useState } from 'react';
import { X } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';

interface TagInputProps {
  value: string[];
  onChange: (tags: string[]) => void;
}

export function TagInput({ value, onChange }: TagInputProps) {
  const [draft, setDraft] = useState('');

  function addTag() {
    const tag = draft.trim().toLowerCase();
    if (!tag || value.includes(tag)) return;
    onChange([...value, tag]);
    setDraft('');
  }

  function removeTag(tag: string) {
    onChange(value.filter((t) => t !== tag));
  }

  return (
    <div className="flex flex-wrap gap-2 p-2 glass rounded-lg min-h-[2.5rem] items-center">
      {value.map((tag) => (
        <Badge key={tag} variant="secondary" className="gap-1 pr-1 flex items-center">
          {tag}
          <button
            type="button"
            aria-label={`Remove ${tag}`}
            onClick={() => removeTag(tag)}
            className="hover:text-destructive transition-colors ml-0.5"
          >
            <X className="h-3 w-3" />
          </button>
        </Badge>
      ))}
      <Input
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') {
            e.preventDefault();
            addTag();
          }
          if (e.key === 'Backspace' && !draft && value.length > 0) {
            onChange(value.slice(0, -1));
          }
        }}
        placeholder="Add tag…"
        className="border-0 bg-transparent h-7 px-1 flex-1 min-w-[6rem] focus-visible:ring-0 focus-visible:ring-offset-0 shadow-none"
      />
    </div>
  );
}
