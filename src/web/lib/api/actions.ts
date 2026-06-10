'use server';

import { authFetch } from './client';
import type { ApiResponse } from '@/lib/types';
import { revalidatePath } from 'next/cache';

// Stub — full implementation in Task 8
export async function deleteSheetAction(id: number): Promise<ApiResponse<null>> {
  const res = await authFetch<null>(`/cheatsheets/${id}`, { method: 'DELETE' });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

// Remaining actions (createSheetAction, updateSheetAction, category actions)
// will be added in Task 8
