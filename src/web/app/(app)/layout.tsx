import { redirect } from 'next/navigation';
import { getToken } from '@/lib/auth';
import { getCategories } from '@/lib/api/categories';
import { getCheatsheets } from '@/lib/api/cheatsheets';
import { AppLayout } from '@/components/layout/AppLayout';

export default async function AuthenticatedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const token = await getToken();
  if (!token) redirect('/login');

  const [categories, sheets] = await Promise.all([
    getCategories(),
    getCheatsheets(),
  ]);

  return (
    <AppLayout categories={categories} sheets={sheets}>
      {children}
    </AppLayout>
  );
}
