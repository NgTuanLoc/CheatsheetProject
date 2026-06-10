import { NextRequest, NextResponse } from 'next/server';
import { getToken } from '@/lib/auth';

const API_URL =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.API_URL ??
  'http://localhost:5000';

export async function GET(request: NextRequest) {
  const q = request.nextUrl.searchParams.get('q') ?? '';
  const token = await getToken();
  const qs = q.trim() ? `?q=${encodeURIComponent(q)}` : '';
  const res = await fetch(`${API_URL}/cheatsheets${qs}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    cache: 'no-store',
  });
  const data = await res.json();
  return NextResponse.json(data);
}
