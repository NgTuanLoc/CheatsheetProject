# Cheatsheet App — Plan 2 of 3: Next.js 16 Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Next.js 16 frontend for the cheatsheet manager — Aurora glassmorphism theme, Milkdown markdown editor, Shiki code highlighting, sandboxed HTML rendering, Ctrl+K command palette, and full BFF auth pattern with Vitest unit tests and Playwright E2E coverage.

**Architecture:** Next.js 16 App Router (React 19) lives in `src/web/`. All API traffic is server-side only (BFF pattern): RSC data-fetching for reads, Server Actions for mutations. The JWT lives in an HttpOnly/SameSite=strict cookie set by a Next.js route handler — the browser never sees the token and never calls the .NET API directly. Aspire AppHost wires the Next.js dev server as a first-class resource so `dotnet run --project src/CheatsheetApp.AppHost` starts everything.

**Tech Stack:** Next.js 16, React 19, TypeScript, Tailwind CSS v4 (CSS-first, no config file), shadcn/ui, next-themes, Milkdown Crepe (editor), unified + remark + `@shikijs/rehype` (markdown RSC rendering), Sonner (toasts), Vitest + React Testing Library (unit), Playwright (E2E).

**Conventions for all tasks:**
- Run all commands from `src/web/` unless stated otherwise.
- Env var `API_URL` (injected by Aspire as `services__api__https__0`) holds the backend base URL.
- Every mutation goes through a Server Action or a Next.js route handler — never a direct client-side `fetch` to the .NET API.
- One detail route deviation from the spec table (inherited from Plan 1): `GET /cheatsheets/{categorySlug}/{slug}` — the frontend uses `/sheets/[category]/[slug]` to match.

---

## File Map

### New files (all under `src/web/` unless noted)

```
src/web/
├── package.json
├── next.config.ts
├── tsconfig.json
├── postcss.config.mjs
├── vitest.config.ts
├── playwright.config.ts
├── middleware.ts                              # Redirect unauthenticated visitors to /login
├── app/
│   ├── globals.css                            # @import "tailwindcss" + @theme Aurora tokens + prose
│   ├── layout.tsx                             # Root layout: ThemeProvider, Inter font, Toaster
│   ├── login/
│   │   └── page.tsx                           # Login page — renders <LoginForm />
│   ├── api/
│   │   └── auth/
│   │       ├── login/route.ts                 # POST: forward creds to API, set HttpOnly cookie
│   │       └── logout/route.ts                # POST: clear cookie, return 200
│   └── (app)/                                 # Route group — shares AppLayout
│       ├── layout.tsx                         # Fetches categories + summaries, renders AppLayout
│       ├── page.tsx                           # / — server redirect to most-recent sheet URL
│       └── sheets/
│           ├── new/
│           │   └── page.tsx                   # Create sheet page
│           └── [category]/
│               └── [slug]/
│                   ├── page.tsx               # Reading view
│                   └── edit/
│                       └── page.tsx           # Edit view
├── components/
│   ├── layout/
│   │   ├── AppLayout.tsx                      # Shell: sidebar slot + main content area
│   │   ├── Sidebar.tsx                        # Desktop persistent sidebar (client component)
│   │   ├── SidebarDrawer.tsx                  # Mobile slide-over drawer (client component)
│   │   ├── SidebarCategory.tsx                # Collapsible category group + sheet link list
│   │   └── AppHeader.tsx                      # Mobile header: hamburger + Ctrl+K search button
│   ├── theme/
│   │   └── ThemeToggle.tsx                    # Sun/moon icon button using next-themes
│   ├── auth/
│   │   └── LoginForm.tsx                      # Client form: username + password inputs
│   ├── search/
│   │   └── CommandPalette.tsx                 # Ctrl+K dialog, calls GET /cheatsheets?q=
│   ├── cheatsheet/
│   │   ├── MarkdownRenderer.tsx               # RSC: unified pipeline → HTML string
│   │   ├── RenderedMarkdown.tsx               # Client island: renders HTML + injects copy buttons
│   │   ├── HtmlRenderer.tsx                   # Sandboxed <iframe srcDoc> (client component)
│   │   ├── SheetActions.tsx                   # Edit / Export / Delete action buttons
│   │   └── DeleteConfirmDialog.tsx            # Confirm dialog before hard delete
│   └── editor/
│       ├── SheetForm.tsx                      # Title + category picker + TagInput + editor area
│       ├── TagInput.tsx                       # Tag chip input (add/remove tags)
│       └── MarkdownEditor.tsx                 # Milkdown Crepe wrapper ('use client')
├── lib/
│   ├── types.ts                               # TypeScript interfaces matching API response shapes
│   ├── auth.ts                                # getToken() — reads HttpOnly cookie server-side
│   └── api/
│       ├── client.ts                          # authFetch(): fetch wrapper that attaches Bearer token
│       ├── actions.ts                         # Server Actions: createSheet, updateSheet, deleteSheet, etc.
│       ├── categories.ts                      # getCategories(), createCategory(), updateCategory(), deleteCategory()
│       ├── cheatsheets.ts                     # getCheatsheets(), getCheatsheet(), searchCheatsheets()
│       └── tags.ts                            # getTags()
└── tests/
    ├── unit/
    │   ├── CommandPalette.test.tsx
    │   ├── SidebarCategory.test.tsx
    │   └── TagInput.test.tsx
    └── e2e/
        ├── login.spec.ts
        ├── crud.spec.ts
        ├── search.spec.ts
        └── import-html.spec.ts
```

### Files to modify

- `src/CheatsheetApp.AppHost/AppHost.cs` — add Next.js dev server as Aspire `AddNpmApp` resource

---

## Task 1: Project scaffold

**Files:**
- Create: `src/web/` (entire Next.js project)

- [ ] **Step 1: Scaffold Next.js 16 app**

Run from repo root `D:\Projects\CheatsheetProject\src`:

```powershell
npx create-next-app@latest web --typescript --eslint --app --no-src-dir --no-tailwind --import-alias "@/*" --yes
```

Expected: `src/web/` created with `app/`, `public/`, `next.config.ts`, `tsconfig.json`, `package.json`.

- [ ] **Step 2: Install Tailwind CSS v4 and PostCSS**

```powershell
cd web
npm install tailwindcss@next @tailwindcss/postcss postcss
```

- [ ] **Step 3: Install all application dependencies**

```powershell
npm install next-themes sonner lucide-react clsx tailwind-merge class-variance-authority
npm install unified remark-parse remark-rehype remark-gfm rehype-stringify rehype-raw @shikijs/rehype
npm install @milkdown/crepe @milkdown/react @milkdown/kit
```

- [ ] **Step 4: Install shadcn/ui CLI and init**

```powershell
npx shadcn@latest init --defaults
```

When prompted, select: Style = Default, Base color = Neutral, CSS variables = yes.

Then add required components:

```powershell
npx shadcn@latest add button input label dialog alert-dialog command sheet badge tooltip separator scroll-area
```

- [ ] **Step 5: Install dev/test dependencies**

```powershell
npm install -D vitest @vitejs/plugin-react @testing-library/react @testing-library/user-event jsdom @types/jsdom
npm install -D @playwright/test
npx playwright install chromium
```

- [ ] **Step 6: Create `postcss.config.mjs`**

Delete any existing `postcss.config.js` then create:

```js
// postcss.config.mjs
export default {
  plugins: {
    '@tailwindcss/postcss': {},
  },
}
```

- [ ] **Step 7: Create `vitest.config.ts`**

```ts
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    globals: true,
    coverage: {
      provider: 'v8',
      thresholds: { lines: 80, functions: 80, branches: 80 },
    },
  },
  resolve: {
    alias: { '@': path.resolve(__dirname, '.') },
  },
});
```

- [ ] **Step 8: Create `tests/setup.ts`**

```ts
import '@testing-library/jest-dom';
```

Install the matcher package:

```powershell
npm install -D @testing-library/jest-dom
```

- [ ] **Step 9: Create `playwright.config.ts`**

```ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false,
  retries: 0,
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
  },
});
```

- [ ] **Step 10: Add scripts to `package.json`**

Edit `package.json` scripts section:

```json
{
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start",
    "lint": "next lint",
    "test": "vitest run",
    "test:watch": "vitest",
    "test:coverage": "vitest run --coverage",
    "test:e2e": "playwright test"
  }
}
```

- [ ] **Step 11: Verify build compiles**

```powershell
npm run build
```

Expected: build succeeds (may show empty app pages — that's fine).

- [ ] **Step 12: Commit**

From repo root:

```powershell
git add src/web
git commit -m "chore: scaffold Next.js 16 frontend with Tailwind v4, shadcn/ui, Vitest, Playwright"
```

---

## Task 2: Aurora theme system

**Files:**
- Create: `app/globals.css`, `app/layout.tsx`, `components/theme/ThemeToggle.tsx`

- [ ] **Step 1: Write failing test for ThemeToggle**

Create `tests/unit/ThemeToggle.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeToggle } from '@/components/theme/ThemeToggle';

// Mock next-themes
vi.mock('next-themes', () => ({
  useTheme: vi.fn(() => ({ theme: 'light', setTheme: vi.fn() })),
}));

import { useTheme } from 'next-themes';

describe('ThemeToggle', () => {
  it('renders a toggle button', () => {
    render(<ThemeToggle />);
    expect(screen.getByRole('button')).toBeInTheDocument();
  });

  it('calls setTheme with dark when current theme is light', async () => {
    const setTheme = vi.fn();
    vi.mocked(useTheme).mockReturnValue({ theme: 'light', setTheme } as any);
    render(<ThemeToggle />);
    await userEvent.click(screen.getByRole('button'));
    expect(setTheme).toHaveBeenCalledWith('dark');
  });

  it('calls setTheme with light when current theme is dark', async () => {
    const setTheme = vi.fn();
    vi.mocked(useTheme).mockReturnValue({ theme: 'dark', setTheme } as any);
    render(<ThemeToggle />);
    await userEvent.click(screen.getByRole('button'));
    expect(setTheme).toHaveBeenCalledWith('light');
  });
});
```

- [ ] **Step 2: Run test — expect FAIL**

```powershell
npm test -- ThemeToggle
```

Expected: `Cannot find module '@/components/theme/ThemeToggle'`

- [ ] **Step 3: Create `components/theme/ThemeToggle.tsx`**

```tsx
'use client';

import { useTheme } from 'next-themes';
import { Moon, Sun } from 'lucide-react';
import { Button } from '@/components/ui/button';

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  return (
    <Button
      variant="ghost"
      size="icon"
      aria-label="Toggle theme"
      onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
    >
      <Sun className="h-4 w-4 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
      <Moon className="absolute h-4 w-4 rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
    </Button>
  );
}
```

- [ ] **Step 4: Run test — expect PASS**

```powershell
npm test -- ThemeToggle
```

Expected: 3 tests passing.

- [ ] **Step 5: Create `app/globals.css`**

Replace the entire file:

```css
@import "tailwindcss";

/* ─── Aurora theme tokens ─────────────────────────────────────────────── */
@theme {
  --color-glass-bg: oklch(100% 0 0 / 0.07);
  --color-glass-border: oklch(100% 0 0 / 0.15);
  --color-glass-highlight: oklch(100% 0 0 / 0.25);

  --color-accent-primary: oklch(65% 0.25 290);   /* indigo */
  --color-accent-secondary: oklch(65% 0.28 330);  /* pink */
  --color-accent-tertiary: oklch(70% 0.22 200);   /* cyan */

  --color-surface: oklch(15% 0.02 270);
  --color-surface-raised: oklch(18% 0.02 270);
  --color-on-surface: oklch(92% 0.01 270);
  --color-on-surface-muted: oklch(65% 0.01 270);

  --blur-glass: blur(12px);
  --radius-glass: 0.75rem;

  --font-sans: 'Inter', ui-sans-serif, system-ui, sans-serif;
}

/* Light mode overrides */
.light, [data-theme="aurora"].light {
  --color-glass-bg: oklch(100% 0 0 / 0.55);
  --color-glass-border: oklch(0% 0 0 / 0.1);
  --color-surface: oklch(96% 0.005 270);
  --color-surface-raised: oklch(100% 0 0);
  --color-on-surface: oklch(15% 0.02 270);
  --color-on-surface-muted: oklch(45% 0.01 270);
}

/* ─── Base ────────────────────────────────────────────────────────────── */
body {
  background-color: var(--color-surface);
  color: var(--color-on-surface);
  font-family: var(--font-sans);
}

/* Dark mode gradient backdrop */
.dark body {
  background:
    radial-gradient(ellipse 60% 50% at 20% 20%, oklch(45% 0.25 290 / 0.35) 0%, transparent 70%),
    radial-gradient(ellipse 50% 40% at 80% 80%, oklch(45% 0.28 330 / 0.3) 0%, transparent 70%),
    radial-gradient(ellipse 40% 50% at 60% 10%, oklch(50% 0.22 200 / 0.2) 0%, transparent 60%),
    var(--color-surface);
  background-attachment: fixed;
}

/* ─── Glass panel utility ─────────────────────────────────────────────── */
.glass {
  background: var(--color-glass-bg);
  border: 1px solid var(--color-glass-border);
  backdrop-filter: var(--blur-glass);
  border-radius: var(--radius-glass);
}

/* ─── Prose / markdown styles ─────────────────────────────────────────── */
.prose {
  color: var(--color-on-surface);
  max-width: 72ch;
}

.prose h1, .prose h2, .prose h3, .prose h4 {
  color: var(--color-on-surface);
  font-weight: 700;
  margin-top: 1.5em;
  margin-bottom: 0.5em;
}

.prose h1 { font-size: 1.875rem; }
.prose h2 { font-size: 1.5rem; }
.prose h3 { font-size: 1.25rem; }

.prose p { margin-bottom: 1em; line-height: 1.7; }

.prose a {
  color: var(--color-accent-primary);
  text-decoration: underline;
  text-decoration-color: oklch(65% 0.25 290 / 0.4);
}

.prose ul, .prose ol { padding-left: 1.5em; margin-bottom: 1em; }
.prose li { margin-bottom: 0.25em; }

.prose blockquote {
  border-left: 3px solid var(--color-accent-primary);
  padding-left: 1em;
  color: var(--color-on-surface-muted);
  font-style: italic;
}

.prose code:not(pre code) {
  background: var(--color-glass-bg);
  border: 1px solid var(--color-glass-border);
  border-radius: 0.25rem;
  padding: 0.1em 0.4em;
  font-size: 0.875em;
}

.prose pre {
  position: relative;
  border-radius: 0.75rem;
  overflow-x: auto;
}

.prose pre code {
  display: block;
  padding: 1rem 1.25rem;
  font-size: 0.875rem;
  line-height: 1.6;
}

/* Shiki dual-theme: hide inactive theme variant */
.dark .shiki.github-light { display: none; }
.light .shiki.catppuccin-mocha, :root:not(.dark) .shiki.catppuccin-mocha { display: none; }
```

- [ ] **Step 6: Create `app/layout.tsx`**

```tsx
import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import { ThemeProvider } from 'next-themes';
import { Toaster } from 'sonner';
import './globals.css';

const inter = Inter({ subsets: ['latin'], variable: '--font-sans' });

export const metadata: Metadata = {
  title: 'Cheatsheets',
  description: 'Personal cheatsheet manager',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={inter.variable}>
        <ThemeProvider attribute="class" defaultTheme="dark" enableSystem>
          {children}
          <Toaster richColors position="bottom-right" />
        </ThemeProvider>
      </body>
    </html>
  );
}
```

- [ ] **Step 7: Run all tests — expect PASS**

```powershell
npm test
```

- [ ] **Step 8: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/app/globals.css src/web/app/layout.tsx src/web/components/theme/ThemeToggle.tsx src/web/tests/unit/ThemeToggle.test.tsx
git commit -m "feat: Aurora theme system — Tailwind v4 tokens, ThemeProvider, ThemeToggle"
```

---

## Task 3: API types and client library

**Files:**
- Create: `lib/types.ts`, `lib/auth.ts`, `lib/api/client.ts`, `lib/api/categories.ts`, `lib/api/cheatsheets.ts`, `lib/api/tags.ts`

These are pure server-side modules — no tests required (they are integration-tested end-to-end).

- [ ] **Step 1: Create `lib/types.ts`**

```ts
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
```

- [ ] **Step 2: Create `lib/auth.ts`**

```ts
import { cookies } from 'next/headers';

export async function getToken(): Promise<string | null> {
  const store = await cookies();
  return store.get('token')?.value ?? null;
}
```

- [ ] **Step 3: Create `lib/api/client.ts`**

```ts
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
```

- [ ] **Step 4: Create `lib/api/categories.ts`**

```ts
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
```

- [ ] **Step 5: Create `lib/api/cheatsheets.ts`**

```ts
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

export async function getCheatsheet(categorySlug: string, slug: string): Promise<CheatsheetDetail | null> {
  const res = await authFetch<CheatsheetDetail>(`/cheatsheets/${categorySlug}/${slug}`);
  return res.data;
}

export async function createCheatsheet(data: CreateCheatsheetRequest): Promise<ApiResponse<CheatsheetDetail>> {
  return authFetch<CheatsheetDetail>('/cheatsheets', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateCheatsheet(id: number, data: UpdateCheatsheetRequest): Promise<ApiResponse<CheatsheetDetail>> {
  return authFetch<CheatsheetDetail>(`/cheatsheets/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function deleteCheatsheet(id: number): Promise<ApiResponse<null>> {
  return authFetch<null>(`/cheatsheets/${id}`, { method: 'DELETE' });
}
```

- [ ] **Step 6: Create `lib/api/tags.ts`**

```ts
import { authFetch } from './client';
import type { Tag } from '@/lib/types';

export async function getTags(): Promise<Tag[]> {
  const res = await authFetch<Tag[]>('/tags');
  return res.data ?? [];
}
```

- [ ] **Step 7: Verify TypeScript compiles**

```powershell
npx tsc --noEmit
```

Expected: no errors.

- [ ] **Step 8: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/lib
git commit -m "feat: API types and server-side client library (BFF)"
```

---

## Task 4: Auth — BFF login/logout, middleware, login page

**Files:**
- Create: `middleware.ts`, `app/api/auth/login/route.ts`, `app/api/auth/logout/route.ts`, `components/auth/LoginForm.tsx`, `app/login/page.tsx`

- [ ] **Step 1: Write failing test for LoginForm**

Create `tests/unit/LoginForm.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LoginForm } from '@/components/auth/LoginForm';

// Minimal fetch mock
global.fetch = vi.fn();

describe('LoginForm', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders username and password fields with a submit button', () => {
    render(<LoginForm />);
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('shows error message when API returns error', async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: false,
      json: async () => ({ error: 'Invalid username or password.' }),
    } as Response);

    render(<LoginForm />);
    await userEvent.type(screen.getByLabelText(/username/i), 'admin');
    await userEvent.type(screen.getByLabelText(/password/i), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText('Invalid username or password.')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test — expect FAIL**

```powershell
npm test -- LoginForm
```

Expected: `Cannot find module '@/components/auth/LoginForm'`

- [ ] **Step 3: Create `components/auth/LoginForm.tsx`**

```tsx
'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export function LoginForm() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    const fd = new FormData(e.currentTarget);
    const res = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: fd.get('username'),
        password: fd.get('password'),
      }),
    });
    const body = await res.json();
    setLoading(false);
    if (!res.ok) {
      setError(body.error ?? 'Login failed.');
      return;
    }
    router.push('/');
    router.refresh();
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 w-full">
      <div className="space-y-1">
        <Label htmlFor="username">Username</Label>
        <Input id="username" name="username" required autoComplete="username" />
      </div>
      <div className="space-y-1">
        <Label htmlFor="password">Password</Label>
        <Input id="password" name="password" type="password" required autoComplete="current-password" />
      </div>
      {error && <p className="text-sm text-red-500">{error}</p>}
      <Button type="submit" className="w-full" disabled={loading}>
        {loading ? 'Signing in…' : 'Sign in'}
      </Button>
    </form>
  );
}
```

- [ ] **Step 4: Run test — expect PASS**

```powershell
npm test -- LoginForm
```

- [ ] **Step 5: Create `app/api/auth/login/route.ts`**

```ts
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
    return NextResponse.json({ error: "Couldn't reach the API." }, { status: 503 });
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
```

- [ ] **Step 6: Create `app/api/auth/logout/route.ts`**

```ts
import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';

export async function POST() {
  const store = await cookies();
  store.delete('token');
  return NextResponse.json({ success: true });
}
```

- [ ] **Step 7: Create `middleware.ts`**

```ts
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

const PUBLIC_PATHS = ['/login', '/api/auth/login'];

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const isPublic = PUBLIC_PATHS.some((p) => pathname.startsWith(p));
  const token = request.cookies.get('token')?.value;

  if (!token && !isPublic) {
    const loginUrl = new URL('/login', request.url);
    loginUrl.searchParams.set('from', pathname);
    return NextResponse.redirect(loginUrl);
  }
  if (token && pathname === '/login') {
    return NextResponse.redirect(new URL('/', request.url));
  }
  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico|.*\\.png$).*)'],
};
```

- [ ] **Step 8: Create `app/login/page.tsx`**

```tsx
import { LoginForm } from '@/components/auth/LoginForm';

export default function LoginPage() {
  return (
    <main className="min-h-screen flex items-center justify-center">
      <div className="glass p-8 w-full max-w-sm space-y-6">
        <h1 className="text-2xl font-bold text-center">Cheatsheets</h1>
        <LoginForm />
      </div>
    </main>
  );
}
```

- [ ] **Step 9: Run all tests — expect PASS**

```powershell
npm test
```

- [ ] **Step 10: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/middleware.ts src/web/app/api src/web/app/login src/web/components/auth src/web/tests/unit/LoginForm.test.tsx
git commit -m "feat: BFF auth — login/logout route handlers, middleware, login page"
```

---

## Task 5: App layout + root redirect

**Files:**
- Create: `app/(app)/layout.tsx`, `app/(app)/page.tsx`, `components/layout/AppLayout.tsx`

The `(app)` route group wraps all authenticated pages with a shared AppLayout shell. The layout fetches categories + all cheatsheet summaries server-side and passes them down as props.

- [ ] **Step 1: Create `components/layout/AppLayout.tsx`**

This is the shell — it just provides slot structure. Sidebar is wired in Task 6.

```tsx
import type { Category, CheatsheetSummary } from '@/lib/types';

interface AppLayoutProps {
  categories: Category[];
  sheets: CheatsheetSummary[];
  children: React.ReactNode;
}

export function AppLayout({ categories, sheets, children }: AppLayoutProps) {
  return (
    <div className="flex h-screen overflow-hidden">
      {/* Sidebar slot — filled in Task 6 */}
      <aside
        id="sidebar-slot"
        className="hidden lg:flex lg:flex-col lg:w-72 lg:shrink-0 glass border-r border-white/10"
        data-categories={JSON.stringify(categories)}
        data-sheets={JSON.stringify(sheets)}
      />
      <main className="flex-1 overflow-y-auto p-6 lg:p-8">
        {children}
      </main>
    </div>
  );
}
```

Note: `data-*` attrs are a temporary placeholder. Sidebar becomes a proper client component receiving the props in Task 6 — we'll update AppLayout then.

- [ ] **Step 2: Create `app/(app)/layout.tsx`**

```tsx
import { redirect } from 'next/navigation';
import { getToken } from '@/lib/auth';
import { getCategories } from '@/lib/api/categories';
import { getCheatsheets } from '@/lib/api/cheatsheets';
import { AppLayout } from '@/components/layout/AppLayout';

export default async function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  const token = await getToken();
  if (!token) redirect('/login');

  const [categories, sheets] = await Promise.all([
    getCategories(),
    getCheatsheets(),
  ]);

  return (
    <AppLayout categories={categories} sheets={sheets}>
      {children}
    </AppLayout>
  );
}
```

- [ ] **Step 3: Create `app/(app)/page.tsx`**

```tsx
import { redirect } from 'next/navigation';
import { getCheatsheets } from '@/lib/api/cheatsheets';

export default async function HomePage() {
  const sheets = await getCheatsheets();
  if (sheets.length === 0) {
    redirect('/sheets/new');
  }
  // Sheets are returned sorted by updatedAt desc — first is most recent
  const latest = sheets[0];
  redirect(`/sheets/${latest.categorySlug}/${latest.slug}`);
}
```

- [ ] **Step 4: Verify TypeScript**

```powershell
npx tsc --noEmit
```

- [ ] **Step 5: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/app/"(app)" src/web/components/layout/AppLayout.tsx
git commit -m "feat: app route group layout — server-side data fetch, root redirect to most-recent sheet"
```

---

## Task 6: Sidebar (desktop + mobile drawer)

**Files:**
- Create: `components/layout/Sidebar.tsx`, `components/layout/SidebarDrawer.tsx`, `components/layout/SidebarCategory.tsx`, `components/layout/AppHeader.tsx`
- Modify: `components/layout/AppLayout.tsx`

- [ ] **Step 1: Write failing test for SidebarCategory**

Create `tests/unit/SidebarCategory.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SidebarCategory } from '@/components/layout/SidebarCategory';
import type { Category, CheatsheetSummary } from '@/lib/types';

vi.mock('next/navigation', () => ({
  usePathname: vi.fn(() => '/sheets/git/undo-last-commit'),
  useRouter: vi.fn(() => ({ push: vi.fn() })),
}));

const category: Category = { id: 1, name: 'Git', slug: 'git', icon: null, sortOrder: 0 };
const sheets: CheatsheetSummary[] = [
  { id: 1, title: 'Undo last commit', slug: 'undo-last-commit', categorySlug: 'git', contentType: 'markdown', updatedAt: '', tags: [] },
  { id: 2, title: 'Stash changes', slug: 'stash-changes', categorySlug: 'git', contentType: 'markdown', updatedAt: '', tags: [] },
];

describe('SidebarCategory', () => {
  it('renders the category name', () => {
    render(<SidebarCategory category={category} sheets={sheets} />);
    expect(screen.getByText('Git')).toBeInTheDocument();
  });

  it('renders sheet links when expanded', async () => {
    render(<SidebarCategory category={category} sheets={sheets} defaultOpen />);
    expect(screen.getByText('Undo last commit')).toBeInTheDocument();
    expect(screen.getByText('Stash changes')).toBeInTheDocument();
  });

  it('toggles open/closed on click', async () => {
    render(<SidebarCategory category={category} sheets={sheets} />);
    const header = screen.getByRole('button', { name: /git/i });
    await userEvent.click(header);
    expect(screen.getByText('Undo last commit')).toBeInTheDocument();
    await userEvent.click(header);
    expect(screen.queryByText('Undo last commit')).not.toBeInTheDocument();
  });

  it('highlights the active sheet', () => {
    render(<SidebarCategory category={category} sheets={sheets} defaultOpen />);
    const activeLink = screen.getByRole('link', { name: 'Undo last commit' });
    expect(activeLink).toHaveClass('bg-accent');
  });
});
```

- [ ] **Step 2: Run test — expect FAIL**

```powershell
npm test -- SidebarCategory
```

- [ ] **Step 3: Create `components/layout/SidebarCategory.tsx`**

```tsx
'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronDown, ChevronRight, FileText } from 'lucide-react';
import type { Category, CheatsheetSummary } from '@/lib/types';
import { cn } from '@/lib/utils';

interface SidebarCategoryProps {
  category: Category;
  sheets: CheatsheetSummary[];
  defaultOpen?: boolean;
}

export function SidebarCategory({ category, sheets, defaultOpen = false }: SidebarCategoryProps) {
  const [open, setOpen] = useState(defaultOpen);
  const pathname = usePathname();

  return (
    <div>
      <button
        aria-label={category.name}
        onClick={() => setOpen((o) => !o)}
        className="flex items-center gap-2 w-full px-3 py-2 rounded-lg text-sm font-medium hover:bg-white/5 transition-colors"
      >
        {open ? <ChevronDown className="h-4 w-4 shrink-0" /> : <ChevronRight className="h-4 w-4 shrink-0" />}
        <span className="truncate flex-1 text-left">{category.icon} {category.name}</span>
        <span className="text-xs text-on-surface-muted">{sheets.length}</span>
      </button>
      {open && (
        <ul className="ml-4 space-y-0.5 mt-0.5">
          {sheets.map((sheet) => {
            const href = `/sheets/${sheet.categorySlug}/${sheet.slug}`;
            const isActive = pathname === href;
            return (
              <li key={sheet.id}>
                <Link
                  href={href}
                  className={cn(
                    'flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm transition-colors truncate',
                    isActive
                      ? 'bg-accent text-accent-foreground font-medium'
                      : 'hover:bg-white/5 text-on-surface-muted hover:text-on-surface',
                  )}
                >
                  <FileText className="h-3 w-3 shrink-0" />
                  <span className="truncate">{sheet.title}</span>
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
```

- [ ] **Step 4: Run test — expect PASS**

```powershell
npm test -- SidebarCategory
```

- [ ] **Step 5: Create `components/layout/Sidebar.tsx`**

```tsx
'use client';

import Link from 'next/link';
import { Plus, Upload, LogOut } from 'lucide-react';
import { useRouter } from 'next/navigation';
import type { Category, CheatsheetSummary } from '@/lib/types';
import { SidebarCategory } from './SidebarCategory';
import { ThemeToggle } from '@/components/theme/ThemeToggle';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';

interface SidebarProps {
  categories: Category[];
  sheets: CheatsheetSummary[];
}

export function Sidebar({ categories, sheets }: SidebarProps) {
  const router = useRouter();

  async function handleLogout() {
    await fetch('/api/auth/logout', { method: 'POST' });
    router.push('/login');
    router.refresh();
  }

  const sheetsByCategory = (catSlug: string) =>
    sheets.filter((s) => s.categorySlug === catSlug);

  return (
    <div className="flex flex-col h-full px-3 py-4 gap-2">
      <div className="flex items-center justify-between px-2 mb-2">
        <span className="font-semibold text-sm tracking-wide">Cheatsheets</span>
        <ThemeToggle />
      </div>

      <div className="flex gap-1">
        <Button asChild size="sm" className="flex-1 gap-1 h-8" variant="outline">
          <Link href="/sheets/new">
            <Plus className="h-3.5 w-3.5" />
            New
          </Link>
        </Button>
        <ImportButton />
      </div>

      <Separator className="my-1 opacity-20" />

      <nav className="flex-1 overflow-y-auto space-y-1">
        {categories.map((cat) => (
          <SidebarCategory
            key={cat.id}
            category={cat}
            sheets={sheetsByCategory(cat.slug)}
            defaultOpen
          />
        ))}
        {categories.length === 0 && (
          <p className="text-xs text-center text-on-surface-muted py-4">
            No categories yet. Create your first sheet to get started.
          </p>
        )}
      </nav>

      <Separator className="my-1 opacity-20" />
      <Button
        variant="ghost"
        size="sm"
        className="w-full justify-start gap-2 text-on-surface-muted"
        onClick={handleLogout}
      >
        <LogOut className="h-4 w-4" />
        Sign out
      </Button>
    </div>
  );
}

function ImportButton() {
  function handleClick() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.md,.markdown,.html,.htm';
    input.onchange = async (e) => {
      const file = (e.target as HTMLInputElement).files?.[0];
      if (!file) return;
      const fd = new FormData();
      fd.append('file', file);
      const res = await fetch('/api/import', { method: 'POST', body: fd });
      if (res.ok) {
        const data = await res.json();
        window.location.href = `/sheets/${data.categorySlug}/${data.slug}`;
      }
    };
    input.click();
  }

  return (
    <Button variant="outline" size="sm" className="h-8 px-2" onClick={handleClick} title="Import .md or .html">
      <Upload className="h-3.5 w-3.5" />
    </Button>
  );
}
```

- [ ] **Step 6: Create `components/layout/AppHeader.tsx`**

```tsx
'use client';

import { useState } from 'react';
import { Menu, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { SidebarDrawer } from './SidebarDrawer';
import type { Category, CheatsheetSummary } from '@/lib/types';

interface AppHeaderProps {
  categories: Category[];
  sheets: CheatsheetSummary[];
  onSearchOpen: () => void;
}

export function AppHeader({ categories, sheets, onSearchOpen }: AppHeaderProps) {
  const [drawerOpen, setDrawerOpen] = useState(false);
  return (
    <>
      <header className="lg:hidden flex items-center justify-between px-4 py-3 glass border-b border-white/10 sticky top-0 z-20">
        <Button variant="ghost" size="icon" onClick={() => setDrawerOpen(true)}>
          <Menu className="h-5 w-5" />
        </Button>
        <span className="font-semibold text-sm">Cheatsheets</span>
        <Button variant="ghost" size="icon" onClick={onSearchOpen}>
          <Search className="h-5 w-5" />
        </Button>
      </header>
      <SidebarDrawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        categories={categories}
        sheets={sheets}
      />
    </>
  );
}
```

- [ ] **Step 7: Create `components/layout/SidebarDrawer.tsx`**

```tsx
'use client';

import { Sheet, SheetContent } from '@/components/ui/sheet';
import { Sidebar } from './Sidebar';
import type { Category, CheatsheetSummary } from '@/lib/types';

interface SidebarDrawerProps {
  open: boolean;
  onClose: () => void;
  categories: Category[];
  sheets: CheatsheetSummary[];
}

export function SidebarDrawer({ open, onClose, categories, sheets }: SidebarDrawerProps) {
  return (
    <Sheet open={open} onOpenChange={(o) => !o && onClose()}>
      <SheetContent side="left" className="p-0 w-72 glass">
        <Sidebar categories={categories} sheets={sheets} />
      </SheetContent>
    </Sheet>
  );
}
```

- [ ] **Step 8: Update `components/layout/AppLayout.tsx`**

Replace the entire file:

```tsx
'use client';

import { useState } from 'react';
import type { Category, CheatsheetSummary } from '@/lib/types';
import { Sidebar } from './Sidebar';
import { AppHeader } from './AppHeader';
import { CommandPalette } from '@/components/search/CommandPalette';

interface AppLayoutProps {
  categories: Category[];
  sheets: CheatsheetSummary[];
  children: React.ReactNode;
}

export function AppLayout({ categories, sheets, children }: AppLayoutProps) {
  const [paletteOpen, setPaletteOpen] = useState(false);

  return (
    <div className="flex flex-col lg:flex-row h-screen overflow-hidden">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex lg:flex-col lg:w-72 lg:shrink-0 border-r border-white/10">
        <Sidebar categories={categories} sheets={sheets} />
      </aside>

      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Mobile header */}
        <AppHeader
          categories={categories}
          sheets={sheets}
          onSearchOpen={() => setPaletteOpen(true)}
        />
        <main className="flex-1 overflow-y-auto p-4 lg:p-8">
          {children}
        </main>
      </div>

      <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} />
    </div>
  );
}
```

Note: `AppLayout` is now a client component because it manages drawer/palette open state. The RSC layout at `app/(app)/layout.tsx` passes data as props; `AppLayout` is the client shell. This is the standard RSC + client boundary pattern.

- [ ] **Step 9: Run all tests — expect PASS**

```powershell
npm test
```

- [ ] **Step 10: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/components/layout
git commit -m "feat: sidebar layout — desktop persistent sidebar, mobile drawer, app shell"
```

---

## Task 7: Sheet reading view

**Files:**
- Create: `components/cheatsheet/MarkdownRenderer.tsx`, `components/cheatsheet/RenderedMarkdown.tsx`, `components/cheatsheet/HtmlRenderer.tsx`, `components/cheatsheet/SheetActions.tsx`, `components/cheatsheet/DeleteConfirmDialog.tsx`, `app/(app)/sheets/[category]/[slug]/page.tsx`

- [ ] **Step 1: Install missing remark plugin**

```powershell
npm install rehype-raw
```

- [ ] **Step 2: Create `components/cheatsheet/MarkdownRenderer.tsx`** (RSC)

```tsx
import { unified } from 'unified';
import remarkParse from 'remark-parse';
import remarkGfm from 'remark-gfm';
import remarkRehype from 'remark-rehype';
import rehypeRaw from 'rehype-raw';
import rehypeShiki from '@shikijs/rehype';
import rehypeStringify from 'rehype-stringify';

export async function MarkdownRenderer({ content }: { content: string }) {
  const file = await unified()
    .use(remarkParse)
    .use(remarkGfm)
    .use(remarkRehype, { allowDangerousHtml: true })
    .use(rehypeRaw)
    .use(rehypeShiki, {
      themes: { light: 'github-light', dark: 'catppuccin-mocha' },
    })
    .use(rehypeStringify)
    .process(content);

  return <RenderedMarkdownLoader html={String(file)} />;
}

// Lazy import to keep RSC boundary clean
import { RenderedMarkdown } from './RenderedMarkdown';
function RenderedMarkdownLoader({ html }: { html: string }) {
  return <RenderedMarkdown html={html} />;
}
```

- [ ] **Step 3: Create `components/cheatsheet/RenderedMarkdown.tsx`** (client island)

```tsx
'use client';

import { useEffect, useRef } from 'react';
import { toast } from 'sonner';

export function RenderedMarkdown({ html }: { html: string }) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const container = ref.current;
    if (!container) return;
    container.querySelectorAll('pre').forEach((pre) => {
      if (pre.querySelector('.copy-btn')) return;
      const code = pre.querySelector('code')?.innerText ?? '';
      const btn = document.createElement('button');
      btn.className = 'copy-btn absolute top-2 right-2 px-2 py-1 text-xs rounded-md glass opacity-0 group-hover:opacity-100 transition-opacity';
      btn.textContent = 'Copy';
      btn.onclick = () => {
        navigator.clipboard.writeText(code);
        btn.textContent = 'Copied!';
        toast.success('Copied to clipboard');
        setTimeout(() => (btn.textContent = 'Copy'), 2000);
      };
      pre.classList.add('group', 'relative');
      pre.appendChild(btn);
    });
  }, [html]);

  return (
    <div
      ref={ref}
      className="prose max-w-none"
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}
```

- [ ] **Step 4: Create `components/cheatsheet/HtmlRenderer.tsx`**

```tsx
'use client';

export function HtmlRenderer({ content }: { content: string }) {
  return (
    <iframe
      srcDoc={content}
      sandbox="allow-same-origin"
      className="w-full min-h-[600px] border-0 rounded-xl"
      title="HTML cheatsheet"
      onLoad={(e) => {
        // Auto-resize to content height
        const iframe = e.currentTarget;
        const body = iframe.contentDocument?.body;
        if (body) {
          iframe.style.height = `${body.scrollHeight + 32}px`;
        }
      }}
    />
  );
}
```

- [ ] **Step 5: Create `components/cheatsheet/DeleteConfirmDialog.tsx`**

```tsx
'use client';

import { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { Button } from '@/components/ui/button';
import { Trash2 } from 'lucide-react';
import { deleteSheetAction } from '@/lib/api/actions';
import { toast } from 'sonner';

export function DeleteConfirmDialog({ sheetId }: { sheetId: number }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();

  function handleDelete() {
    startTransition(async () => {
      const res = await deleteSheetAction(sheetId);
      if (res.success) {
        toast.success('Cheatsheet deleted');
        router.push('/');
        router.refresh();
      } else {
        toast.error(res.error ?? 'Failed to delete');
      }
    });
  }

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant="destructive" size="sm" className="gap-1">
          <Trash2 className="h-4 w-4" />
          Delete
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete cheatsheet?</AlertDialogTitle>
          <AlertDialogDescription>
            This is a hard delete — the cheatsheet cannot be recovered.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction onClick={handleDelete} disabled={pending}>
            {pending ? 'Deleting…' : 'Delete'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
```

- [ ] **Step 6: Create `components/cheatsheet/SheetActions.tsx`**

```tsx
import Link from 'next/link';
import { Pencil, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { DeleteConfirmDialog } from './DeleteConfirmDialog';
import type { CheatsheetDetail } from '@/lib/types';

interface SheetActionsProps {
  sheet: CheatsheetDetail;
}

export function SheetActions({ sheet }: SheetActionsProps) {
  return (
    <div className="flex items-center gap-2">
      <Button asChild size="sm" variant="outline" className="gap-1">
        <Link href={`/sheets/${sheet.categorySlug}/${sheet.slug}/edit`}>
          <Pencil className="h-4 w-4" />
          Edit
        </Link>
      </Button>
      <ExportButton sheetId={sheet.id} />
      <DeleteConfirmDialog sheetId={sheet.id} />
    </div>
  );
}

function ExportButton({ sheetId }: { sheetId: number }) {
  return (
    <Button asChild size="sm" variant="outline" className="gap-1">
      <a href={`/api/export/${sheetId}`} download>
        <Download className="h-4 w-4" />
        Export
      </a>
    </Button>
  );
}
```

- [ ] **Step 7: Create `app/api/export/[id]/route.ts`**

```ts
import { NextRequest, NextResponse } from 'next/server';
import { getToken } from '@/lib/auth';

const API_URL =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.API_URL ??
  'http://localhost:5000';

export async function GET(_req: NextRequest, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const token = await getToken();
  const res = await fetch(`${API_URL}/cheatsheets/${id}/export`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  if (!res.ok) {
    return NextResponse.json({ error: 'Export failed' }, { status: res.status });
  }
  const blob = await res.blob();
  const disposition = res.headers.get('content-disposition') ?? 'attachment; filename="export"';
  return new NextResponse(blob, {
    headers: {
      'Content-Type': res.headers.get('content-type') ?? 'application/octet-stream',
      'Content-Disposition': disposition,
    },
  });
}
```

- [ ] **Step 8: Create `app/(app)/sheets/[category]/[slug]/page.tsx`**

```tsx
import { notFound } from 'next/navigation';
import { getCheatsheet } from '@/lib/api/cheatsheets';
import { MarkdownRenderer } from '@/components/cheatsheet/MarkdownRenderer';
import { HtmlRenderer } from '@/components/cheatsheet/HtmlRenderer';
import { SheetActions } from '@/components/cheatsheet/SheetActions';
import { Badge } from '@/components/ui/badge';

interface Props {
  params: Promise<{ category: string; slug: string }>;
}

export default async function SheetPage({ params }: Props) {
  const { category, slug } = await params;
  const sheet = await getCheatsheet(category, slug);
  if (!sheet) notFound();

  return (
    <article className="max-w-4xl mx-auto space-y-6">
      <header className="space-y-3">
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <h1 className="text-3xl font-bold">{sheet.title}</h1>
          <SheetActions sheet={sheet} />
        </div>
        {sheet.tags.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {sheet.tags.map((tag) => (
              <Badge key={tag} variant="secondary" className="glass">
                {tag}
              </Badge>
            ))}
          </div>
        )}
        <p className="text-xs text-on-surface-muted">
          Updated {new Date(sheet.updatedAt).toLocaleDateString()}
        </p>
      </header>

      <div className="glass p-6 lg:p-8 rounded-xl">
        {sheet.contentType === 'markdown' ? (
          <MarkdownRenderer content={sheet.content} />
        ) : (
          <HtmlRenderer content={sheet.content} />
        )}
      </div>
    </article>
  );
}
```

- [ ] **Step 9: Verify TypeScript**

```powershell
npx tsc --noEmit
```

- [ ] **Step 10: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/components/cheatsheet src/web/app/"(app)"/sheets/"[category]" src/web/app/api/export
git commit -m "feat: sheet reading view — markdown RSC + Shiki, sandboxed HTML iframe, export"
```

---

## Task 8: Server Actions + Sheet creation form

**Files:**
- Create: `lib/api/actions.ts`, `components/editor/TagInput.tsx`, `components/editor/MarkdownEditor.tsx`, `components/editor/SheetForm.tsx`, `app/(app)/sheets/new/page.tsx`

- [ ] **Step 1: Write failing test for TagInput**

Create `tests/unit/TagInput.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TagInput } from '@/components/editor/TagInput';

describe('TagInput', () => {
  it('renders existing tags as chips', () => {
    render(<TagInput value={['react', 'typescript']} onChange={vi.fn()} />);
    expect(screen.getByText('react')).toBeInTheDocument();
    expect(screen.getByText('typescript')).toBeInTheDocument();
  });

  it('adds a tag on Enter', async () => {
    const onChange = vi.fn();
    render(<TagInput value={[]} onChange={onChange} />);
    const input = screen.getByPlaceholderText(/add tag/i);
    await userEvent.type(input, 'nextjs{Enter}');
    expect(onChange).toHaveBeenCalledWith(['nextjs']);
  });

  it('removes a tag on × click', async () => {
    const onChange = vi.fn();
    render(<TagInput value={['react']} onChange={onChange} />);
    await userEvent.click(screen.getByRole('button', { name: /remove react/i }));
    expect(onChange).toHaveBeenCalledWith([]);
  });

  it('does not add duplicate tags', async () => {
    const onChange = vi.fn();
    render(<TagInput value={['react']} onChange={onChange} />);
    await userEvent.type(screen.getByPlaceholderText(/add tag/i), 'react{Enter}');
    expect(onChange).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run test — expect FAIL**

```powershell
npm test -- TagInput
```

- [ ] **Step 3: Create `components/editor/TagInput.tsx`**

```tsx
'use client';

import { useState } from 'react';
import { X } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';

interface TagInputProps {
  value: string[];
  onChange: (tags: string[]) => void;
}

export function TagInput({ value, onChange }: TagInputProps) {
  const [draft, setDraft] = useState('');

  function addTag() {
    const tag = draft.trim().toLowerCase();
    if (!tag || value.includes(tag)) return;
    onChange([...value, tag]);
    setDraft('');
  }

  function removeTag(tag: string) {
    onChange(value.filter((t) => t !== tag));
  }

  return (
    <div className="flex flex-wrap gap-2 p-2 glass rounded-lg min-h-[2.5rem]">
      {value.map((tag) => (
        <Badge key={tag} variant="secondary" className="gap-1 pr-1">
          {tag}
          <button
            type="button"
            aria-label={`Remove ${tag}`}
            onClick={() => removeTag(tag)}
            className="hover:text-destructive transition-colors"
          >
            <X className="h-3 w-3" />
          </button>
        </Badge>
      ))}
      <Input
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') { e.preventDefault(); addTag(); }
          if (e.key === 'Backspace' && !draft && value.length > 0) {
            onChange(value.slice(0, -1));
          }
        }}
        placeholder="Add tag…"
        className="border-0 bg-transparent h-7 px-1 w-24 flex-1 min-w-[6rem] focus-visible:ring-0 focus-visible:ring-offset-0"
      />
    </div>
  );
}
```

- [ ] **Step 4: Run test — expect PASS**

```powershell
npm test -- TagInput
```

- [ ] **Step 5: Create `lib/api/actions.ts`**

```ts
'use server';

import { authFetch } from './client';
import type {
  ApiResponse,
  CheatsheetDetail,
  CreateCheatsheetRequest,
  UpdateCheatsheetRequest,
  SaveCategoryRequest,
  Category,
} from '@/lib/types';
import { revalidatePath } from 'next/cache';

export async function createSheetAction(data: CreateCheatsheetRequest): Promise<ApiResponse<CheatsheetDetail>> {
  const res = await authFetch<CheatsheetDetail>('/cheatsheets', {
    method: 'POST',
    body: JSON.stringify(data),
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function updateSheetAction(id: number, data: UpdateCheatsheetRequest): Promise<ApiResponse<CheatsheetDetail>> {
  const res = await authFetch<CheatsheetDetail>(`/cheatsheets/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function deleteSheetAction(id: number): Promise<ApiResponse<null>> {
  const res = await authFetch<null>(`/cheatsheets/${id}`, { method: 'DELETE' });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function createCategoryAction(data: SaveCategoryRequest): Promise<ApiResponse<Category>> {
  const res = await authFetch<Category>('/categories', {
    method: 'POST',
    body: JSON.stringify(data),
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function updateCategoryAction(id: number, data: SaveCategoryRequest): Promise<ApiResponse<Category>> {
  const res = await authFetch<Category>(`/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}

export async function deleteCategoryAction(id: number): Promise<ApiResponse<null>> {
  const res = await authFetch<null>(`/categories/${id}`, { method: 'DELETE' });
  if (res.success) revalidatePath('/', 'layout');
  return res;
}
```

- [ ] **Step 6: Create `components/editor/MarkdownEditor.tsx`**

```tsx
'use client';

import { useEffect, useRef } from 'react';
import { Crepe } from '@milkdown/crepe';
import '@milkdown/crepe/theme/common/style.css';

interface MarkdownEditorProps {
  value: string;
  onChange: (value: string) => void;
}

export function MarkdownEditor({ value, onChange }: MarkdownEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const crepeRef = useRef<Crepe | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    const crepe = new Crepe({
      root: containerRef.current,
      defaultValue: value,
    });
    crepe.create().then(() => {
      crepeRef.current = crepe;
    });
    return () => {
      crepe.destroy();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Sync content out on blur (Milkdown is uncontrolled)
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;
    function handleInput() {
      if (crepeRef.current) {
        onChange(crepeRef.current.getMarkdown());
      }
    }
    container.addEventListener('input', handleInput);
    return () => container.removeEventListener('input', handleInput);
  }, [onChange]);

  return (
    <div
      ref={containerRef}
      className="min-h-[400px] glass rounded-xl p-1 focus-within:ring-1 focus-within:ring-accent-primary/50"
    />
  );
}
```

- [ ] **Step 7: Create `components/editor/SheetForm.tsx`**

```tsx
'use client';

import { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { TagInput } from './TagInput';
import { MarkdownEditor } from './MarkdownEditor';
import type { Category, CheatsheetDetail } from '@/lib/types';
import { createSheetAction, updateSheetAction } from '@/lib/api/actions';

interface SheetFormProps {
  categories: Category[];
  sheet?: CheatsheetDetail;        // present in edit mode
}

export function SheetForm({ categories, sheet }: SheetFormProps) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [title, setTitle] = useState(sheet?.title ?? '');
  const [categoryId, setCategoryId] = useState<number>(sheet?.categoryId ?? categories[0]?.id ?? 0);
  const [tags, setTags] = useState<string[]>(sheet?.tags ?? []);
  const [content, setContent] = useState(sheet?.content ?? '');
  const [htmlFile, setHtmlFile] = useState<File | null>(null);
  const isHtmlSheet = sheet?.contentType === 'html';

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    startTransition(async () => {
      let finalContent = content;
      if (isHtmlSheet && htmlFile) {
        finalContent = await htmlFile.text();
      }

      const payload = { title, categoryId, tags, content: finalContent };
      const res = sheet
        ? await updateSheetAction(sheet.id, payload)
        : await createSheetAction({ ...payload, contentType: 'markdown' });

      if (res.success && res.data) {
        toast.success(sheet ? 'Saved' : 'Created');
        router.push(`/sheets/${res.data.categorySlug}/${res.data.slug}`);
        router.refresh();
      } else {
        toast.error(res.error ?? 'Something went wrong');
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="max-w-4xl mx-auto space-y-6">
      <div className="space-y-1">
        <Label htmlFor="title">Title</Label>
        <Input
          id="title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Sheet title"
          required
          className="text-lg"
        />
      </div>

      <div className="space-y-1">
        <Label htmlFor="category">Category</Label>
        {categories.length > 0 ? (
          <select
            id="category"
            value={categoryId}
            onChange={(e) => setCategoryId(Number(e.target.value))}
            className="w-full glass rounded-lg px-3 py-2 text-sm"
          >
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        ) : (
          <p className="text-sm text-on-surface-muted">
            No categories yet — the API will create "Uncategorized" automatically if needed. Add a category from the sidebar after saving.
          </p>
        )}
      </div>

      <div className="space-y-1">
        <Label>Tags</Label>
        <TagInput value={tags} onChange={setTags} />
      </div>

      {isHtmlSheet ? (
        <div className="space-y-1">
          <Label htmlFor="replace-file">Replace HTML file</Label>
          <Input
            id="replace-file"
            type="file"
            accept=".html,.htm"
            onChange={(e) => setHtmlFile(e.target.files?.[0] ?? null)}
          />
          <p className="text-xs text-on-surface-muted">Leave empty to keep current content.</p>
        </div>
      ) : (
        <div className="space-y-1">
          <Label>Content</Label>
          <MarkdownEditor value={content} onChange={setContent} />
        </div>
      )}

      <div className="flex gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? 'Saving…' : sheet ? 'Save changes' : 'Create sheet'}
        </Button>
        <Button type="button" variant="ghost" onClick={() => router.back()}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
```

- [ ] **Step 8: Create `app/(app)/sheets/new/page.tsx`**

```tsx
import { getCategories } from '@/lib/api/categories';
import { SheetForm } from '@/components/editor/SheetForm';

export default async function NewSheetPage() {
  const categories = await getCategories();
  return (
    <div className="py-4">
      <h1 className="text-2xl font-bold mb-8">New cheatsheet</h1>
      <SheetForm categories={categories} />
    </div>
  );
}
```

- [ ] **Step 9: Run all tests**

```powershell
npm test
```

Expected: TagInput 4 tests passing; all others still green.

- [ ] **Step 10: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/lib/api/actions.ts src/web/components/editor src/web/app/"(app)"/sheets/new src/web/tests/unit/TagInput.test.tsx
git commit -m "feat: sheet creation — Server Actions, TagInput, Milkdown editor, SheetForm, /sheets/new"
```

---

## Task 9: Sheet edit + delete

**Files:**
- Create: `app/(app)/sheets/[category]/[slug]/edit/page.tsx`

- [ ] **Step 1: Create `app/(app)/sheets/[category]/[slug]/edit/page.tsx`**

```tsx
import { notFound } from 'next/navigation';
import { getCheatsheet } from '@/lib/api/cheatsheets';
import { getCategories } from '@/lib/api/categories';
import { SheetForm } from '@/components/editor/SheetForm';

interface Props {
  params: Promise<{ category: string; slug: string }>;
}

export default async function EditSheetPage({ params }: Props) {
  const { category, slug } = await params;
  const [sheet, categories] = await Promise.all([
    getCheatsheet(category, slug),
    getCategories(),
  ]);
  if (!sheet) notFound();

  return (
    <div className="py-4">
      <h1 className="text-2xl font-bold mb-8">Edit — {sheet.title}</h1>
      <SheetForm categories={categories} sheet={sheet} />
    </div>
  );
}
```

- [ ] **Step 2: Verify TypeScript**

```powershell
npx tsc --noEmit
```

- [ ] **Step 3: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/app/"(app)"/sheets/"[category]"/"[slug]"/edit
git commit -m "feat: sheet edit page — pre-fills SheetForm, handles HTML replace-file upload"
```

---

## Task 10: Command palette (Ctrl+K search)

**Files:**
- Create: `components/search/CommandPalette.tsx`

- [ ] **Step 1: Write failing test for CommandPalette**

Create `tests/unit/CommandPalette.test.tsx`:

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CommandPalette } from '@/components/search/CommandPalette';

vi.mock('next/navigation', () => ({
  useRouter: vi.fn(() => ({ push: vi.fn() })),
}));

global.fetch = vi.fn();

describe('CommandPalette', () => {
  beforeEach(() => vi.clearAllMocks());

  it('is hidden when open=false', () => {
    render(<CommandPalette open={false} onOpenChange={vi.fn()} />);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('shows the dialog when open=true', () => {
    render(<CommandPalette open={true} onOpenChange={vi.fn()} />);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('searches and displays results', async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        success: true,
        data: [
          { id: 1, title: 'Undo last commit', slug: 'undo', categorySlug: 'git', contentType: 'markdown', tags: [], updatedAt: '' },
        ],
        error: null,
      }),
    } as Response);

    render(<CommandPalette open={true} onOpenChange={vi.fn()} />);
    const input = screen.getByPlaceholderText(/search/i);
    await userEvent.type(input, 'undo');

    await waitFor(() => {
      expect(screen.getByText('Undo last commit')).toBeInTheDocument();
    });
  });
});
```

- [ ] **Step 2: Run test — expect FAIL**

```powershell
npm test -- CommandPalette
```

- [ ] **Step 3: Create `components/search/CommandPalette.tsx`**

```tsx
'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';
import { FileText } from 'lucide-react';
import type { CheatsheetSummary } from '@/lib/types';

interface CommandPaletteProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CommandPalette({ open, onOpenChange }: CommandPaletteProps) {
  const router = useRouter();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<CheatsheetSummary[]>([]);
  const [loading, setLoading] = useState(false);

  // Global Ctrl+K shortcut
  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'k' && (e.ctrlKey || e.metaKey)) {
        e.preventDefault();
        onOpenChange(!open);
      }
    }
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [open, onOpenChange]);

  const search = useCallback(async (q: string) => {
    setLoading(true);
    const qs = q.trim() ? `?q=${encodeURIComponent(q)}` : '';
    const res = await fetch(`/api/search${qs}`);
    const body = await res.json();
    setResults(body.data ?? []);
    setLoading(false);
  }, []);

  useEffect(() => {
    if (!open) { setQuery(''); setResults([]); return; }
    const t = setTimeout(() => search(query), 200);
    return () => clearTimeout(t);
  }, [query, open, search]);

  function navigate(sheet: CheatsheetSummary) {
    onOpenChange(false);
    router.push(`/sheets/${sheet.categorySlug}/${sheet.slug}`);
  }

  return (
    <CommandDialog open={open} onOpenChange={onOpenChange}>
      <CommandInput
        placeholder="Search cheatsheets…"
        value={query}
        onValueChange={setQuery}
      />
      <CommandList>
        {loading && <CommandEmpty>Searching…</CommandEmpty>}
        {!loading && results.length === 0 && query && (
          <CommandEmpty>No results for "{query}"</CommandEmpty>
        )}
        {results.length > 0 && (
          <CommandGroup heading="Cheatsheets">
            {results.map((sheet) => (
              <CommandItem
                key={sheet.id}
                value={sheet.title}
                onSelect={() => navigate(sheet)}
                className="gap-2"
              >
                <FileText className="h-4 w-4 shrink-0" />
                <span>{sheet.title}</span>
                <span className="ml-auto text-xs text-on-surface-muted">{sheet.categorySlug}</span>
              </CommandItem>
            ))}
          </CommandGroup>
        )}
      </CommandList>
    </CommandDialog>
  );
}
```

- [ ] **Step 4: Create `app/api/search/route.ts`** (proxies to .NET API)

```ts
import { NextRequest, NextResponse } from 'next/server';
import { getToken } from '@/lib/auth';

const API_URL =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.API_URL ??
  'http://localhost:5000';

export async function GET(request: NextRequest) {
  const q = request.nextUrl.searchParams.get('q') ?? '';
  const token = await getToken();
  const qs = q ? `?q=${encodeURIComponent(q)}` : '';
  const res = await fetch(`${API_URL}/cheatsheets${qs}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    cache: 'no-store',
  });
  const data = await res.json();
  return NextResponse.json(data);
}
```

- [ ] **Step 5: Run test — expect PASS**

```powershell
npm test -- CommandPalette
```

Note: the test mocks `fetch` to `/api/search`, so the mock is on the global `fetch`.

- [ ] **Step 6: Run all tests**

```powershell
npm test
```

- [ ] **Step 7: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/components/search src/web/app/api/search src/web/tests/unit/CommandPalette.test.tsx
git commit -m "feat: Ctrl+K command palette — debounced full-text search, keyboard navigation"
```

---

## Task 11: Import route handler

**Files:**
- Create: `app/api/import/route.ts`

The import button in `Sidebar.tsx` (Task 6) already posts to `/api/import`. This task adds that route.

- [ ] **Step 1: Create `app/api/import/route.ts`**

```ts
import { NextRequest, NextResponse } from 'next/server';
import { getToken } from '@/lib/auth';

const API_URL =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.API_URL ??
  'http://localhost:5000';

export async function POST(request: NextRequest) {
  const token = await getToken();
  if (!token) return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });

  // Forward the multipart form directly to the .NET API
  const formData = await request.formData();
  const res = await fetch(`${API_URL}/cheatsheets/import`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
    body: formData,
  });

  const data = await res.json();
  if (!res.ok || !data.success) {
    return NextResponse.json({ error: data.error ?? 'Import failed' }, { status: res.status });
  }

  // Return categorySlug + slug so Sidebar can redirect
  return NextResponse.json({
    success: true,
    categorySlug: data.data.categorySlug,
    slug: data.data.slug,
  });
}
```

- [ ] **Step 2: Verify TypeScript**

```powershell
npx tsc --noEmit
```

- [ ] **Step 3: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/app/api/import
git commit -m "feat: import route handler — proxy multipart upload to .NET API"
```

---

## Task 12: Aspire integration

**Files:**
- Modify: `src/CheatsheetApp.AppHost/AppHost.cs`
- Modify: `src/CheatsheetApp.AppHost/CheatsheetApp.AppHost.csproj`
- Create: `src/web/next.config.ts`

- [ ] **Step 1: Add Node.js hosting support to AppHost csproj**

Edit `src/CheatsheetApp.AppHost/CheatsheetApp.AppHost.csproj` — add the package:

```powershell
cd D:\Projects\CheatsheetProject
dotnet add src/CheatsheetApp.AppHost package Aspire.Hosting.NodeJs
```

- [ ] **Step 2: Update `src/CheatsheetApp.AppHost/AppHost.cs`**

Replace with:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb();

var db = postgres.AddDatabase("cheatsheets");

var api = builder.AddProject<Projects.CheatsheetApp_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("Admin__Username", "admin")
    .WithEnvironment("Admin__Password", "dev-password-change-me")
    .WithEnvironment("Jwt__Key", "dev-only-jwt-signing-key-0123456789abcdef0123456789abcdef")
    .WithEnvironment("Database__SeedSampleData", "true");

builder.AddNpmApp("web", "../web", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
```

- [ ] **Step 3: Create `src/web/next.config.ts`**

```ts
import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  output: 'standalone',
  // Aspire injects PORT; Next.js reads it automatically via --port
};

export default nextConfig;
```

- [ ] **Step 4: Verify AppHost builds**

```powershell
dotnet build src/CheatsheetApp.AppHost
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```powershell
git add src/CheatsheetApp.AppHost/AppHost.cs src/CheatsheetApp.AppHost/CheatsheetApp.AppHost.csproj src/web/next.config.ts
git commit -m "feat: wire Next.js dev server into Aspire AppHost as AddNpmApp resource"
```

---

## Task 13: Vitest unit test coverage check

**Files:**
- Modify: any test file where coverage is below 80%

- [ ] **Step 1: Run coverage report**

```powershell
cd src/web
npm run test:coverage
```

Review the report. Minimum thresholds: 80% lines, functions, branches (configured in `vitest.config.ts`).

- [ ] **Step 2: Add missing tests if threshold fails**

If any component is below 80%, add a test covering the missing branch. Key candidates:
- `LoginForm.tsx` — loading state during submit
- `SidebarCategory.tsx` — empty sheets list
- `TagInput.tsx` — Backspace removes last tag

Example missing test for `TagInput` Backspace behavior:

```tsx
it('removes last tag on Backspace when input is empty', async () => {
  const onChange = vi.fn();
  render(<TagInput value={['react', 'ts']} onChange={onChange} />);
  const input = screen.getByPlaceholderText(/add tag/i);
  await userEvent.type(input, '{Backspace}');
  expect(onChange).toHaveBeenCalledWith(['react']);
});
```

- [ ] **Step 3: Run coverage again — expect all thresholds pass**

```powershell
npm run test:coverage
```

- [ ] **Step 4: Commit if tests were added**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/tests
git commit -m "test: bring Vitest coverage above 80% threshold"
```

---

## Task 14: Playwright E2E tests

**Files:**
- Create: `tests/e2e/login.spec.ts`, `tests/e2e/crud.spec.ts`, `tests/e2e/search.spec.ts`, `tests/e2e/import-html.spec.ts`

Prerequisite: the full Aspire stack must be running (`dotnet run --project src/CheatsheetApp.AppHost`) before running E2E tests, or use the `webServer` config in `playwright.config.ts` to start `npm run dev` in isolation. For CI, configure `API_URL` pointing to a test API instance.

- [ ] **Step 1: Create `tests/e2e/login.spec.ts`**

```ts
import { test, expect } from '@playwright/test';

test.describe('Login flow', () => {
  test('redirects unauthenticated user to /login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/login/);
  });

  test('shows error for wrong credentials', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill('wrongpassword');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page.getByText(/invalid username or password/i)).toBeVisible();
  });

  test('logs in with correct credentials and redirects', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill(process.env.TEST_ADMIN_PASSWORD ?? 'dev-password-change-me');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).not.toHaveURL(/\/login/);
  });
});
```

- [ ] **Step 2: Create `tests/e2e/crud.spec.ts`**

```ts
import { test, expect } from '@playwright/test';

test.use({ storageState: 'tests/e2e/.auth.json' });

test.beforeAll(async ({ browser }) => {
  // Authenticate once and save state
  const page = await browser.newPage();
  await page.goto('/login');
  await page.getByLabel('Username').fill('admin');
  await page.getByLabel('Password').fill(process.env.TEST_ADMIN_PASSWORD ?? 'dev-password-change-me');
  await page.getByRole('button', { name: /sign in/i }).click();
  await page.waitForURL((url) => !url.pathname.includes('/login'));
  await page.context().storageState({ path: 'tests/e2e/.auth.json' });
  await page.close();
});

test('create → read → edit → delete a markdown sheet', async ({ page }) => {
  // Create
  await page.goto('/sheets/new');
  await page.getByLabel('Title').fill('E2E Test Sheet');
  await page.locator('.milkdown').click();
  await page.keyboard.type('# Hello\n\nThis is a test.');
  await page.getByRole('button', { name: /create sheet/i }).click();
  await expect(page).toHaveURL(/\/sheets\//);
  await expect(page.getByRole('heading', { name: 'E2E Test Sheet' })).toBeVisible();

  // Edit
  await page.getByRole('link', { name: /edit/i }).click();
  await page.getByLabel('Title').fill('E2E Test Sheet (edited)');
  await page.getByRole('button', { name: /save changes/i }).click();
  await expect(page.getByRole('heading', { name: 'E2E Test Sheet (edited)' })).toBeVisible();

  // Delete
  await page.getByRole('button', { name: /delete/i }).click();
  await page.getByRole('button', { name: /^delete$/i }).click(); // confirm
  await expect(page).not.toHaveURL(/E2E/);
});
```

- [ ] **Step 3: Create `tests/e2e/search.spec.ts`**

```ts
import { test, expect } from '@playwright/test';

test.use({ storageState: 'tests/e2e/.auth.json' });

test('Ctrl+K opens command palette and navigates to a result', async ({ page }) => {
  await page.goto('/');
  await page.keyboard.press('Control+k');
  await expect(page.getByRole('dialog')).toBeVisible();

  await page.getByPlaceholder(/search/i).fill('git');
  // Wait for results to appear (debounce + fetch)
  const firstResult = page.getByRole('option').first();
  await expect(firstResult).toBeVisible({ timeout: 3000 });
  await firstResult.click();
  await expect(page).toHaveURL(/\/sheets\//);
  await expect(page.getByRole('dialog')).not.toBeVisible();
});
```

- [ ] **Step 4: Create `tests/e2e/import-html.spec.ts`**

```ts
import { test, expect, type Page } from '@playwright/test';
import path from 'path';
import fs from 'fs';
import os from 'os';

test.use({ storageState: 'tests/e2e/.auth.json' });

async function createTempHtmlFile(): Promise<string> {
  const dir = os.tmpdir();
  const filePath = path.join(dir, 'test-sheet.html');
  fs.writeFileSync(filePath, `
    <html>
      <head><style>body { background: rebeccapurple; color: white; }</style></head>
      <body><h1>HTML Test Sheet</h1><p>Custom styles intact.</p></body>
    </html>
  `);
  return filePath;
}

test('imports an HTML file and renders it in a sandboxed iframe', async ({ page }) => {
  const htmlPath = await createTempHtmlFile();

  await page.goto('/');

  // Trigger the hidden file input via the import button
  const [fileChooser] = await Promise.all([
    page.waitForEvent('filechooser'),
    page.getByTitle(/import/i).click(),
  ]);
  await fileChooser.setFiles(htmlPath);

  // Should redirect to the imported sheet
  await expect(page).toHaveURL(/\/sheets\//, { timeout: 5000 });

  // Iframe should be present
  const iframe = page.frameLocator('iframe');
  await expect(iframe.getByRole('heading', { name: 'HTML Test Sheet' })).toBeVisible();

  fs.unlinkSync(htmlPath);
});
```

- [ ] **Step 5: Run E2E tests (requires full stack running)**

```powershell
# In a separate terminal: dotnet run --project src/CheatsheetApp.AppHost
# Then:
cd src/web
npm run test:e2e
```

Expected: all 4 spec files pass.

- [ ] **Step 6: Commit**

```powershell
cd D:\Projects\CheatsheetProject
git add src/web/tests/e2e
git commit -m "test: Playwright E2E — login, CRUD, Ctrl+K search, HTML import"
```

---

## Self-review

### Spec coverage check

| Spec requirement | Task |
|---|---|
| Create/edit markdown cheatsheets with modern editor | Task 8 (Milkdown Crepe) |
| Import `.md`/`.html` files | Task 11 + Task 6 (sidebar upload button) |
| Upload `.html` and render with own CSS | Task 7 (sandboxed iframe) |
| Full-text search | Task 10 (Command palette → GET /cheatsheets?q=) |
| Syntax-highlighted code blocks + copy button | Task 7 (Shiki + RenderedMarkdown) |
| Dark/light mode, Aurora glassmorphism | Task 2 |
| Token-based theme system (adding themes = zero component changes) | Task 2 (CSS @theme block) |
| Persistent sidebar (desktop), collapse to drawer (mobile) | Task 6 |
| `/login` page | Task 4 |
| `/` → redirect to most recent | Task 5 |
| `/sheets/[category]/[slug]` reading view | Task 7 |
| `/sheets/new`, `/sheets/[category]/[slug]/edit` editor | Tasks 8, 9 |
| Ctrl+K command palette | Task 10 |
| JWT in HttpOnly cookie (browser never sees token) | Tasks 4 (route handler + middleware) |
| BFF — Next.js calls API server-side only | Task 3 (authFetch in RSCs/Server Actions) |
| Export `.md`/`.html` | Task 7 (ExportButton → /api/export/[id]) |
| Delete with confirm dialog | Task 7 (DeleteConfirmDialog) |
| Error boundaries / toast notifications | Sonner Toaster in layout (Task 2), toast.error in actions |
| Responsive — mobile-friendly | Task 6 (AppHeader + SidebarDrawer) |
| Aspire orchestration | Task 12 |
| Vitest unit tests | Tasks 2, 4, 6, 8, 10, 13 |
| Playwright E2E | Task 14 |
| 80% coverage | Task 13 |

All spec requirements are covered.

### Placeholder scan

No "TBD", "TODO", or "implement later" markers found. Every step shows actual code.

### Type consistency

- `CheatsheetSummary.categorySlug` used consistently across lib/types.ts, lib/api/cheatsheets.ts, and all components.
- `deleteSheetAction` defined in `lib/api/actions.ts` Task 8 Step 5; used in `DeleteConfirmDialog` Task 7 Step 5.
- `createSheetAction` / `updateSheetAction` defined in actions.ts; used in `SheetForm`.
- `authFetch` defined in `lib/api/client.ts` Task 3 Step 3; imported by all api modules and actions.ts.
- `getToken()` defined in `lib/auth.ts` Task 3 Step 2; imported by route handlers and actions.

---

**Plan complete and saved to `docs/superpowers/plans/2026-06-10-nextjs-frontend.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** — execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
