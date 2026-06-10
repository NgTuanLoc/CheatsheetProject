import { authFetch } from './client';
import type { ApiResponse, Category, SaveCategoryRequest } from '@/lib/types';

export async function getCategories(): Promise<Category[]> {
  const res = await authFetch<Category[]>('/categories');
  return res.data ?? [];
}

export async function createCategory(data: SaveCategoryRequest): Promise<ApiResponse<Category>> {
  return authFetch<Category>('/categories', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateCategory(id: number, data: SaveCategoryRequest): Promise<ApiResponse<Category>> {
  return authFetch<Category>(`/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function deleteCategory(id: number): Promise<ApiResponse<null>> {
  return authFetch<null>(`/categories/${id}`, { method: 'DELETE' });
}
