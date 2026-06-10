'use server';

import { authFetch } from './client';
import type {
  ApiResponse,
  Category,
  CheatsheetDetail,
  CreateCheatsheetRequest,
  UpdateCheatsheetRequest,
  SaveCategoryRequest,
} from '@/lib/types';
import { revalidatePath } from 'next/cache';

export async function deleteSheetAction(id: number): Promise<ApiResponse<null>> {
  const res = await authFetch<null>(`/cheatsheets/${id}`, { method: 'DELETE' });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function createSheetAction(
  data: CreateCheatsheetRequest,
): Promise<ApiResponse<CheatsheetDetail>> {
  const res = await authFetch<CheatsheetDetail>('/cheatsheets', {
    method: 'POST',
    body: JSON.stringify(data),
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function updateSheetAction(
  id: number,
  data: UpdateCheatsheetRequest,
): Promise<ApiResponse<CheatsheetDetail>> {
  const res = await authFetch<CheatsheetDetail>(`/cheatsheets/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function createCategoryAction(
  data: SaveCategoryRequest,
): Promise<ApiResponse<Category>> {
  const res = await authFetch<Category>('/categories', {
    method: 'POST',
    body: JSON.stringify(data),
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function updateCategoryAction(
  id: number,
  data: SaveCategoryRequest,
): Promise<ApiResponse<Category>> {
  const res = await authFetch<Category>(`/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function deleteCategoryAction(
  id: number,
): Promise<ApiResponse<null>> {
  const res = await authFetch<null>(`/categories/${id}`, {
    method: 'DELETE',
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}
