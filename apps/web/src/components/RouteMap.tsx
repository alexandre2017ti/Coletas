// Motivo: desenho esquemático dispensa provedor/GPS e não sugere precisão real.
// Coordenadas pertencem à arte, nunca ao cálculo de distâncias ou ao despacho.
// Mudança: docs/mudancas/2026-09-10-01-interface-operacional.md
export function RouteMap({ compact = false }: { compact?: boolean }) {
  return (
    <svg
      className={`route-map${compact ? " route-map-compact" : ""}`}
      viewBox="0 0 620 300"
      role="img"
      aria-label="Desenho ilustrativo de uma coleta A e destinos B e C, sem escala geográfica"
    >
      <rect width="620" height="300" rx="12" fill="var(--coletas-canvas)" />
      <g
        fill="var(--coletas-map-block)"
        stroke="var(--coletas-border)"
        strokeWidth="1"
      >
        <rect x="28" y="22" width="87" height="58" rx="9" />
        <rect x="144" y="22" width="124" height="58" rx="9" />
        <rect x="295" y="22" width="85" height="58" rx="9" />
        <rect x="408" y="22" width="178" height="58" rx="9" />
        <rect x="28" y="106" width="87" height="70" rx="9" />
        <rect x="144" y="106" width="124" height="70" rx="9" />
        <rect x="295" y="106" width="85" height="70" rx="9" />
        <rect x="408" y="106" width="178" height="70" rx="9" />
        <rect x="28" y="203" width="87" height="66" rx="9" />
        <rect x="144" y="203" width="124" height="66" rx="9" />
        <rect x="295" y="203" width="85" height="66" rx="9" />
        <rect x="408" y="203" width="178" height="66" rx="9" />
      </g>
      <path
        d="M65 284C120 233 195 277 243 242s87-46 132-28 47 65 135 56 72-28 118-32"
        fill="none"
        stroke="var(--coletas-map-water)"
        strokeWidth="19"
      />
      <g fill="var(--coletas-map-park)">
        <rect x="158" y="119" width="96" height="42" rx="12" />
        <circle cx="441" cy="46" r="12" />
        <circle cx="465" cy="49" r="9" />
      </g>
      <path
        d="M78 94H282V188H394V94H510"
        fill="none"
        stroke="var(--coletas-surface)"
        strokeWidth="10"
        strokeLinejoin="round"
      />
      <path
        d="M78 94H282V188H394V94H510"
        fill="none"
        stroke="var(--coletas-primary)"
        strokeWidth="3"
        strokeDasharray="5 6"
        strokeLinejoin="round"
      />
      <g
        fontFamily="var(--coletas-font)"
        fontSize="12"
        fill="var(--coletas-muted)"
        textAnchor="middle"
      >
        <text x="206" y="145">
          PARQUE
        </text>
        <text x="331" y="56">
          CENTRO
        </text>
        <text x="494" y="149">
          JARDIM
        </text>
      </g>
      <g
        fill="var(--coletas-primary)"
        stroke="var(--coletas-surface)"
        strokeWidth="4"
      >
        <circle cx="78" cy="94" r="18" />
        <circle cx="282" cy="188" r="18" />
        <circle cx="510" cy="94" r="18" />
      </g>
      <g
        fill="var(--coletas-surface)"
        textAnchor="middle"
        fontFamily="var(--coletas-font)"
        fontWeight="700"
        fontSize="14"
      >
        <text x="78" y="99">
          A
        </text>
        <text x="282" y="193">
          B
        </text>
        <text x="510" y="99">
          C
        </text>
      </g>
      <text
        x="28"
        y="292"
        fontSize="10"
        fill="var(--coletas-muted)"
        fontFamily="var(--coletas-font)"
      >
        TRAJETO ILUSTRATIVO · SEM ESCALA
      </text>
    </svg>
  );
}
