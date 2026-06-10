import { test as setup } from '@playwright/test';
import path from 'path';

export const AUTH_FILE = path.join(__dirname, '.auth.json');

setup('authenticate', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Username').fill('admin');
  await page.getByLabel('Password').fill(
    process.env.TEST_ADMIN_PASSWORD ?? 'dev-password-change-me',
  );
  await page.getByRole('button', { name: /sign in/i }).click();
  await page.waitForURL((url) => !url.pathname.includes('/login'), {
    timeout: 10000,
  });
  await page.context().storageState({ path: AUTH_FILE });
});
