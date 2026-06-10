'use client';

import { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { TagInput } from './TagInput';
import { MarkdownEditor } from './MarkdownEditor';
import type { Category, CheatsheetDetail } from '@/lib/types';
import { createSheetAction, updateSheetAction } from '@/lib/api/actions';

interface SheetFormProps {
  categories: Category[];
  sheet?: CheatsheetDetail;
}

export function SheetForm({ categories, sheet }: SheetFormProps) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [title, setTitle] = useState(sheet?.title ?? '');
  const [categoryId, setCategoryId] = useState<number>(
    sheet?.categoryId ?? categories[0]?.id ?? 0,
  );
  const [tags, setTags] = useState<string[]>(sheet?.tags ?? []);
  const [content, setContent] = useState(sheet?.content ?? '');
  const [htmlFile, setHtmlFile] = useState<File | null>(null);

  const isHtmlSheet = sheet?.contentType === 'html';

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    startTransition(async () => {
      let finalContent = content;
      if (isHtmlSheet && htmlFile) {
        finalContent = await htmlFile.text();
      }

      const payload = { title, categoryId, tags, content: finalContent };

      const res = sheet
        ? await updateSheetAction(sheet.id, payload)
        : await createSheetAction({ ...payload, contentType: 'markdown' });

      if (res.success && res.data) {
        toast.success(sheet ? 'Saved' : 'Created');
        router.push(`/sheets/${res.data.categorySlug}/${res.data.slug}`);
        router.refresh();
      } else {
        toast.error(res.error ?? 'Something went wrong');
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="max-w-4xl mx-auto space-y-6">
      <div className="space-y-1">
        <Label htmlFor="title">Title</Label>
        <Input
          id="title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Sheet title"
          required
          className="text-lg"
        />
      </div>

      <div className="space-y-1">
        <Label htmlFor="category">Category</Label>
        {categories.length > 0 ? (
          <select
            id="category"
            value={categoryId}
            onChange={(e) => setCategoryId(Number(e.target.value))}
            className="w-full glass rounded-lg px-3 py-2 text-sm bg-transparent"
          >
            {categories.map((c) => (
              <option key={c.id} value={c.id} className="bg-surface text-on-surface">
                {c.name}
              </option>
            ))}
          </select>
        ) : (
          <p className="text-sm text-on-surface-muted glass rounded-lg px-3 py-2">
            No categories yet — the first sheet will be assigned to a default category.
            Add categories from the sidebar after saving.
          </p>
        )}
      </div>

      <div className="space-y-1">
        <Label>Tags</Label>
        <TagInput value={tags} onChange={setTags} />
      </div>

      {isHtmlSheet ? (
        <div className="space-y-1">
          <Label htmlFor="replace-file">Replace HTML file</Label>
          <Input
            id="replace-file"
            type="file"
            accept=".html,.htm"
            onChange={(e) => setHtmlFile(e.target.files?.[0] ?? null)}
          />
          <p className="text-xs text-on-surface-muted">Leave empty to keep current content.</p>
        </div>
      ) : (
        <div className="space-y-1">
          <Label>Content</Label>
          <MarkdownEditor value={content} onChange={setContent} />
        </div>
      )}

      <div className="flex gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? 'Saving…' : sheet ? 'Save changes' : 'Create sheet'}
        </Button>
        <Button type="button" variant="ghost" onClick={() => router.back()}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
