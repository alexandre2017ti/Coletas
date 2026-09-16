// Motivo: máscaras são apresentação; API recebe identificadores normalizados.
// Mudança: docs/mudancas/2026-09-11-01-validacao-cadastros.md
export function phoneDigits(value: string): string {
  let digits = value.replace(/\D/g, "");
  if ((digits.length === 12 || digits.length === 13) && digits.startsWith("55")) digits = digits.slice(2);
  return digits;
}
export function formatPhone(value: string): string {
  const d = phoneDigits(value).slice(0, 11);
  if (d.length <= 2) return d ? `(${d}` : "";
  const middle = d.length > 10 ? 7 : 6;
  return `(${d.slice(0, 2)}) ${d.slice(2, middle)}${d.length > middle ? `-${d.slice(middle)}` : ""}`;
}
export function validPhone(value: string): boolean {
  return /^[1-9][0-9](?:[2-5][0-9]{7}|9[0-9]{8})$/.test(phoneDigits(value));
}
export function plateValue(value: string): string { return value.toUpperCase().replace(/[^A-Z0-9]/g, ""); }
// CPF é enviado sem máscara e validado novamente pela API; registro 2026-09-14-04-identificadores-exclusivos-entregador.md.
export function cpfValue(value: string): string { return value.replace(/\D/g, ""); }
export function formatCpf(value: string): string {
  const d = cpfValue(value).slice(0, 11);
  return d.slice(0, 3) + (d.length > 3 ? `.${d.slice(3, 6)}` : "")
    + (d.length > 6 ? `.${d.slice(6, 9)}` : "") + (d.length > 9 ? `-${d.slice(9)}` : "");
}
export function validCpf(value: string): boolean {
  const d = value.replace(/[.\s-]/g, "");
  if (!/^[0-9]{11}$/.test(d) || /^(.)\1+$/.test(d)) return false;
  for (let length = 9; length <= 10; length++) {
    let sum = 0;
    for (let i = 0; i < length; i++) sum += Number(d[i]) * (length + 1 - i);
    const digit = sum * 10 % 11;
    if (Number(d[length]) !== (digit === 10 ? 0 : digit)) return false;
  }
  return true;
}
export function formatPlate(value: string): string {
  // Hífen é separador visual da placa antiga; ele ocupa a oitava posição sem aumentar o identificador real.
  // Mudança: docs/mudancas/2026-09-11-02-hifen-placa.md
  const typedHyphen = /^[A-Za-z]{3}-/.test(value);
  const p = plateValue(value).slice(0, 7);
  // Preservar somente o separador após as três letras permite apagar o campo inteiro.
  // Mudança: docs/mudancas/2026-09-14-01-aceite-formularios.md
  if (typedHyphen) return `${p.slice(0, 3)}-${p.slice(3)}`;
  return p.length > 4 && /[0-9]/.test(p[4]) ? `${p.slice(0, 3)}-${p.slice(3)}` : p;
}
export function validPlate(value: string): boolean { return /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(plateValue(value)); }
export function cnpjValue(value: string): string { return value.toUpperCase().replace(/[^A-Z0-9]/g, ""); }
export function formatCnpj(value: string): string {
  const d = cnpjValue(value).slice(0, 14);
  return d.slice(0, 2) + (d.length > 2 ? `.${d.slice(2, 5)}` : "") + (d.length > 5 ? `.${d.slice(5, 8)}` : "")
    + (d.length > 8 ? `/${d.slice(8, 12)}` : "") + (d.length > 12 ? `-${d.slice(12)}` : "");
}
export function validCnpj(value: string): boolean {
  const d = cnpjValue(value);
  if (!/^[A-Z0-9]{12}[0-9]{2}$/.test(d) || /^(.)\1+$/.test(d)) return false;
  // Receita Federal: módulo 11; letras usam ASCII menos 48, mesmo algoritmo dos números.
  for (const length of [12, 13]) {
    let sum = 0;
    for (let i = 0; i < length; i++) sum += (d.charCodeAt(i) - 48) * ((length - 1 - i) % 8 + 2);
    const remainder = sum % 11;
    if (Number(d[length]) !== (remainder < 2 ? 0 : 11 - remainder)) return false;
  }
  return true;
}
