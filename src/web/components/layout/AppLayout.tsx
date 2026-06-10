'use client';

import { useState } from 'react';
import type { Category, CheatsheetSummary } from '@/lib/types';

interface AppLayoutProps {
  categories: Category[];
  sheets: CheatsheetSummary[];
  children: React.ReactNode;
}

export function AppLayout({ categories: _categories, sheets: _sheets, children }: AppLayoutProps) {
  const [_paletteOpen, _setPaletteOpen] = useState(false);

  return (
    <div className="flex flex-col lg:flex-row h-screen overflow-hidden">
      {/* Desktop sidebar placeholder — filled in Task 6 */}
      <aside className="hidden lg:flex lg:flex-col lg:w-72 lg:shrink-0 border-r border-white/10 glass" />

      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Mobile header placeholder — filled in Task 6 */}
        <header className="lg:hidden flex items-center px-4 py-3 glass border-b border-white/10 sticky top-0 z-20">
          <span className="font-semibold text-sm">Cheatsheets</span>
        </header>
        <main className="flex-1 overflow-y-auto p-4 lg:p-8">
          {children}
        </main>
      </div>
    </div>
  );
}
