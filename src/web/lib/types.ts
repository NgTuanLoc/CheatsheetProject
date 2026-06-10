export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: string | null;
}

export interface Category {
  id: number;
  name: string;
  slug: string;
  icon: string | null;
  sortOrder: number;
}

export interface CheatsheetSummary {
  id: number;
  title: string;
  slug: string;
  categorySlug: string;
  contentType: 'markdown' | 'html';
  updatedAt: string;
  tags: string[];
}

export interface CheatsheetDetail {
  id: number;
  title: string;
  slug: string;
  categoryId: number;
  categorySlug: string;
  contentType: 'markdown' | 'html';
  content: string;
  createdAt: string;
  updatedAt: string;
  tags: string[];
}

export interface Tag {
  id: number;
  name: string;
  slug: string;
  count: number;
}

export interface CreateCheatsheetRequest {
  title: string;
  categoryId: number;
  contentType: 'markdown' | 'html';
  content: string;
  tags: string[];
}

export interface UpdateCheatsheetRequest {
  title: string;
  categoryId: number;
  content: string;
  tags: string[];
}

export interface SaveCategoryRequest {
  name: string;
  icon?: string;
  sortOrder?: number;
}
