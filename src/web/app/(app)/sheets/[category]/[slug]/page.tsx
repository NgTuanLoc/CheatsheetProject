import { notFound } from 'next/navigation';
import { getCheatsheet } from '@/lib/api/cheatsheets';
import { MarkdownRenderer } from '@/components/cheatsheet/MarkdownRenderer';
import { HtmlRenderer } from '@/components/cheatsheet/HtmlRenderer';
import { SheetActions } from '@/components/cheatsheet/SheetActions';
import { Badge } from '@/components/ui/badge';

interface Props {
  params: Promise<{ category: string; slug: string }>;
}

export default async function SheetPage({ params }: Props) {
  const { category, slug } = await params;
  const sheet = await getCheatsheet(category, slug);
  if (!sheet) notFound();

  return (
    <article className="max-w-4xl mx-auto space-y-6">
      <header className="space-y-3">
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <h1 className="text-3xl font-bold">{sheet.title}</h1>
          <SheetActions sheet={sheet} />
        </div>
        {sheet.tags.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {sheet.tags.map((tag) => (
              <Badge key={tag} variant="secondary" className="glass">
                {tag}
              </Badge>
            ))}
          </div>
        )}
        <p className="text-xs text-on-surface-muted">
          Updated {new Date(sheet.updatedAt).toLocaleDateString()}
        </p>
      </header>

      <div className="glass p-6 lg:p-8 rounded-xl">
        {sheet.contentType === 'markdown' ? (
          <MarkdownRenderer content={sheet.content} />
        ) : (
          <HtmlRenderer content={sheet.content} />
        )}
      </div>
    </article>
  );
}
