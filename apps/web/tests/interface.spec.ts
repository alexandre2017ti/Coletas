import { expect, test } from "@playwright/test";
import { ColetasPage } from "./pages/ColetasPage";

test("navegação, título e destino ativo permanecem coerentes", async ({
  page,
}) => {
  const app = new ColetasPage(page);
  await app.goto();
  await page.getByRole("link", { name: "Entregas", exact: true }).click();
  await expect(app.heading).toHaveText("Entregas");
  await expect(page).toHaveTitle("Entregas — Coletas");
  await expect(
    page.getByRole("link", { name: "Entregas", exact: true }),
  ).toHaveAttribute("aria-current", "page");
  await page.getByRole("link", { name: "Visão do entregador" }).click();
  await expect(
    page.getByRole("button", { name: "Aceitar entrega" }),
  ).toBeDisabled();
  await expect(page.getByText("Ainda não verificado")).toHaveCount(4);
  await page.goBack();
  await expect(app.heading).toHaveText("Entregas");
  await app.assertNoOverflow();
});

test("busca e filtros restauram pela URL e oferecem estado vazio recuperável", async ({
  page,
}) => {
  const app = new ColetasPage(page);
  await app.goto("deliveries");
  await app.search.fill("Jardim");
  await expect(
    page.getByText("2 de 6 exemplos", { exact: true }),
  ).toBeVisible();
  await page.getByRole("link", { name: "Em rota", exact: true }).click();
  await expect(
    page.getByText("1 de 6 exemplos", { exact: true }),
  ).toBeVisible();
  await page.reload();
  await expect(app.search).toHaveValue("Jardim");
  await expect(
    page.getByText("1 de 6 exemplos", { exact: true }),
  ).toBeVisible();
  await app.search.fill("inexistente");
  await expect(
    page.getByRole("heading", { name: "Nenhuma entrega encontrada" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Limpar busca" }).click();
  await expect(app.search).toBeFocused();
  await expect(app.search).toHaveValue("");
  await expect(
    page.getByText("2 de 6 exemplos", { exact: true }),
  ).toBeVisible();
  await app.assertNoOverflow();
});

test("composição IME só consolida a busca ao terminar", async ({ page }) => {
  const app = new ColetasPage(page);
  await app.goto("deliveries");
  await app.search.dispatchEvent("compositionstart");
  await app.search.fill("Jardim");
  await expect(
    page.getByText("6 de 6 exemplos", { exact: true }),
  ).toBeVisible();
  await app.search.dispatchEvent("compositionend", { data: "Jardim" });
  await expect(
    page.getByText("2 de 6 exemplos", { exact: true }),
  ).toBeVisible();
});

test("detalhes com teclado, foco contido e retorno ao acionador", async ({
  page,
}) => {
  const app = new ColetasPage(page);
  await app.goto("deliveries");
  const trigger = page.getByRole("button", { name: "Ver detalhes de EX-1042" });
  await trigger.focus();
  await page.keyboard.press("Enter");
  await expect(app.dialog).toBeVisible();
  await expect(
    app.dialog.getByRole("heading", { name: "Entrega EX-1042" }),
  ).toBeVisible();
  const close = page.getByRole("button", { name: "Fechar detalhes" });
  await close.focus();
  await page.keyboard.press("Tab");
  expect(
    await app.dialog.evaluate((element) =>
      element.contains(document.activeElement),
    ),
  ).toBe(true);
  await page.keyboard.press("Shift+Tab");
  expect(
    await app.dialog.evaluate((element) =>
      element.contains(document.activeElement),
    ),
  ).toBe(true);
  await page.keyboard.press("Escape");
  await expect(app.dialog).toBeHidden();
  await expect(trigger).toBeFocused();
});

test("prévia valida, preserva campos e nunca escreve na rede", async ({
  page,
}) => {
  const writes: string[] = [];
  page.on("request", (request) => {
    if (["POST", "PUT", "PATCH", "DELETE"].includes(request.method()))
      writes.push(request.url());
  });
  const app = new ColetasPage(page);
  await app.goto();
  await app.openPreview();
  await page.getByRole("button", { name: "Revisar prévia" }).click();
  const pickup = page.getByRole("textbox", { name: "Endereço de coleta" });
  await expect(pickup).toBeFocused();
  await expect(pickup).toHaveAttribute("aria-invalid", "true");
  await expect(
    page.getByText("Informe um endereço de exemplo para a coleta."),
  ).toBeVisible();
  await expect(pickup).toHaveAccessibleDescription(/Informe um endereço/);
  await app.fillPreview();
  await expect(
    page.getByRole("heading", { name: "Prévia pronta — não enviada" }),
  ).toBeFocused();
  await page.getByRole("button", { name: "Fechar prévia" }).click();
  await page.getByRole("link", { name: "Entregas", exact: true }).click();
  await app.openPreview();
  await expect(pickup).toHaveValue("Rua Exemplo, 10");
  await expect(
    page.getByRole("textbox", { name: "Endereço de entrega" }),
  ).toHaveValue("Praça Exemplo, 20");
  expect(writes).toEqual([]);
  expect(new URL(page.url()).search).not.toContain("Exemplo");
  expect(
    await page.evaluate(() => [localStorage.length, sessionStorage.length]),
  ).toEqual([0, 0]);
  await app.assertNoOverflow();
});

test("rota desconhecida oferece recuperação", async ({ page }) => {
  const app = new ColetasPage(page);
  await app.goto("desconhecida");
  await expect(app.heading).toHaveText("Página não encontrada");
  await page.getByRole("link", { name: "Voltar à visão geral" }).click();
  await expect(app.heading).toHaveText("Visão geral");
});
