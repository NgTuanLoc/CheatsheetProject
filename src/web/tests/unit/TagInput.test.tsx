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

  it('removes last tag on Backspace when input is empty', async () => {
    const onChange = vi.fn();
    render(<TagInput value={['react', 'ts']} onChange={onChange} />);
    const input = screen.getByPlaceholderText(/add tag/i);
    await userEvent.type(input, '{Backspace}');
    expect(onChange).toHaveBeenCalledWith(['react']);
  });
});
