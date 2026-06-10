import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeToggle } from '@/components/theme/ThemeToggle';

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
