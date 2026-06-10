import { notFound } from 'next/navigation';
import { getCheatsheet } from '@/lib/api/cheatsheets';
import { getCategories } from '@/lib/api/categories';
import { SheetForm } from '@/components/editor/SheetForm';

interface Props {
  params: Promise<{ category: string; slug: string }>;
}

export default async function EditSheetPage({ params }: Props) {
  const { category, slug } = await params;
  const [sheet, categories] = await Promise.all([
    getCheatsheet(category, slug),
    getCategories(),
  ]);
  if (!sheet) notFound();

  return (
    <div className="py-4">
      <h1 className="text-2xl font-bold mb-8">Edit — {sheet.title}</h1>
      <SheetForm categories={categories} sheet={sheet} />
    </div>
  );
}
