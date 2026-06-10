import { NextRequest, NextResponse } from 'next/server';
import { getToken } from '@/lib/auth';

const API_URL =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.API_URL ??
  'http://localhost:5000';

export async function POST(request: NextRequest) {
  const token = await getToken();
  if (!token) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const formData = await request.formData();
  let res: Response;
  try {
    res = await fetch(`${API_URL}/cheatsheets/import`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: formData,
    });
  } catch {
    return NextResponse.json({ error: "Couldn't reach the API." }, { status: 503 });
  }

  const data = await res.json();
  if (!res.ok || !data.success) {
    return NextResponse.json(
      { error: data.error ?? 'Import failed' },
      { status: res.status },
    );
  }

  return NextResponse.json({
    success: true,
    categorySlug: data.data.categorySlug,
    slug: data.data.slug,
  });
}
