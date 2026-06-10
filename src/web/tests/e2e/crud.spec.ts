import { test, expect } from '@playwright/test';
import { AUTH_FILE } from './auth.setup';

test.use({ storageState: AUTH_FILE });

test('create → read → edit → delete a markdown sheet', async ({ page }) => {
  // Create
  await page.goto('/sheets/new');
  await page.getByLabel('Title').fill('E2E Test Sheet');
  // Type into the Milkdown editor area
  await page.locator('.milkdown').click();
  await page.keyboard.type('# Hello\n\nThis is an E2E test.');
  await page.getByRole('button', { name: /create sheet/i }).click();

  // Read
  await expect(page).toHaveURL(/\/sheets\//, { timeout: 5000 });
  await expect(page.getByRole('heading', { name: 'E2E Test Sheet' })).toBeVisible();

  // Edit
  await page.getByRole('link', { name: /edit/i }).click();
  await page.getByLabel('Title').fill('E2E Test Sheet (edited)');
  await page.getByRole('button', { name: /save changes/i }).click();
  await expect(page.getByRole('heading', { name: 'E2E Test Sheet (edited)' })).toBeVisible({
    timeout: 5000,
  });

  // Delete
  await page.getByRole('button', { name: /delete/i }).click();
  await page.getByRole('button', { name: /^delete$/i }).click(); // confirm dialog
  await expect(page).not.toHaveURL(/e2e-test-sheet/, { timeout: 5000 });
});
