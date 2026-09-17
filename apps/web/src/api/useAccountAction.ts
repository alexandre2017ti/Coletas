import { useCallback, useEffect, useRef, useState } from "react";
// Compartilha somente ciclo de requisição; regras e campos continuam nos respectivos fluxos.
// Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
export function useAccountAction() {
  const controller = useRef<AbortController | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const cancel = useCallback(() => { controller.current?.abort(); controller.current = null; }, []);
  useEffect(() => cancel, [cancel]);
  const run = useCallback(async (action: (signal: AbortSignal) => Promise<void>) => {
    if (controller.current) return;
    const request = new AbortController();
    controller.current = request;
    setBusy(true); setError("");
    try { await action(request.signal); }
    catch (failure) { if (!request.signal.aborted) setError(failure instanceof Error ? failure.message : "Não foi possível concluir."); }
    finally { if (controller.current === request) controller.current = null; if (!request.signal.aborted) setBusy(false); }
  }, []);
  return { busy, error, run, cancel };
}
