import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('login pendente abre conta e logout volta ao acesso', async ({ page }) => {
  await page.route('**/api/v1/**', route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/auth/login')) return route.fulfill({ status: 401, json: {} });
    if (path.endsWith('/auth/onboarding/login')) return route.fulfill({ json: { accessToken: 'onboarding', refreshToken: null } });
    if (path.endsWith('/auth/me')) return route.fulfill({ json: { userId: '1', email: 'teste@example.test', role: 'Establishment', status: 'Pending', courierId: null, documents: [], vehicles: [] } });
    if (path.endsWith('/account/review')) return route.fulfill({ json: { history: [] } });
    return route.fulfill({ status: 204 });
  });
  await page.goto('/?view=login');
  await page.getByLabel('E-mail', { exact: true }).fill('teste@example.test');
  await page.getByLabel(/^Senha/).fill('teste-password-123');
  await page.getByRole('button', { name: 'Mostrar senha', exact: true }).click();
  await expect(page.getByLabel(/^Senha/)).toHaveAttribute('type', 'text');
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(page.getByText('Aguardando análise', { exact: true })).toBeVisible();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.getByRole('button', { name: 'Sair da conta' }).click();
  await expect(page.getByRole('link', { name: 'Ir para login' })).toBeVisible();
});

test('recuperação genérica e redefinição retiram token da URL sem perder o envio', async ({ page }) => {
  let token = '';
  await page.route('**/api/v1/auth/reset-password', route => {
    token = route.request().postDataJSON().token;
    return route.fulfill({ status: 204 });
  });
  await page.goto('/?view=reset-password#token=test-only-token');
  await expect(page).not.toHaveURL(/#token/);
  await page.getByLabel(/^Senha/).fill('test-password-123');
  await page.getByLabel(/^Confirmar senha/).fill('test-password-123');
  await page.getByRole('button', { name: 'Redefinir senha', exact: true }).click();
  await expect(page.getByText('Senha redefinida. Entre com a nova senha.')).toBeVisible();
  expect(token).toBe('test-only-token');
});

test('conta comum não acessa fila administrativa', async ({ page }) => {
  await page.goto('/');
  await page.route('**/api/v1/auth/login', route => route.fulfill({ json: { accessToken: 'test', refreshToken: null } }));
  await page.route('**/api/v1/auth/me', route => route.fulfill({ json: { role: 'Courier' } }));
  await page.evaluate(async () => {
    const path = '/src/api/client.ts';
    const client = await import(path);
    await client.signIn('test@example.test', 'test-password');
    history.pushState(null, '', '?view=admin-reviews');
    dispatchEvent(new PopStateEvent('popstate'));
  });
  await expect(page.getByRole('alert')).toHaveText('Acesso restrito a administradores.');
});

test('início de análise usa mensagem padrão e envia somente a versão exibida', async ({ page }) => {
  let started = false;
  let requestBody: Record<string, unknown> | undefined;
  await page.route('**/api/v1/**', route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/auth/login')) return route.fulfill({ json: { accessToken: 'admin', refreshToken: null } });
    if (path.endsWith('/auth/me')) return route.fulfill({ json: { userId: 'admin', email: 'admin@example.test', role: 'Admin', status: 'Active', courierId: null, documents: [], vehicles: [] } });
    if (path.endsWith('/admin/reviews')) return route.fulfill({ json: { items: [{ userId: 'subject', role: 'Establishment', accountStatus: 'Enabled', reviewStatus: started ? 'InReview' : 'Pending', version: started ? 1 : 0 }], total: 1, page: 1, pageSize: 20 } });
    if (path.endsWith('/admin/reviews/subject')) return route.fulfill({ json: { review: { userId: 'subject', role: 'Establishment', accountStatus: 'Enabled', reviewStatus: started ? 'InReview' : 'Pending', version: started ? 1 : 0 }, history: [] } });
    if (path.endsWith('/admin/users/subject/profile')) return route.fulfill({ json: { userId: 'subject', email: 'empresa@example.test', role: 'Establishment', status: 'Pending', courierId: null, documents: [], vehicles: [], registration: { name: 'Empresa teste', tradeName: 'Teste', taxId: '31147798000122', phoneWhatsApp: '65999999999' } } });
    if (path.endsWith('/admin/reviews/subject/decisions')) { requestBody = route.request().postDataJSON(); started = true; return route.fulfill({ status: 200, json: {} }); }
    return route.fulfill({ status: 204 });
  });
  await page.goto('/');
  await page.evaluate(async () => {
    const client = await import('/src/api/client.ts');
    await client.signIn('admin@example.test', 'test-password');
    history.pushState(null, '', '?view=admin-reviews');
    dispatchEvent(new PopStateEvent('popstate'));
  });
  await page.getByRole('button', { name: 'Analisar cadastro', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Iniciar análise', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Iniciar análise', exact: true }).click();
  await expect.poll(() => requestBody).toEqual({ action: 'Start', expectedVersion: 0 });
});

test('cadastro rejeitado pode reabrir análise antes de uma nova decisão', async ({ page }) => {
  let reopened = false;
  let requestBody: Record<string, unknown> | undefined;
  await page.route('**/api/v1/**', route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/auth/login')) return route.fulfill({ json: { accessToken: 'admin', refreshToken: null } });
    if (path.endsWith('/auth/me')) return route.fulfill({ json: { userId: 'admin', email: 'admin@example.test', role: 'Admin', status: 'Active', courierId: null, documents: [], vehicles: [] } });
    if (path.endsWith('/admin/reviews')) return route.fulfill({ json: { items: [{ userId: 'subject', role: 'Establishment', accountStatus: 'Enabled', reviewStatus: reopened ? 'InReview' : 'Rejected', version: reopened ? 2 : 1 }], total: 1, page: 1, pageSize: 20 } });
    if (path.endsWith('/admin/reviews/subject')) return route.fulfill({ json: { review: { userId: 'subject', role: 'Establishment', accountStatus: 'Enabled', reviewStatus: reopened ? 'InReview' : 'Rejected', version: reopened ? 2 : 1 }, history: [] } });
    if (path.endsWith('/admin/users/subject/profile')) return route.fulfill({ json: { userId: 'subject', email: 'empresa@example.test', role: 'Establishment', status: 'Pending', courierId: null, documents: [], vehicles: [], registration: { name: 'Empresa teste', tradeName: 'Teste', taxId: '31147798000122', phoneWhatsApp: '65999999999' } } });
    if (path.endsWith('/admin/reviews/subject/decisions')) { requestBody = route.request().postDataJSON(); reopened = true; return route.fulfill({ status: 200, json: {} }); }
    return route.fulfill({ status: 204 });
  });
  await page.goto('/');
  await page.evaluate(async () => {
    const client = await import('/src/api/client.ts');
    await client.signIn('admin@example.test', 'test-password');
    history.pushState(null, '', '?view=admin-reviews');
    dispatchEvent(new PopStateEvent('popstate'));
  });
  await page.getByRole('button', { name: 'Analisar cadastro', exact: true }).click();
  await page.getByRole('button', { name: 'Reabrir análise', exact: true }).click();
  await expect(page.getByRole('alertdialog')).toBeVisible();
  await page.getByRole('alertdialog').getByRole('button', { name: 'Confirmar decisão' }).click();
  await expect.poll(() => requestBody).toEqual({ action: 'Reopen', reason: '', internalNote: '', expectedVersion: 1 });
  await expect(page.getByText('Análise: Em análise')).toBeVisible();
});
