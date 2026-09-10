import type { MouseEvent } from "react";

export function subscribe(listener: () => void) {
  window.addEventListener("popstate", listener);
  return () => window.removeEventListener("popstate", listener);
}
export function snapshot() {
  return window.location.search;
}
export function updateLocation(parameters: URLSearchParams, replace = false) {
  const method = replace ? "replaceState" : "pushState";
  window.history[method](null, "", `?${parameters.toString()}`);
  window.dispatchEvent(new PopStateEvent("popstate"));
}
export function navigate(event: MouseEvent<HTMLAnchorElement>) {
  // Motivo: preservar abrir em nova aba e o rascunho ao navegar internamente.
  // Mudança: docs/mudancas/2026-09-10-01-interface-operacional.md
  if (
    event.button !== 0 ||
    event.ctrlKey ||
    event.metaKey ||
    event.shiftKey ||
    event.altKey
  )
    return;
  event.preventDefault();
  updateLocation(new URL(event.currentTarget.href).searchParams);
  document.getElementById("page-title")?.focus();
}
