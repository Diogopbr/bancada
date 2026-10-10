import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('staff signs in and registers a persisted order through the interface', async ({ page }) => {
  const password = process.env['BANCADA_DEMO_PASSWORD'];
  if (!password) throw new Error('BANCADA_DEMO_PASSWORD is required for real UI verification');
  await page.goto('/entrar');
  await page.getByLabel(/email/i).fill('atendente@bancada.example');
  await page.getByLabel(/senha/i).fill(password);
  await page.getByRole('button', { name: /entrar/i }).click();
  await expect(page).toHaveURL(/\/ordens$/);
  await page.getByRole('complementary').getByRole('link', { name: 'Nova ordem', exact: true }).click();
  const equipment = `Notebook fictício UI ${Date.now()}`;
  await page.getByLabel(/nome completo do cliente/i).fill('Cliente fictício do teste');
  await page.getByLabel(/email de contato/i).fill('ui-client@example.test');
  await page.getByLabel(/equipamento \(marca/i).fill(equipment);
  await page.getByLabel(/problema relatado pelo cliente/i).fill('Equipamento de teste não inicia.');
  await page.getByLabel(/técnico responsável pelo diagnóstico/i).selectOption({ label: 'Tecnico Um' });
  await page.getByRole('button', { name: /abrir ordem de serviço/i }).click();
  await expect(page).toHaveURL(/\/ordens\/[0-9a-f-]+$/);
  await expect(page.getByText(equipment, { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByText(equipment, { exact: true })).toBeVisible();
  const accessibility = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();
  expect(accessibility.violations).toEqual([]);
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByText(equipment, { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
});
