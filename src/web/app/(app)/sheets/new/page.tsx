import { getCategories } from '@/lib/api/categories';
import { SheetForm } from '@/components/editor/SheetForm';

export default async function NewSheetPage() {
  const categories = await getCategories();
  return (
    <div className="py-4">
      <h1 className="text-2xl font-bold mb-8">New cheatsheet</h1>
      <SheetForm categories={categories} />
    </div>
  );
}
