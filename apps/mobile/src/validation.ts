export type VehicleType = 'Motorcycle' | 'Car';
export type DocumentType = 'DriverLicense' | 'VehicleRegistration';
export const vehicleChoices: ReadonlyArray<{ value: VehicleType; label: string }> = [{ value: 'Motorcycle', label: 'Motocicleta' }, { value: 'Car', label: 'Automóvel' }];
export const documentChoices: ReadonlyArray<{ value: DocumentType; label: string }> = [{ value: 'DriverLicense', label: 'CNH' }, { value: 'VehicleRegistration', label: 'Documento do veículo' }];
export const accountStatus: Record<string, string> = { Pending: 'Aguardando aprovação', Active: 'Conta aprovada', Blocked: 'Conta bloqueada' };
export const documentStatus: Record<string, string> = { Pending: 'Pendente', UnderReview: 'Em análise', Approved: 'Aprovado', Rejected: 'Reprovado', Expired: 'Vencido', Blocked: 'Bloqueado' };
import { validCpf, validPhone, validPlate } from "../../shared/registrationValidation";
export type Registration = { cpf: string; fullName: string; email: string; phoneWhatsApp: string; password: string; confirmation: string; vehicleType: VehicleType; plate: string };

export function emailError(value: string) { return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()) ? undefined : 'Informe um e-mail válido, como nome@exemplo.com.'; }
export function passwordError(value: string) {
  // Limites correspondem à validação da API, não são uma política independente do app.
  // Mudança: docs/mudancas/2026-09-10-17-mobile-fase1.md
  return value.length < 12 || value.length > 128 ? 'Use entre 12 e 128 caracteres.' : undefined;
}
export function registrationErrors(value: Registration): Record<string, string | undefined> {
  return { cpf: validCpf(value.cpf) ? undefined : 'Informe um CPF válido.', fullName: value.fullName.trim() ? undefined : 'Informe seu nome completo.', email: emailError(value.email), phoneWhatsApp: validPhone(value.phoneWhatsApp) ? undefined : 'Informe seu WhatsApp com DDD.', password: passwordError(value.password), confirmation: value.confirmation === value.password ? undefined : 'As senhas precisam ser iguais.', plate: validPlate(value.plate) ? undefined : 'Use ABC-1234 ou ABC1D23.' };
}
export function expirationIso(value: string): string | null {
  if (!value.trim()) return null;
  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(value);
  if (!match) throw new Error('Informe a validade no formato DD/MM/AAAA.');
  const [, d, m, y] = match;
  const date = new Date(`${y}-${m}-${d}T23:59:59.000Z`);
  if (Number.isNaN(date.getTime()) || date.getUTCDate() !== Number(d) || date.getUTCMonth() + 1 !== Number(m)) throw new Error('Informe uma data que exista no calendário.');
  if (date.getTime() <= Date.now()) throw new Error('A validade deve estar no futuro.');
  return date.toISOString();
}
