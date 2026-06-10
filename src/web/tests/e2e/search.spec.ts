import { test, expect } from '@playwright/test';
import { AUTH_FILE } from './auth.setup';

test.use({ storageState: AUTH_FILE });

test('Ctrl+K opens command palette and navigates to a result', async ({ page }) => {
  await page.goto('/');
  // Open palette
  await page.keyboard.press('Control+k');
  await expect(page.getByRole('dialog')).toBeVisible();

  // Type search query
  await page.getByPlaceholder(/search/i).fill('git');

  // Wait for results (debounce + fetch)
  await expect(page.getByRole('option').first()).toBeVisible({ timeout: 3000 });

  // Navigate to first result
  await page.getByRole('option').first().click();

  // Should navigate away from palette to a sheet
  await expect(page.getByRole('dialog')).not.toBeVisible({ timeout: 2000 });
  await expect(page).toHaveURL(/\/sheets\//);
});
