import { test, expect } from '@playwright/test';
import { AUTH_FILE } from './auth.setup';
import path from 'path';
import fs from 'fs';
import os from 'os';

test.use({ storageState: AUTH_FILE });

test('imports an HTML file and renders it in a sandboxed iframe', async ({ page }) => {
  // Create a temp HTML file
  const htmlContent = `
    <html>
      <head><style>body { background: rebeccapurple; color: white; font-family: sans-serif; }</style></head>
      <body><h1>HTML E2E Import Test</h1><p>Custom styles should be intact.</p></body>
    </html>
  `;
  const tmpDir = os.tmpdir();
  const htmlPath = path.join(tmpDir, `e2e-import-${Date.now()}.html`);
  fs.writeFileSync(htmlPath, htmlContent);

  await page.goto('/');

  // Trigger the file input via the Import button in the sidebar
  const [fileChooser] = await Promise.all([
    page.waitForEvent('filechooser', { timeout: 5000 }),
    page.getByTitle(/import/i).click(),
  ]);
  await fileChooser.setFiles(htmlPath);

  // Should redirect to the imported sheet
  await expect(page).toHaveURL(/\/sheets\//, { timeout: 8000 });

  // Iframe should be present and have the expected content
  const iframe = page.frameLocator('iframe');
  await expect(iframe.getByRole('heading', { name: 'HTML E2E Import Test' })).toBeVisible({
    timeout: 5000,
  });

  fs.unlinkSync(htmlPath);
});
