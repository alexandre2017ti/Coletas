import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "@fontsource-variable/manrope";
import "@flowstack-ui/brick/reset.css";
// Motivo: o CSS completo mediu 633,76 kB; carregamos somente os donos usados,
// mantendo core uma única vez. Teste de entrega CSS impede componentes sem estilo.
// Mudança: docs/mudancas/2026-09-10-01-interface-operacional.md
import "@flowstack-ui/brick/styles/core.css";
import "@flowstack-ui/brick/styles/badge.css";
import "@flowstack-ui/brick/styles/button.css";
import "@flowstack-ui/brick/styles/card.css";
import "@flowstack-ui/brick/styles/container.css";
import "@flowstack-ui/brick/styles/dialog.css";
import "@flowstack-ui/brick/styles/field.css";
import "@flowstack-ui/brick/styles/form.css";
import "@flowstack-ui/brick/styles/frame.css";
import "@flowstack-ui/brick/styles/grid.css";
import "@flowstack-ui/brick/styles/icon.css";
import "@flowstack-ui/brick/styles/input.css";
import "@flowstack-ui/brick/styles/password-toggle-field.css";
import "@flowstack-ui/brick/styles/select.css";
import "@flowstack-ui/brick/styles/alert-dialog.css";
import "@flowstack-ui/brick/styles/nav-list.css";
import "@flowstack-ui/brick/styles/section.css";
import "@flowstack-ui/brick/styles/show.css";
import "@flowstack-ui/brick/styles/stack.css";
import "@flowstack-ui/brick/styles/surface.css";
import "@flowstack-ui/brick/styles/text.css";
import "./theme.css";
import "./index.css";
import App from "./App.tsx";
import { AppErrorBoundary } from "./components/AppErrorBoundary";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AppErrorBoundary>
      <App />
    </AppErrorBoundary>
  </StrictMode>,
);
