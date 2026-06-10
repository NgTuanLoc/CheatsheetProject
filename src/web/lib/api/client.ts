import { getToken } from '@/lib/auth';
import type { ApiResponse } from '@/lib/types';

const API_URL =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.API_URL ??
  'http://localhost:5000';

export async function authFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<ApiResponse<T>> {
  const token = await getToken();
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
    cache: 'no-store',
  });
  if (!res.ok && res.status !== 400 && res.status !== 401 && res.status !== 404) {
    throw new Error(`API error ${res.status}: ${path}`);
  }
  return res.json() as Promise<ApiResponse<T>>;
}
