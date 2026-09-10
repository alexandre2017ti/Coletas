import { expect, type Page } from "@playwright/test";

export class ColetasPage {
  constructor(readonly page: Page) {}
  get heading() {
    return this.page.getByRole("heading", { level: 1 });
  }
  get search() {
    return this.page.getByRole("textbox", { name: "Buscar nos exemplos" });
  }
  get dialog() {
    return this.page.getByRole("dialog");
  }
  async goto(view = "overview") {
    await this.page.goto(`/?view=${view}`);
    await expect(this.heading).toBeVisible();
  }
  async openPreview() {
    await this.page
      .getByRole("button", { name: "Prévia de solicitação", exact: true })
      .click();
    await expect(this.dialog).toBeVisible();
  }
  async fillPreview() {
    await this.page
      .getByRole("textbox", { name: "Endereço de coleta" })
      .fill("Rua Exemplo, 10");
    await this.page
      .getByRole("textbox", { name: "Endereço de entrega" })
      .fill("Praça Exemplo, 20");
    await this.page.getByRole("button", { name: "Revisar prévia" }).click();
  }
  async assertNoOverflow() {
    expect(
      await this.page.evaluate(
        () => document.documentElement.scrollWidth <= window.innerWidth,
      ),
    ).toBe(true);
  }
}
