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

  it('searches and displays results after typing', async () => {
    vi.mocked(fetch).mockResolvedValue({
      ok: true,
      json: async () => ({
        success: true,
        data: [
          {
            id: 1,
            title: 'Undo last commit',
            slug: 'undo',
            categorySlug: 'git',
            contentType: 'markdown',
            tags: [],
            updatedAt: '',
          },
        ],
        error: null,
      }),
    } as Response);

    render(<CommandPalette open={true} onOpenChange={vi.fn()} />);
    const input = screen.getByPlaceholderText(/search/i);
    await userEvent.type(input, 'undo');

    await waitFor(
      () => expect(screen.getByText('Undo last commit')).toBeInTheDocument(),
      { timeout: 1000 },
    );
  });
});
