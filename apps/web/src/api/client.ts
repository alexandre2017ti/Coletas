import { useSyncExternalStore } from "react";

export type Role = "Admin" | "Operator" | "Establishment" | "Courier";
export interface Vehicle { id: string; type: string; plate: string }
export interface AccountDocument { id: string; type: string; status: string; expiresAt: string | null; hasFile: boolean; reason?: string | null }
export interface Profile { userId: string; email: string; role: Role; status: string; courierId: string | null; vehicles: Vehicle[]; documents: AccountDocument[]; reason?: string | null }
export interface AuthResponse { accessToken: string; expiresAt: string; refreshToken: string | null }
type Session = { profile: Profile | null; error: string };
let session: Session = { profile: null, error: "" };
let accessToken: string | null = null;
let refreshToken: string | null = null;
let refreshFlight: Promise<void> | null = null;
let generation = 0;
const listeners = new Set<() => void>();
const apiBase = (import.meta.env.VITE_API_URL ?? "").replace(/\/$/, "");
function publish(patch: Partial<Session>) {
  session = { ...session, ...patch };
  listeners.forEach(listener => listener());
}
export function useSession() {
  return useSyncExternalStore(listener => {
    listeners.add(listener);
    return () => listeners.delete(listener);
  }, () => session);
}
export class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) { super(message); this.status = status; }
}
function clearSession(error = "") {
  generation++;
  accessToken = refreshToken = null;
  refreshFlight = null;
  publish({ profile: null, error });
}
function assertCurrent(current: number) {
  if (current !== generation) throw new ApiError("A sessão mudou. Atualize antes de continuar.", 401);
}

// Tokens ficam só em memória: a API retorna JSON, não cookie HttpOnly.
// Recarregar exige login. Não repetir gravações após falha de rede incerta.
// Motivo: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md
async function transport(path: string, init: RequestInit = {}, authenticated = false): Promise<Response> {
  const headers = new Headers(init.headers);
  if (init.body && !(init.body instanceof FormData)) headers.set("Content-Type", "application/json");
  if (authenticated && accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
  try {
    return await fetch(`${apiBase}/api/v1${path}`, {
      ...init, headers, credentials: "omit",
      signal: init.signal ? AbortSignal.any([init.signal, AbortSignal.timeout(30_000)]) : AbortSignal.timeout(30_000),
    });
  } catch {
    throw new ApiError(init.signal?.aborted
      ? "Operação interrompida. Confira os dados atualizados antes de reenviar."
      : "Não foi possível confirmar a resposta. Seus dados foram mantidos. Verifique sua conexão e confirme o cadastro antes de tentar novamente.", 0);
  }
}
async function checked(response: Response) {
  if (response.ok) return response;
  const messages: Record<number, string> = {
    400: "Confira os dados informados e tente novamente.",
    401: "Não foi possível autenticar. Confira o e-mail e a senha.",
    403: "Você não tem permissão para esta ação. Entre novamente ou procure a equipe.",
    404: "Registro não encontrado. Atualize a página e tente novamente.",
    409: "Já existe cadastro com esses dados ou eles foram alterados. Atualize antes de reenviar.",
    413: "O arquivo excede o tamanho permitido.", 415: "Formato de arquivo não permitido.",
    429: "Muitas tentativas. Aguarde alguns instantes antes de tentar novamente.",
  };
  // Somente erros esperados de validação/conflito são exibidos; nunca stack traces 5xx.
  if ([400, 409].includes(response.status)) {
    const body = await response.json().catch(() => null);
    const detail = body?.errors && typeof body.errors === "object"
      ? Object.values(body.errors).flat().filter(value => typeof value === "string").join(" ")
      : body?.error;
    if (typeof detail === "string" && detail.trim()) throw new ApiError(detail.slice(0, 1000), response.status);
  }
  throw new ApiError(messages[response.status] ?? "Serviço indisponível. Tente novamente em instantes.", response.status);
}
async function renew() {
  if (!refreshToken) throw new ApiError("Sua sessão terminou. Entre novamente.", 401);
  if (!refreshFlight) {
    const current = generation;
    const token = refreshToken;
    const flight = (async () => {
      const response = await checked(await transport("/auth/refresh", {
        method: "POST", body: JSON.stringify({ refreshToken: token }),
      }));
      const auth: AuthResponse = await response.json();
      assertCurrent(current);
      accessToken = auth.accessToken;
      refreshToken = auth.refreshToken;
    })();
    refreshFlight = flight;
    void flight.finally(() => { if (refreshFlight === flight) refreshFlight = null; }).catch(() => {});
  }
  return refreshFlight;
}
export async function apiResponse(path: string, init: RequestInit = {}, authenticated = true) {
  const current = generation;
  const sentToken = accessToken;
  let response = await transport(path, init, authenticated);
  if (authenticated) assertCurrent(current);
  if (authenticated && response.status === 401) {
    try {
      // Um 401 atrasado pode chegar depois da renovação de outra requisição.
      if (sentToken === accessToken) await renew();
      assertCurrent(current);
      response = await transport(path, init, authenticated);
      assertCurrent(current);
      if (response.status === 401) clearSession("Sua sessão terminou. Entre novamente.");
    } catch (error) {
      if (current === generation && error instanceof ApiError && [401, 403].includes(error.status))
        clearSession("Sua sessão terminou. Entre novamente.");
      throw error;
    }
  }
  return checked(response);
}
export async function api<T = void>(path: string, init: RequestInit = {}, authenticated = true): Promise<T> {
  const response = await apiResponse(path, init, authenticated);
  return response.status === 204 ? undefined as T : response.json();
}
export const jsonBody = (value: unknown) => JSON.stringify(value);
export async function loadProfile() {
  const current = generation;
  const profile = await api<Profile>("/auth/me");
  assertCurrent(current);
  publish({ profile, error: "" });
  return profile;
}
export async function signIn(email: string, password: string) {
  clearSession();
  const current = generation;
  let response = await transport("/auth/login", { method: "POST", body: jsonBody({ email, password }) });
  assertCurrent(current);
  if (response.status === 401)
    response = await transport("/auth/onboarding/login", { method: "POST", body: jsonBody({ email, password }) });
  const auth: AuthResponse = await (await checked(response)).json();
  assertCurrent(current);
  accessToken = auth.accessToken;
  refreshToken = auth.refreshToken;
  return loadProfile();
}
export async function signOut() {
  // Capturar Bearer antes de limpar; falha remota nunca mantém credenciais locais.
  const response = transport("/auth/logout", { method: "POST" }, true);
  clearSession();
  const current = generation;
  channel?.postMessage("logout");
  try { await checked(await response); }
  catch (error) {
    if (current === generation) publish({ error: "Sessão local encerrada, mas não foi possível confirmar a revogação no servidor." });
    throw error;
  }
}
const channel = typeof BroadcastChannel !== "undefined" ? new BroadcastChannel("coletas-session") : null;
if (channel) channel.onmessage = event => {
  if (event.data === "logout") clearSession("Sessão encerrada em outra aba.");
};
