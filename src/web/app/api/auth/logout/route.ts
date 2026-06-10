import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';

export async function POST() {
  const store = await cookies();
  store.delete('token');
  return NextResponse.json({ success: true });
}
