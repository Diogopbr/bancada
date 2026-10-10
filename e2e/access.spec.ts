import { test, expect } from '@playwright/test';

test('an unauthenticated visitor is offered staff login', async ({ page }) => {
  await page.goto('/ordens');
  await expect(page).toHaveURL(/\/entrar/);
  await expect(page.getByLabel(/e-?mail/i)).toBeVisible();
  await expect(page.getByLabel(/senha/i)).toBeVisible();
  await expect(page.getByRole('button', { name: /entrar/i })).toBeVisible();
});

test('expired customer access offers clear recovery without approval controls', async ({ page }) => {
  await page.goto('/acompanhar#token=invalid-example-access');
  await expect(page).not.toHaveURL(/token=/);
  await expect(page.getByText(/acesso.*(indisponível|expirado)|link.*(inválido|expirado)/i).first()).toBeVisible();
  await expect(page.getByRole('button', { name: /^aprovar/i })).toHaveCount(0);
});
