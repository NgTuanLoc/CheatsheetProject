import Link from 'next/link';
import { Pencil, Download } from 'lucide-react';
import { buttonVariants } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { DeleteConfirmDialog } from './DeleteConfirmDialog';
import type { CheatsheetDetail } from '@/lib/types';

interface SheetActionsProps {
  sheet: CheatsheetDetail;
}

export function SheetActions({ sheet }: SheetActionsProps) {
  return (
    <div className="flex items-center gap-2 flex-wrap">
      <Link
        href={`/sheets/${sheet.categorySlug}/${sheet.slug}/edit`}
        className={cn(buttonVariants({ variant: 'outline', size: 'sm' }), 'gap-1')}
      >
        <Pencil className="h-4 w-4" />
        Edit
      </Link>
      <a
        href={`/api/export/${sheet.id}`}
        download
        className={cn(buttonVariants({ variant: 'outline', size: 'sm' }), 'gap-1')}
      >
        <Download className="h-4 w-4" />
        Export
      </a>
      <DeleteConfirmDialog sheetId={sheet.id} />
    </div>
  );
}
