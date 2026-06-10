import { authFetch } from './client';
import type { Tag } from '@/lib/types';

export async function getTags(): Promise<Tag[]> {
  const res = await authFetch<Tag[]>('/tags');
  return res.data ?? [];
}
