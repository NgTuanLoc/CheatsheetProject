'use client';

import { useState } from 'react';
import type { Category, CheatsheetSummary } from '@/lib/types';
import { Sidebar } from './Sidebar';
import { AppHeader } from './AppHeader';
import { CommandPalette } from '@/components/search/CommandPalette';

interface AppLayoutProps {
  categories: Category[];
  sheets: CheatsheetSummary[];
  children: React.ReactNode;
}

export function AppLayout({ categories, sheets, children }: AppLayoutProps) {
  const [paletteOpen, setPaletteOpen] = useState(false);

  return (
    <div className="flex flex-col lg:flex-row h-screen overflow-hidden">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex lg:flex-col lg:w-72 lg:shrink-0 border-r border-white/10">
        <Sidebar categories={categories} sheets={sheets} />
      </aside>

      <div className="flex-1 flex flex-col overflow-hidden">
        <AppHeader
          categories={categories}
          sheets={sheets}
          onSearchOpen={() => setPaletteOpen(true)}
        />
        <main className="flex-1 overflow-y-auto p-4 lg:p-8">
          {children}
        </main>
      </div>

      <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} />
    </div>
  );
}
