import { test, expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

async function signIn(page: Page, email: string) {
  const password = process.env['BANCADA_DEMO_PASSWORD'];
  if (!password) throw new Error('Real UI tests require BANCADA_DEMO_PASSWORD');
  await page.goto('/entrar');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/senha/i).fill(password);
  await page.getByRole('button', { name: /entrar/i }).click();
  await expect(page).toHaveURL(/\/ordens$/);
}

test('receptionist, technician and customer complete a repair in the browser', async ({ browser, page }) => {
  await signIn(page, 'atendente@bancada.example');
  await page.getByRole('complementary').getByRole('link', { name: 'Nova ordem', exact: true }).click();
  await page.getByLabel(/nome completo do cliente/i).fill('Cliente fictício do percurso');
  await page.getByLabel(/email de contato/i).fill('journey@example.test');
  await page.getByLabel(/equipamento \(marca/i).fill(`Console de teste ${Date.now()}`);
  await page.getByLabel(/problema relatado pelo cliente/i).fill('Equipamento desliga durante o uso.');
  await page.getByLabel(/técnico responsável pelo diagnóstico/i).selectOption({ label: 'Tecnico Um' });
  await page.getByRole('button', { name: /abrir ordem de serviço/i }).click();
  await expect(page).toHaveURL(/\/ordens\/[0-9a-f-]+$/);
  const orderUrl = page.url();

  const techContext = await browser.newContext();
  const customerContext = await browser.newContext({ viewport: { width: 390, height: 844 } });
  try {
    const tech = await techContext.newPage();
    await signIn(tech, 'tecnico1@bancada.example');
    await tech.goto(orderUrl);
    await tech.getByRole('button', { name: /registrar diagnóstico/i }).click();
    await tech.getByLabel(/descrição do diagnóstico/i).fill('Falha na fonte confirmada. Troca da peça e teste de carga.');
    await tech.getByRole('button', { name: /salvar diagnóstico/i }).click();
    await expect(tech.locator('.status-badge')).toHaveText('Em diagnóstico');
    await tech.getByRole('button', { name: /publicar orçamento/i }).click();
    await tech.getByPlaceholder('Peça ou serviço...').fill('Reparo e teste da fonte');
    await tech.getByPlaceholder('0.00').fill('275.50');
    await tech.getByRole('button', { name: /publicar orçamento/i }).click();
    await expect(tech.locator('.status-badge')).toHaveText('Aguardando aprovação');
    await expect(tech.locator('.quote-total-value')).toContainText('275,50');

    await page.reload();
    await page.getByRole('button', { name: /gerar link de acompanhamento/i }).click();
    const access = page.getByLabel(/link de acompanhamento gerado/i);
    await expect(access).toHaveValue(/#token=/);
    const customer = await customerContext.newPage();
    await customer.goto(await access.inputValue());
    await expect(customer).toHaveURL(/\/acompanhar$/);
    await customer.getByRole('button', { name: 'Aprovar orçamento', exact: true }).click();
    await customer.getByRole('button', { name: 'Sim, aprovar serviço', exact: true }).click();
    await expect(customer.getByText(/orçamento aprovado com sucesso/i)).toBeVisible();
    expect((await new AxeBuilder({ page: customer }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze()).violations).toEqual([]);
    expect(await customer.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);

    await tech.reload();
    await tech.getByRole('button', { name: /iniciar reparo/i }).click();
    await expect(tech.locator('.status-badge')).toHaveText('Em reparo');
    await tech.getByRole('button', { name: /concluir reparo/i }).click();
    await expect(tech.locator('.status-badge')).toHaveText('Pronta para retirada');
    await page.reload();
    await page.getByRole('button', { name: /confirmar entrega ao cliente/i }).click();
    await expect(page.locator('.status-badge')).toHaveText('Entregue');
    await page.reload();
    await expect(page.locator('.status-badge')).toHaveText('Entregue');
    await expect(page.getByRole('list', { name: /histórico de alterações/i }).getByRole('listitem')).toHaveCount(7);
  } finally {
    await techContext.close();
    await customerContext.close();
  }
});
