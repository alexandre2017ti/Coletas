import { test, expect, type Page } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
test.use({ trace: 'off', screenshot: 'off' });

// Execução opt-in exclusivamente com scripts/phase1-acceptance.sh e banco isolado.
// Nunca apontar esta fixture para a demonstração ou produção.
test('cadastro, documentos e aprovação pelo navegador com PostgreSQL real', async ({ browser }) => {
  test.skip(process.env.COLETAS_REAL_ACCEPTANCE !== '1', 'Exige API/banco de aceite isolados.');
  test.setTimeout(180_000);
  const password = randomBytes(18).toString('hex');
  const tag = randomBytes(6).toString('hex');
  const adminEmail = `admin-${tag}@example.test`;
  const storeEmail = `store-${tag}@example.test`;
  const courierEmail = `courier-${tag}@example.test`;
  const context = await browser.newContext();
  const page = await context.newPage();
  const admin = await context.newPage();
  const base = `http://127.0.0.1:${process.env.PLAYWRIGHT_PORT ?? 5191}`;
  function sql(query: string) {
    return execFileSync('wsl', ['-d', 'coletas-dev', '--', 'docker', 'exec', '-i', 'coletas-preview-postgres-1', 'psql', '-X', '-v', 'ON_ERROR_STOP=1', '-U', 'coletas', '-d', 'coletas_phase1_acceptance', '-At'], { input: query, encoding: 'utf8' }).trim();
  }
  async function enter(target: Page, email: string) {
    await target.goto(base + '/?view=login');
    await target.getByLabel('E-mail', { exact: true }).fill(email);
    await target.getByLabel(/^Senha/).fill(password);
    await target.getByRole('button', { name: 'Entrar', exact: true }).click();
    await expect(target.getByRole('heading', { name: 'Minha conta', exact: true })).toBeVisible();
  }
  async function decision(label: string, index = 0) {
    if (label === 'Iniciar análise') {
      await admin.getByRole('button', { name: label, exact: true }).click();
      await expect(admin.getByText('Análise: Em análise')).toBeVisible();
      return;
    }
    // A mensagem é opcional na interface, mas o aceite a preenche para validar a confirmação e o histórico público.
    // Mudança: docs/mudancas/2026-09-21-01-seletor-aceite-reabertura.md
    await admin.getByLabel('Mensagem ao titular (opcional)', { exact: true }).fill('Conferência fictícia de homologação');
    await admin.getByRole('button', { name: label, exact: true }).nth(index).click();
    await expect(admin.getByRole('alertdialog')).toBeVisible();
    await admin.getByRole('alertdialog').getByRole('button', { name: 'Confirmar decisão' }).click();
    await expect(admin.getByRole('alertdialog')).not.toBeVisible();
  }
  try {
    const seed = await page.request.post(base + '/api/v1/auth/register/establishments', { data: { email: adminEmail, password, legalName: 'Administrador fictício', tradeName: 'Aceite', taxId: '19131243000197', phoneWhatsApp: '65988888888' } });
    expect(seed.status()).toBe(201);
    // Elevação somente da fixture recém-criada no banco descartável, nunca bootstrap de produção.
    sql(`UPDATE identity."Users" SET "Role"='Admin', "Status"='Active' WHERE "Email"='${adminEmail}';`);
    await page.goto(base + '/?view=register-establishment');
    await page.getByLabel('CNPJ', { exact: true }).fill('31147798000122');
    await page.getByLabel('Razão social', { exact: true }).fill('Empresa fictícia de teste');
    await page.getByLabel('Nome comercial', { exact: true }).fill('Loja de teste');
    await page.getByLabel('WhatsApp', { exact: true }).fill('65977777777');
    await page.getByLabel('E-mail', { exact: true }).fill(storeEmail);
    await page.getByLabel(/^Senha/).fill(password);
    await page.getByRole('button', { name: 'Enviar cadastro' }).click();
    await expect(page.getByText('Cadastro enviado. Aguarde a análise da equipe.')).toBeVisible();
    await page.goto(base + '/?view=register-courier');
    await page.getByLabel('CPF', { exact: true }).fill('52998224725');
    await page.getByLabel('Nome completo', { exact: true }).fill('Entregador fictício');
    await page.getByLabel('WhatsApp', { exact: true }).fill('65999999999');
    await page.getByLabel('E-mail', { exact: true }).fill(courierEmail);
    await page.getByLabel(/^Senha/).fill(password);
    await page.getByLabel('Placa', { exact: true }).fill('ABC1234');
    await page.getByRole('button', { name: 'Enviar cadastro' }).click();
    await expect(page.getByText('Cadastro enviado. Aguarde a análise da equipe.')).toBeVisible();
    expect(sql('SELECT count(*) FROM couriers."Couriers";')).toBe('1');
    await enter(admin, adminEmail);
    await admin.getByRole('link', { name: 'Analisar cadastros', exact: true }).click();
    await admin.getByRole('button', { name: 'Analisar cadastro', exact: true }).first().click();
    await decision('Iniciar análise');
    await decision('Solicitar correção');
    await decision('Iniciar análise');
    await decision('Aprovar cadastro');
    await admin.getByRole('button', { name: 'Voltar à fila' }).click();
    await enter(page, courierEmail);
    for (const type of ['CNH', 'Documento do veículo']) {
      await page.getByRole('radio', { name: type, exact: true }).check();
      await page.getByLabel('Validade (AAAA-MM-DD)', { exact: true }).fill('2030-12-31');
      await page.locator('input[type=file]').setInputFiles({ name: 'teste.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4 documento ficticio') });
      await page.getByRole('button', { name: 'Enviar documento e solicitar nova análise' }).click();
      await expect(page.getByRole('link', { name: 'Ir para login' })).toBeVisible();
      if (type === 'CNH') await enter(page, courierEmail);
    }
    await admin.getByRole('button', { name: 'Atualizar fila' }).click();
    await admin.getByRole('button', { name: 'Analisar cadastro', exact: true }).click();
    await decision('Aprovar documento', 0);
    await decision('Aprovar documento', 1);
    await decision('Iniciar análise');
    await decision('Aprovar cadastro');
    await enter(page, courierEmail);
    await expect(page.getByText('Conta aprovada', { exact: true })).toBeVisible();
    await page.screenshot({ path: '../../artifacts/account-approved.png', fullPage: true });
    await admin.screenshot({ path: '../../artifacts/admin-review.png', fullPage: true });
    const download = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Baixar documento' }).first().click();
    // Chromium acrescenta a extensão reconhecida pelo MIME ao nome do attachment.
    // Mudança: docs/mudancas/2026-09-17-01-aceite-fase-1.md
    expect((await download).suggestedFilename()).toBe('documento.pdf');
    expect(sql('SELECT count(*) FROM identity."ReviewEvents";')).toBe('10');
    // Duas decisões partem da mesma versão: apenas uma pode persistir no PostgreSQL.
    // A fixture usa o cliente autenticado da página sem extrair nem registrar tokens.
    const courierId = sql(`SELECT "Id" FROM identity."Users" WHERE "Email"='${courierEmail}';`);
    const outcomes = await admin.evaluate(async userId => {
      const modulePath = '/src/api/client.ts';
      const client = await import(modulePath);
      const detail = await client.api(`/admin/reviews/${userId}`);
      return Promise.all(['Bloqueio fictício A', 'Bloqueio fictício B'].map(async reason => {
        try {
          await client.api(`/admin/reviews/${userId}/decisions`, { method: 'POST', body: JSON.stringify({ expectedVersion: detail.review.version, action: 'Block', reason }) });
          return 200;
        } catch (error) { return (error as { status: number }).status; }
      }));
    }, courierId);
    expect(outcomes.sort()).toEqual([200, 409]);
    await page.getByRole('button', { name: 'Atualizar situação' }).click();
    await expect(page.getByRole('link', { name: 'Ir para login' })).toBeVisible();
    expect(sql(`SELECT "Status" FROM identity."Users" WHERE "Id"='${courierId}';`)).toBe('Blocked');
    expect(sql(`SELECT count(*) FROM identity."ReviewEvents" WHERE "UserId"='${courierId}' AND "Action"='Block';`)).toBe('1');
    // A fixture anterior consome a cota pública de autenticação. Respeitar a janela
    // real de um minuto, sem relaxar o rate limit nem repetir um POST incerto.
    await new Promise(resolve => setTimeout(resolve, 60_000));
    const rejectedEmail = `rejected-${tag}@example.test`;
    const extra = await page.request.post(base + '/api/v1/auth/register/establishments', { data: {
      email: rejectedEmail, password, legalName: 'Reprovação fictícia', tradeName: 'Teste', taxId: '04252011000110', phoneWhatsApp: '65966666666',
    } });
    expect(extra.status()).toBe(201);
    const rejectedId = sql(`SELECT "Id" FROM identity."Users" WHERE "Email"='${rejectedEmail}';`);
    const rejected = await admin.evaluate(async userId => {
      const modulePath = '/src/api/client.ts';
      const client = await import(modulePath);
      for (const [expectedVersion, action] of ['Start', 'Reject', 'Reopen'].entries()) {
        await client.api(`/admin/reviews/${userId}/decisions`, { method: 'POST', body: JSON.stringify({ expectedVersion, action, reason: 'Cadastro fictício reprovado' }) });
      }
      return (await client.api(`/admin/reviews/${userId}`)).review.reviewStatus;
    }, rejectedId);
    expect(rejected).toBe('InReview');
    expect(sql(`SELECT "Status" FROM identity."Users" WHERE "Id"='${rejectedId}';`)).toBe('Pending');
  } finally { await context.close().catch(() => {}); }
});
