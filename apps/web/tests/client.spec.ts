import { test, expect } from '@playwright/test';

test('timeout mantém dados e trava envio duplicado sem repetir POST', async ({ page }) => {
  test.setTimeout(45_000);
  let calls = 0;
  await page.route('**/api/v1/auth/register/couriers', () => { calls++; });
  await page.goto('/?view=register-courier');
  await page.getByLabel('CPF', { exact: true }).fill('52998224725');
  await page.getByLabel('Nome completo', { exact: true }).fill('Entregador de teste');
  await page.getByLabel('WhatsApp', { exact: true }).fill('65999999999');
  await page.getByLabel('E-mail', { exact: true }).fill('test@example.test');
  await page.getByLabel('Senha', { exact: true }).fill('test-password-123');
  await page.getByLabel('Placa', { exact: true }).fill('ABC1234');
  await page.getByRole('button', { name: 'Enviar cadastro' }).click();
  await expect(page.getByRole('button', { name: 'Enviando…' })).toBeDisabled();
  await page.locator('form').evaluate(form => {
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
  });
  await expect(page.getByRole('alert')).toContainText('Não foi possível confirmar a resposta.', { timeout: 35_000 });
  await expect(page.getByLabel('E-mail', { exact: true })).toHaveValue('test@example.test');
  await expect(page.getByRole('button', { name: 'Enviar cadastro' })).toBeEnabled();
  expect(calls).toBe(1);
});

// Exercita o módulo real no Vite, sem inventar uma tela de login.
// Motivo: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md
test('cliente renova uma vez para requisições concorrentes e envia refresh JSON', async ({ page }) => {
  let refreshes = 0;
  await page.route('**/api/v1/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/auth/login'))
      return route.fulfill({ json: { accessToken: 'old', refreshToken: 'refresh-old' } });
    if (path.endsWith('/auth/refresh')) {
      refreshes++;
      expect(route.request().postDataJSON()).toEqual({ refreshToken: 'refresh-old' });
      return route.fulfill({ json: { accessToken: 'new', refreshToken: 'refresh-new' } });
    }
    if (path.endsWith('/auth/me')) return route.fulfill({ json: { role: 'Courier' } });
    return route.fulfill({ status: route.request().headers().authorization === 'Bearer new' ? 200 : 401, json: { ok: true } });
  });
  await page.goto('/');
  const result = await page.evaluate(async () => {
    const path = '/src/api/client.ts';
    const client = await import(path);
    await client.signIn('test@example.test', 'test-password');
    return Promise.all([client.api('/probe'), client.api('/probe')]);
  });
  expect(result).toEqual([{ ok: true }, { ok: true }]);
  expect(refreshes).toBe(1);
});

test('logout envia Bearer e elimina sessão local mesmo se o servidor falhar', async ({ page }) => {
  await page.route('**/api/v1/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/auth/login')) return route.fulfill({ json: { accessToken: 'access', refreshToken: 'refresh' } });
    if (path.endsWith('/auth/logout')) {
      expect(route.request().headers().authorization).toBe('Bearer access');
      return route.fulfill({ status: 503, body: 'private stack trace' });
    }
    if (path.endsWith('/probe')) {
      expect(route.request().headers().authorization).toBeUndefined();
      return route.fulfill({ json: {} });
    }
    return route.fulfill({ json: { role: 'Courier' } });
  });
  await page.goto('/');
  const message = await page.evaluate(async () => {
    const path = '/src/api/client.ts';
    const client = await import(path);
    await client.signIn('test@example.test', 'test-password');
    let message = '';
    try { await client.signOut(); } catch (error) { message = (error as Error).message; }
    await client.api('/probe');
    return message;
  });
  expect(message).toBe('Serviço indisponível. Tente novamente em instantes.');
});

for (const status of [400, 409, 503, 0]) {
  test(`cadastro mantém campos e não repete envio após erro ${status}`, async ({ page }) => {
    let calls = 0;
    await page.route('**/api/v1/auth/register/couriers', async route => {
      calls++;
      if (!status) return route.abort('failed');
      return route.fulfill({ status, json: status === 400 ? { errors: { email: ['Confira o e-mail.'] } } : { error: status === 409 ? 'Placa já cadastrada.' : 'private stack trace' } });
    });
    await page.goto('/?view=register-courier');
    await page.getByLabel('CPF', { exact: true }).fill('52998224725');
    await page.getByLabel('Nome completo', { exact: true }).fill('Entregador de teste');
    await page.getByLabel('WhatsApp', { exact: true }).fill('65999999999');
    await page.getByLabel('E-mail', { exact: true }).fill('test@example.test');
    await page.getByLabel('Senha', { exact: true }).fill('test-password-123');
    await page.getByLabel('Placa', { exact: true }).fill('ABC1234');
    await page.getByRole('button', { name: 'Enviar cadastro' }).click();
    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByRole('alert')).not.toContainText('private stack trace');
    if (status === 400) await expect(page.getByRole('alert')).toContainText('Confira o e-mail.');
    if (status === 409) await expect(page.getByRole('alert')).toContainText('Placa já cadastrada.');
    await expect(page.getByLabel('E-mail', { exact: true })).toHaveValue('test@example.test');
    await expect(page.getByLabel('Placa', { exact: true })).toHaveValue('ABC-1234');
    await expect(page.getByRole('button', { name: 'Enviar cadastro' })).toBeEnabled();
    expect(calls).toBe(1);
  });
}
