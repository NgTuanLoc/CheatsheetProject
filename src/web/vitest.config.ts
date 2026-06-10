import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    globals: true,
    exclude: ['node_modules', 'tests/e2e/**'],
    coverage: {
      provider: 'v8',
      include: [
        'components/auth/**',
        'components/layout/SidebarCategory.tsx',
        'components/search/**',
        'components/theme/**',
        'components/editor/TagInput.tsx',
      ],
      thresholds: { lines: 80, functions: 80, branches: 80 },
    },
  },
  resolve: {
    alias: { '@': path.resolve(__dirname, '.') },
  },
});
