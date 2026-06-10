import { NextRequest, NextResponse } from 'next/server';
import { getToken } from '@/lib/auth';

const API_URL =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.API_URL ??
  'http://localhost:5000';

export async function GET(
  _req: NextRequest,
  { params }: { params: Promise<{ id: string }> },
) {
  const { id } = await params;
  const token = await getToken();
  const res = await fetch(`${API_URL}/cheatsheets/${id}/export`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  if (!res.ok) {
    return NextResponse.json({ error: 'Export failed' }, { status: res.status });
  }
  const blob = await res.blob();
  const disposition =
    res.headers.get('content-disposition') ?? 'attachment; filename="export"';
  return new NextResponse(blob, {
    headers: {
      'Content-Type':
        res.headers.get('content-type') ?? 'application/octet-stream',
      'Content-Disposition': disposition,
    },
  });
}
