import { expect, test } from '@playwright/test';
import { formatPhone, validPhone, formatPlate, validPlate, formatCnpj, validCnpj } from '../src/registrationValidation';

test('máscaras e validadores preservam limites e ambos os modelos', () => {
  expect(formatPhone('+55 (65) 99999-9999')).toBe('(65) 99999-9999');
  expect(formatPhone('abc6533334444')).toBe('(65) 3333-4444');
  expect(formatPhone('6599999999999999')).toBe('(65) 99999-9999');
  expect(validPhone('659999999999')).toBe(false);
  expect(formatPlate('abc1234')).toBe('ABC-1234');
  expect(formatPlate('ABC-1234')).toBe('ABC-1234');
  expect(formatPlate('abc1d23')).toBe('ABC1D23');
  expect(validPlate('ABC1DD3')).toBe(false);
  expect(formatCnpj('19131243000197')).toBe('19.131.243/0001-97');
  expect(validCnpj('19131243000198')).toBe(false);
  expect(validCnpj('00000000E08G12')).toBe(true);
  expect(validCnpj('00000000000000')).toBe(false);
});

test('cadastro envia telefone e placa normalizados e mostra sucesso', async ({ page }) => {
  let sent: Record<string,string> | undefined;
  await page.route('**/api/v1/auth/register/couriers', async route => {
    sent = route.request().postDataJSON();
    await route.fulfill({ status: 201, json: { status: 'Pending' } });
  });
  await page.goto('/?view=register-courier');
  await page.getByLabel('Nome completo', { exact: true }).fill('Entregador de teste');
  await page.getByLabel('CPF', { exact: true }).fill('52998224725');
  await expect(page.getByLabel('CPF', { exact: true })).toHaveValue('529.982.247-25');
  await page.getByLabel('WhatsApp', { exact: true }).fill('65999999999');
  await expect(page.getByLabel('WhatsApp', { exact: true })).toHaveValue('(65) 99999-9999');
  await page.getByLabel('E-mail', { exact: true }).fill('teste@example.test');
  await page.getByLabel('Senha', { exact: true }).fill('test-password-123');
  await page.getByLabel('Placa', { exact: true }).fill('abc1d23');
  await page.getByRole('button', {name: 'Enviar cadastro'}).click();
  await expect(page.getByText('Cadastro enviado. Aguarde a análise da equipe.')).toBeVisible();
  expect(sent?.phoneWhatsApp).toBe('65999999999');
  expect(sent?.plate).toBe('ABC1D23');
  expect(sent?.cpf).toBe('52998224725');
});

test('validação mostra erro e preserva dados sem enviar requisição', async ({ page }) => {
  let writes = 0;
  await page.route('**/api/v1/auth/register/**', async route => { writes++; await route.abort(); });
  await page.goto('/?view=register-courier');
  await page.getByLabel('WhatsApp', { exact: true }).fill('123');
  await page.getByRole('button', {name: 'Enviar cadastro'}).click();
  await expect(page.getByLabel('WhatsApp', { exact: true })).toHaveAttribute('aria-invalid', 'true');
  await expect(page.getByLabel('CPF', {exact:true})).toBeFocused();
  expect(writes).toBe(0);
});

test('consulta CNPJ preenche dados editáveis sem sobrescrever telefone', async ({ page }) => {
  await page.route('https://brasilapi.com.br/api/cnpj/v1/*', route => route.fulfill({ json: { cnpj:'19131243000197', razao_social:'Empresa de teste', nome_fantasia:'Loja de teste' } }));
  await page.goto('/?view=register-establishment');
  await page.getByLabel('WhatsApp', { exact: true }).fill('6533334444');
  await page.getByLabel('CNPJ', { exact: true }).fill('19131243000197');
  await expect(page.getByLabel('CNPJ', {exact:true})).toHaveValue('19.131.243/0001-97');
  await page.getByRole('button', {name:'Buscar CNPJ'}).click();
  await expect(page.getByLabel('Razão social', {exact:true})).toHaveValue('Empresa de teste');
  await expect(page.getByLabel('Nome comercial', {exact:true})).toHaveValue('Loja de teste');
  await expect(page.getByLabel('WhatsApp', {exact:true})).toHaveValue('(65) 3333-4444');
  await page.getByLabel('Nome comercial', {exact:true}).fill('Nome ajustado');
});

test('consulta indisponível mantém cadastro manual habilitado', async ({ page }) => {
  await page.route('https://brasilapi.com.br/api/cnpj/v1/*', route => route.fulfill({ status:503, body:'indisponível' }));
  await page.goto('/?view=register-establishment');
  await page.getByLabel('CNPJ', {exact:true}).fill('19131243000197');
  await page.getByRole('button', {name:'Buscar CNPJ'}).click();
  await expect(page.getByText('Consulta indisponível. Tente novamente ou preencha manualmente.')).toBeVisible();
  await expect(page.getByLabel('Razão social', {exact:true})).toBeEnabled();
});

test('editar CNPJ cancela consulta anterior e ignora preenchimento atrasado', async ({ page }) => {
  let release!: () => void;
  const gate = new Promise<void>(resolve => { release = resolve; });
  await page.route('https://brasilapi.com.br/api/cnpj/v1/*', async route => { await gate; await route.fulfill({json:{cnpj:'19131243000197', razao_social:'Resposta antiga', nome_fantasia:'Antigo'}}).catch(() => {}); });
  await page.goto('/?view=register-establishment');
  await page.getByLabel('CNPJ', {exact:true}).fill('19131243000197');
  await page.getByRole('button', {name:'Buscar CNPJ'}).click();
  await expect(page.getByRole('button', {name:'Consultando…'})).toBeDisabled();
  await page.getByLabel('CNPJ', {exact:true}).fill('00000000E08G12');
  release();
  await expect(page.getByLabel('Razão social', {exact:true})).toHaveValue('');
  await page.getByRole('button', {name:'Buscar CNPJ'}).click();
  await expect(page.getByText(/consulta aceita apenas CNPJ numérico/)).toBeVisible();
});

test('placa permite hífen, limita identificador e pode ser apagada', async ({ page }) => {
  await page.goto('/?view=register-courier');
  const plate = page.getByLabel('Placa', { exact: true });
  await plate.pressSequentially('abc-1234');
  await expect(plate).toHaveValue('ABC-1234');
  await plate.pressSequentially('5');
  await expect(plate).toHaveValue('ABC-1234');
  for (let i = 0; i < 8; i++) await plate.press('Backspace');
  await expect(plate).toHaveValue('');
  await plate.pressSequentially('abc1d234');
  await expect(plate).toHaveValue('ABC1D23');
});

for (const kind of ['courier', 'establishment']) {
  test(`${kind}: bordas, placeholders, alturas e alinhamento`, async ({page}, info) => {
    await page.goto(`/?view=register-${kind}`);
    const fields = page.locator('.registration-form input');
    for (const field of await fields.all()) {
      await expect(field).not.toHaveAttribute('placeholder', '');
      const metrics = await field.evaluate(el => { const s = getComputedStyle(el); const r = el.getBoundingClientRect(); return {border:s.borderTopWidth, color:s.borderTopColor, height:r.height}; });
      expect(metrics.border).toBe('1px');
      expect(metrics.color).not.toBe('rgba(0, 0, 0, 0)');
      expect(metrics.height).toBe(46);
    }
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    if (kind === 'establishment' && info.project.name === 'desktop') {
      const first = await page.getByLabel('Razão social', {exact:true}).boundingBox();
      const second = await page.getByLabel('Nome comercial', {exact:true}).boundingBox();
      expect(first?.y).toBe(second?.y);
    }
    await page.screenshot({path:info.outputPath(`${kind}-form.png`), fullPage:true});
  });
}
