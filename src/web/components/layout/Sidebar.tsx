'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Plus, Upload, LogOut } from 'lucide-react';
import type { Category, CheatsheetSummary } from '@/lib/types';
import { SidebarCategory } from './SidebarCategory';
import { ThemeToggle } from '@/components/theme/ThemeToggle';
import { Button, buttonVariants } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';

interface SidebarProps {
  categories: Category[];
  sheets: CheatsheetSummary[];
}

export function Sidebar({ categories, sheets }: SidebarProps) {
  const router = useRouter();

  async function handleLogout() {
    await fetch('/api/auth/logout', { method: 'POST' });
    router.push('/login');
    router.refresh();
  }

  const sheetsByCategory = (catSlug: string) =>
    sheets.filter((s) => s.categorySlug === catSlug);

  return (
    <div className="flex flex-col h-full px-3 py-4 gap-2">
      <div className="flex items-center justify-between px-2 mb-2">
        <span className="font-semibold text-sm tracking-wide">Cheatsheets</span>
        <ThemeToggle />
      </div>

      <div className="flex gap-1">
        <Link
          href="/sheets/new"
          className={buttonVariants({ variant: 'outline', size: 'sm', className: 'flex-1 gap-1 h-8' })}
        >
          <Plus className="h-3.5 w-3.5" />
          New
        </Link>
        <ImportButton />
      </div>

      <Separator className="my-1 opacity-20" />

      <nav className="flex-1 overflow-y-auto space-y-1">
        {categories.map((cat) => (
          <SidebarCategory
            key={cat.id}
            category={cat}
            sheets={sheetsByCategory(cat.slug)}
            defaultOpen
          />
        ))}
        {categories.length === 0 && (
          <p className="text-xs text-center text-on-surface-muted py-4">
            No categories yet. Create your first sheet to get started.
          </p>
        )}
      </nav>

      <Separator className="my-1 opacity-20" />

      <Button
        variant="ghost"
        size="sm"
        className="w-full justify-start gap-2 text-on-surface-muted hover:text-on-surface"
        onClick={handleLogout}
      >
        <LogOut className="h-4 w-4" />
        Sign out
      </Button>
    </div>
  );
}

function ImportButton() {
  function handleClick() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.md,.markdown,.html,.htm';
    input.onchange = async (e) => {
      const file = (e.target as HTMLInputElement).files?.[0];
      if (!file) return;
      const fd = new FormData();
      fd.append('file', file);
      try {
        const res = await fetch('/api/import', { method: 'POST', body: fd });
        if (res.ok) {
          const data = await res.json() as { categorySlug: string; slug: string };
          window.location.href = `/sheets/${data.categorySlug}/${data.slug}`;
        }
      } catch {
        // Errors surfaced by the server-side import handler
      }
    };
    input.click();
  }

  return (
    <Button
      variant="outline"
      size="sm"
      className="h-8 px-2"
      onClick={handleClick}
      title="Import .md or .html"
    >
      <Upload className="h-3.5 w-3.5" />
    </Button>
  );
}
