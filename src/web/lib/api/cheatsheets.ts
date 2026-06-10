import { authFetch } from './client';
import type {
  ApiResponse,
  CheatsheetSummary,
  CheatsheetDetail,
  CreateCheatsheetRequest,
  UpdateCheatsheetRequest,
} from '@/lib/types';

export async function getCheatsheets(params?: {
  category?: string;
  tag?: string;
  q?: string;
}): Promise<CheatsheetSummary[]> {
  const qs = new URLSearchParams();
  if (params?.category) qs.set('category', params.category);
  if (params?.tag) qs.set('tag', params.tag);
  if (params?.q) qs.set('q', params.q);
  const suffix = qs.size ? `?${qs}` : '';
  const res = await authFetch<CheatsheetSummary[]>(`/cheatsheets${suffix}`);
  return res.data ?? [];
}

export async function getCheatsheet(
  categorySlug: string,
  slug: string,
): Promise<CheatsheetDetail | null> {
  const res = await authFetch<CheatsheetDetail>(`/cheatsheets/${categorySlug}/${slug}`);
  return res.data;
}

export async function createCheatsheet(
  data: CreateCheatsheetRequest,
): Promise<ApiResponse<CheatsheetDetail>> {
  return authFetch<CheatsheetDetail>('/cheatsheets', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateCheatsheet(
  id: number,
  data: UpdateCheatsheetRequest,
): Promise<ApiResponse<CheatsheetDetail>> {
  return authFetch<CheatsheetDetail>(`/cheatsheets/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function deleteCheatsheet(id: number): Promise<ApiResponse<null>> {
  return authFetch<null>(`/cheatsheets/${id}`, { method: 'DELETE' });
}
