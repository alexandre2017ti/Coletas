import { readFile, readdir } from "node:fs/promises";
import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { ColetasPage } from "./pages/ColetasPage";

for (const view of ["overview", "deliveries", "courier"]) {
  test(`${view}: acessibilidade automatizada, locale, layout e captura visual`, async ({
    page,
  }, testInfo) => {
    const app = new ColetasPage(page);
    await app.goto(view);
    await page.evaluate(() => document.fonts.ready);
    await app.assertNoOverflow();
    await expect(page.locator("html")).toHaveAttribute("lang", "pt-BR");
    await expect(
      page.getByText("Ambiente de demonstração", { exact: true }),
    ).toBeVisible();
    const results = await new AxeBuilder({ page })
      .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
      .analyze();
    expect(results.violations).toEqual([]);
    await page.screenshot({
      path: testInfo.outputPath(`${view}.png`),
      fullPage: true,
    });
  });
}

test("modal e validação preservam semântica acessível", async ({ page }) => {
  const app = new ColetasPage(page);
  await app.goto();
  await app.openPreview();
  await page.getByRole("button", { name: "Revisar prévia" }).click();
  const results = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  expect(results.violations).toEqual([]);
  const rect = await app.dialog.boundingBox();
  const viewport = page.viewportSize()!;
  expect(rect!.x).toBeGreaterThanOrEqual(0);
  expect(rect!.x + rect!.width).toBeLessThanOrEqual(viewport.width);
  expect(rect!.y + rect!.height).toBeLessThanOrEqual(viewport.height);
});

test("tokens documentados, fontes e scrollbar chegam ao runtime", async ({
  page,
}) => {
  const app = new ColetasPage(page);
  await app.goto();
  const design = await readFile(
    new URL("../../../DESIGN.md", import.meta.url),
    "utf8",
  );
  const mapping = {
    primary: "#155f49",
    ink: "#153731",
    canvas: "#f3f6f2",
    surface: "#ffffff",
    muted: "#52645a",
    border: "#d5dfd6",
  };
  for (const [token, value] of Object.entries(mapping)) {
    expect(design).toContain(`${token}: "${value}"`);
    expect(
      await page.evaluate(
        (name) =>
          getComputedStyle(document.documentElement)
            .getPropertyValue(`--coletas-${name}`)
            .trim(),
        token,
      ),
    ).toBe(value);
  }
  expect(
    await page
      .getByRole("heading", { level: 1 })
      .evaluate((element) => getComputedStyle(element).fontFamily),
  ).toContain("Manrope");
  expect(
    await page.evaluate(
      () => getComputedStyle(document.documentElement).scrollbarColor,
    ),
  ).not.toBe("auto");
  const result = await page.evaluate(() => {
    const element = document.createElement("div");
    element.style.overflow = "auto";
    document.body.append(element);
    const color = getComputedStyle(element).scrollbarColor;
    element.remove();
    return color;
  });
  expect(result).not.toBe("auto");
});

test("movimento reduzido, alto contraste e reflow estreito", async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: "reduce", forcedColors: "active" });
  await page.setViewportSize({ width: 320, height: 700 });
  const app = new ColetasPage(page);
  await app.goto("courier");
  await app.assertNoOverflow();
  await expect(
    page.getByRole("button", { name: "Aceitar entrega" }),
  ).toBeDisabled();
  expect(
    await page.evaluate(() =>
      getComputedStyle(document.documentElement)
        .getPropertyValue("--brick-motion-duration-fast")
        .trim(),
    ),
  ).toBe("0s");
  expect(
    await page.evaluate(
      () => getComputedStyle(document.documentElement).scrollbarColor,
    ),
  ).toBe("auto");
});

test("cada dono FLOWSTACK importado recebe seu CSS modular", async () => {
  const src = new URL("../src/", import.meta.url);
  const entry = await readFile(new URL("main.tsx", src), "utf8");
  expect(
    entry.match(/import ['"]@flowstack-ui\/brick\/styles\/core.css['"]/g),
  ).toHaveLength(1);
  expect(entry).not.toMatch(/import ['"]@flowstack-ui\/brick\/styles.css['"]/);
  for (const file of await readdir(src, { recursive: true })) {
    if (!/\.[jt]sx?$/.test(file)) continue;
    const source = await readFile(
      new URL(file.replaceAll("\\", "/"), src),
      "utf8",
    );
    expect(source).not.toContain("from '@flowstack-ui/atom");
    for (const match of source.matchAll(
      /from ['"]@flowstack-ui\/brick\/([a-z-]+)['"]/g,
    )) {
      expect(entry).toContain(`@flowstack-ui/brick/styles/${match[1]}.css`);
    }
  }
});
