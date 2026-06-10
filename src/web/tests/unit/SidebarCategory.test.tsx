import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SidebarCategory } from '@/components/layout/SidebarCategory';
import type { Category, CheatsheetSummary } from '@/lib/types';

vi.mock('next/navigation', () => ({
  usePathname: vi.fn(() => '/sheets/git/undo-last-commit'),
  useRouter: vi.fn(() => ({ push: vi.fn() })),
  Link: vi.fn(({ href, children, className }: { href: string; children: React.ReactNode; className?: string }) => (
    <a href={href} className={className}>{children}</a>
  )),
}));

vi.mock('next/link', () => ({
  default: ({ href, children, className }: { href: string; children: React.ReactNode; className?: string }) => (
    <a href={href} className={className}>{children}</a>
  ),
}));

const category: Category = {
  id: 1,
  name: 'Git',
  slug: 'git',
  icon: null,
  sortOrder: 0,
};

const sheets: CheatsheetSummary[] = [
  {
    id: 1,
    title: 'Undo last commit',
    slug: 'undo-last-commit',
    categorySlug: 'git',
    contentType: 'markdown',
    updatedAt: '',
    tags: [],
  },
  {
    id: 2,
    title: 'Stash changes',
    slug: 'stash-changes',
    categorySlug: 'git',
    contentType: 'markdown',
    updatedAt: '',
    tags: [],
  },
];

describe('SidebarCategory', () => {
  it('renders the category name', () => {
    render(<SidebarCategory category={category} sheets={sheets} />);
    expect(screen.getByText('Git')).toBeInTheDocument();
  });

  it('renders sheet links when defaultOpen=true', () => {
    render(<SidebarCategory category={category} sheets={sheets} defaultOpen />);
    expect(screen.getByText('Undo last commit')).toBeInTheDocument();
    expect(screen.getByText('Stash changes')).toBeInTheDocument();
  });

  it('toggles open/closed on button click', async () => {
    render(<SidebarCategory category={category} sheets={sheets} />);
    const toggleBtn = screen.getByRole('button');
    // Initially closed
    expect(screen.queryByText('Undo last commit')).not.toBeInTheDocument();
    // Open
    await userEvent.click(toggleBtn);
    expect(screen.getByText('Undo last commit')).toBeInTheDocument();
    // Close
    await userEvent.click(toggleBtn);
    expect(screen.queryByText('Undo last commit')).not.toBeInTheDocument();
  });

  it('applies active class to the current sheet link', () => {
    render(<SidebarCategory category={category} sheets={sheets} defaultOpen />);
    const activeLink = screen.getByRole('link', { name: /undo last commit/i });
    expect(activeLink.className).toMatch(/bg-accent/);
  });

  it('renders empty category without crashing', () => {
    render(<SidebarCategory category={category} sheets={[]} defaultOpen />);
    expect(screen.getByText('Git')).toBeInTheDocument();
    expect(screen.queryByRole('link')).not.toBeInTheDocument();
  });
});
