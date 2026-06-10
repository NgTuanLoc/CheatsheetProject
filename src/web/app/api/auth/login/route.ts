import { cookies } from 'next/headers';
import { NextRequest, NextResponse } from 'next/server';

const API_URL =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.API_URL ??
  'http://localhost:5000';

export async function POST(request: NextRequest) {
  const body = await request.json();
  let res: Response;
  try {
    res = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  } catch {
    return NextResponse.json(
      { error: "Couldn't reach the API." },
      { status: 503 },
    );
  }

  const data = await res.json();
  if (!res.ok || !data.success) {
    return NextResponse.json(
      { error: data.error ?? 'Login failed.' },
      { status: res.status },
    );
  }

  const store = await cookies();
  store.set('token', data.data.token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'strict',
    path: '/',
    maxAge: 7 * 24 * 60 * 60,
  });

  return NextResponse.json({ success: true });
}
