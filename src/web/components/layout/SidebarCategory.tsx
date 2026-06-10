'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronDown, ChevronRight, FileText } from 'lucide-react';
import type { Category, CheatsheetSummary } from '@/lib/types';
import { cn } from '@/lib/utils';

interface SidebarCategoryProps {
  category: Category;
  sheets: CheatsheetSummary[];
  defaultOpen?: boolean;
}

export function SidebarCategory({
  category,
  sheets,
  defaultOpen = false,
}: SidebarCategoryProps) {
  const [open, setOpen] = useState(defaultOpen);
  const pathname = usePathname();

  return (
    <div>
      <button
        onClick={() => setOpen((o) => !o)}
        className="flex items-center gap-2 w-full px-3 py-2 rounded-lg text-sm font-medium hover:bg-white/5 transition-colors text-left"
      >
        {open ? (
          <ChevronDown className="h-4 w-4 shrink-0 text-on-surface-muted" />
        ) : (
          <ChevronRight className="h-4 w-4 shrink-0 text-on-surface-muted" />
        )}
        <span className="truncate flex-1">
          {category.icon ? `${category.icon} ` : ''}
          {category.name}
        </span>
        <span className="text-xs text-on-surface-muted tabular-nums">{sheets.length}</span>
      </button>

      {open && sheets.length > 0 && (
        <ul className="ml-4 space-y-0.5 mt-0.5">
          {sheets.map((sheet) => {
            const href = `/sheets/${sheet.categorySlug}/${sheet.slug}`;
            const isActive = pathname === href;
            return (
              <li key={sheet.id}>
                <Link
                  href={href}
                  className={cn(
                    'flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm transition-colors',
                    isActive
                      ? 'bg-accent text-accent-foreground font-medium'
                      : 'hover:bg-white/5 text-on-surface-muted hover:text-on-surface',
                  )}
                >
                  <FileText className="h-3 w-3 shrink-0" />
                  <span className="truncate">{sheet.title}</span>
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
