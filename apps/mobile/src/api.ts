type Auth = { accessToken: string; refreshToken: string | null };
export type Profile = { userId: string; email: string; status: string; role: string; courierId: string | null; vehicles: Array<{ id: string; type: string; plate: string }>; documents: Array<{ id: string; type: string; status: string; expiresAt: string | null; reason: string | null }>; reason: string | null };
let auth: Auth | null = null;
let renewal: Promise<void> | null = null;
let generation = 0;

export class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) { super(message); this.status = status; }
}

export function clearSession() { auth = null; renewal = null; generation++; }
export async function request<T = void>(path: string, init: RequestInit = {}, secured = true): Promise<T> {
  const base = process.env.EXPO_PUBLIC_API_URL?.replace(/\/$/, "");
  if (!base || (!__DEV__ && !base.startsWith("https://"))) throw new Error("Configure uma URL HTTPS para a API.");
  const current = generation;
  const send = async () => {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 30_000);
    const cancel = () => controller.abort();
    init.signal?.addEventListener("abort", cancel);
    if (init.signal?.aborted) controller.abort();
    try {
      return await fetch(base + "/api/v1" + path, { ...init, signal: controller.signal, headers: {
        ...(init.body && !(init.body instanceof FormData) ? { "Content-Type": "application/json" } : {}),
        ...(secured && auth ? { Authorization: "Bearer " + auth.accessToken } : {}), ...init.headers,
      } });
    } catch { throw new Error("Resposta não confirmada. Verifique a conexão e confira os dados antes de reenviar."); }
    finally { clearTimeout(timeout); init.signal?.removeEventListener("abort", cancel); }
  };
  const sentToken = auth?.accessToken;
  let response = await send();
  if (secured && current !== generation) throw new Error("Sessão alterada. Entre novamente.");
  if (secured && response.status === 401 && auth?.refreshToken) {
    if (sentToken === auth.accessToken) {
      if (!renewal) {
        const token = auth.refreshToken;
        const flight = request<Auth>("/auth/refresh", { method: "POST", body: JSON.stringify({ refreshToken: token }) }, false).then(value => {
          if (current === generation) auth = value;
        });
        renewal = flight;
        void flight.finally(() => { if (renewal === flight) renewal = null; }).catch(() => {});
      }
      try { await renewal; }
      catch (error) {
        if (current === generation && error instanceof ApiError && [401, 403].includes(error.status)) clearSession();
        throw error;
      }
    }
    if (current !== generation) throw new Error("Sessão alterada. Entre novamente.");
    response = await send();
  }
  if (!response.ok) {
    if (secured && response.status === 401) clearSession();
    const body = [400, 409].includes(response.status) ? await response.json().catch(() => null) : null;
    throw new ApiError(typeof body?.error === "string" ? body.error : response.status === 401 ? "Entre novamente; credenciais inválidas ou sessão expirada." : response.status === 403 ? "Acesso não permitido." : "Não foi possível concluir. Confira os dados ou tente novamente.", response.status);
  }
  return response.status === 204 ? undefined as T : response.json();
}
export async function login(email: string, password: string) {
  clearSession();
  const current = generation;
  // Contas pendentes só recebem escopo de cadastro; nunca liberar operação por decisão do app.
  // Mudança: docs/mudancas/2026-09-16-04-mobile-acesso-cpf.md
  let tokens: Auth;
  try { tokens = await request<Auth>("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }, false); }
  catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) throw error;
    tokens = await request<Auth>("/auth/onboarding/login", { method: "POST", body: JSON.stringify({ email, password }) }, false);
  }
  if (generation !== current) throw new Error("Sessão alterada. Entre novamente.");
  auth = tokens;
  return request<Profile>("/auth/me");
}
export async function logout() {
  const current = generation;
  const response = request("/auth/logout", { method: "POST" });
  // Captura o Bearer antes de apagar a memória; revogação remota pode falhar.
  auth = null;
  // Uma resposta antiga de logout não pode apagar um login posterior.
  // Mudança: docs/mudancas/2026-09-17-01-aceite-fase-1.md
  try { await response; } finally { if (current === generation) clearSession(); }
}
