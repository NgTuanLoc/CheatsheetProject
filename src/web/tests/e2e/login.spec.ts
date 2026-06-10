import { test, expect } from '@playwright/test';

test.describe('Login', () => {
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
    await page.getByLabel('Password').fill(
      process.env.TEST_ADMIN_PASSWORD ?? 'dev-password-change-me',
    );
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).not.toHaveURL(/\/login/, { timeout: 5000 });
  });
});
