import { redirect } from 'next/navigation';
import { getCheatsheets } from '@/lib/api/cheatsheets';

export default async function HomePage() {
  const sheets = await getCheatsheets();
  if (sheets.length === 0) {
    redirect('/sheets/new');
  }
  const latest = sheets[0];
  redirect(`/sheets/${latest.categorySlug}/${latest.slug}`);
}
