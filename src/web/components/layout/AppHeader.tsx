'use client';

import { useState } from 'react';
import { Menu, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { SidebarDrawer } from './SidebarDrawer';
import type { Category, CheatsheetSummary } from '@/lib/types';

interface AppHeaderProps {
  categories: Category[];
  sheets: CheatsheetSummary[];
  onSearchOpen: () => void;
}

export function AppHeader({
  categories,
  sheets,
  onSearchOpen,
}: AppHeaderProps) {
  const [drawerOpen, setDrawerOpen] = useState(false);

  return (
    <>
      <header className="lg:hidden flex items-center justify-between px-4 py-3 glass border-b border-white/10 sticky top-0 z-20">
        <Button
          variant="ghost"
          size="icon"
          aria-label="Open navigation"
          onClick={() => setDrawerOpen(true)}
        >
          <Menu className="h-5 w-5" />
        </Button>
        <span className="font-semibold text-sm">Cheatsheets</span>
        <Button
          variant="ghost"
          size="icon"
          aria-label="Search"
          onClick={onSearchOpen}
        >
          <Search className="h-5 w-5" />
        </Button>
      </header>
      <SidebarDrawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        categories={categories}
        sheets={sheets}
      />
    </>
  );
}
