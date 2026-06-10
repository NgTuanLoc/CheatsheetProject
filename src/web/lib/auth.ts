import { cookies } from 'next/headers';

export async function getToken(): Promise<string | null> {
  const store = await cookies();
  return store.get('token')?.value ?? null;
}
