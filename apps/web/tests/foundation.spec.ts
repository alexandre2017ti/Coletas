import { expect, test } from "@playwright/test";

test("conexão disponível e layout sem rolagem horizontal", async ({ page }) => {
  await page.route("**/health/ready", (route) =>
    route.fulfill({ status: 200, body: "Healthy" }),
  );
  await page.goto("/");
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Visão geral",
  );
  await page.getByRole("button", { name: "Verificar conexão" }).click();
  await expect(page.getByRole("status")).toHaveText("Conexão disponível");
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);
});

test("falha permite nova tentativa", async ({ page }) => {
  await page.route("**/health/ready", (route) =>
    route.fulfill({ status: 503, body: "Unhealthy" }),
  );
  await page.goto("/");
  await page.getByRole("button", { name: "Verificar conexão" }).click();
  await expect(page.getByRole("status")).toContainText("Serviço indisponível");
  await expect(
    page.getByRole("button", { name: "Verificar conexão" }),
  ).toBeEnabled();
});

test("requisição lenta mantém geometria, bloqueia duplicata e permite recuperação", async ({
  page,
}) => {
  let release: () => void = () => undefined;
  const pending = new Promise<void>((resolve) => {
    release = resolve;
  });
  let calls = 0;
  await page.route("**/health/ready", async (route) => {
    calls += 1;
    await pending;
    await route.fulfill({ status: 200, body: "Healthy" });
  });
  await page.goto("/");
  const button = page.getByRole("button", { name: "Verificar conexão" });
  // Comparar a mesma fonte nos dois estados, não fallback versus fonte carregada.
  // Motivo: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md
  await page.evaluate(() => document.fonts.ready);
  const before = await button.boundingBox();
  await button.click();
  await expect(button).toBeDisabled();
  await expect(page.getByRole("status")).toHaveText("Verificando conexão…");
  expect((await button.boundingBox())!.width).toBe(before!.width);
  release();
  await expect(page.getByRole("status")).toHaveText("Conexão disponível");
  expect(calls).toBe(1);
});

test("falha de rede não bloqueia leitura e retry pode recuperar", async ({
  page,
}) => {
  await page.route("**/health/ready", (route) =>
    route.abort("internetdisconnected"),
  );
  await page.goto("/");
  const button = page.getByRole("button", { name: "Verificar conexão" });
  await button.click();
  await expect(page.getByRole("status")).toContainText("Serviço indisponível");
  await page.unroute("**/health/ready");
  await page.route("**/health/ready", (route) =>
    route.fulfill({ status: 200, body: "Healthy" }),
  );
  await button.click();
  await expect(page.getByRole("status")).toHaveText("Conexão disponível");
  await expect(
    page.getByRole("button", { name: "Prévia de solicitação" }),
  ).toBeEnabled();
});
