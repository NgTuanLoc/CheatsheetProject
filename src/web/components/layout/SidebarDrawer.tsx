'use client';

import {
  Sheet,
  SheetContent,
} from '@/components/ui/sheet';
import { Sidebar } from './Sidebar';
import type { Category, CheatsheetSummary } from '@/lib/types';

interface SidebarDrawerProps {
  open: boolean;
  onClose: () => void;
  categories: Category[];
  sheets: CheatsheetSummary[];
}

export function SidebarDrawer({
  open,
  onClose,
  categories,
  sheets,
}: SidebarDrawerProps) {
  return (
    <Sheet open={open} onOpenChange={(o) => !o && onClose()}>
      <SheetContent side="left" className="p-0 w-72 glass border-r-white/10">
        <Sidebar categories={categories} sheets={sheets} />
      </SheetContent>
    </Sheet>
  );
}
