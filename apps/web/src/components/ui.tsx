import { useEffect, useRef, useState } from "react";
import { Badge } from "@flowstack-ui/brick/badge";
import { Button } from "@flowstack-ui/brick/button";
import { Icon } from "@flowstack-ui/brick/icon";
import { HStack, Stack, VStack } from "@flowstack-ui/brick/stack";
import { Surface } from "@flowstack-ui/brick/surface";
import { Paragraph, Text } from "@flowstack-ui/brick/text";

const glyphs: Record<string, string> = {
  dashboard: "M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z",
  box: "M3 7l9-4 9 4v10l-9 4-9-4V7z M3 7l9 4 9-4 M12 11v10 M7 5l10 4",
  bike: "M6 17a3 3 0 1 0 0 .01 M18 17a3 3 0 1 0 0 .01 M6 17l4-8 5 8H6 M9 9h8l-2-5h-3 M10 9l-2-3H5",
  route:
    "M5 5a2 2 0 1 0 0 .01 M19 19a2 2 0 1 0 0 .01 M7 5h8a4 4 0 0 1 0 8H9a4 4 0 0 0 0 8h8",
  check: "M5 12l4 4L19 6",
  arrow: "M4 12h16 M14 6l6 6-6 6",
  shield: "M12 3l8 3v6c0 5-8 9-8 9s-8-4-8-9V6l8-3z M8 12l3 3 5-6",
  store:
    "M3 10l2-6h14l2 6 M4 10v10h16V10 M9 20v-6h6v6 M3 10c0 3 4 3 4 0 0 3 5 3 5 0 0 3 5 3 5 0 0 3 4 3 4 0",
  info: "M12 3a9 9 0 1 0 .01 0 M12 11v6 M12 7v1",
  plus: "M12 5v14 M5 12h14",
  search: "M10 3a7 7 0 1 0 .01 0 M15 15l6 6",
  phone: "M6 3h12v18H6z M10 17h4",
  calendar: "M4 5h16v16H4z M8 3v4 M16 3v4 M4 10h16 M8 14h2 M14 14h2",
  training: "M2 9l10-6 10 6-10 6-10-6z M6 12v5l6 4 6-4v-5 M22 9v8",
  signal: "M4 18v3 M9 13v8 M14 8v13 M19 3v18",
};

export function Glyph({ name, raw = false }: { name: string; raw?: boolean }) {
  const drawing = (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.65"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d={glyphs[name] ?? glyphs.box} />
    </svg>
  );
  return raw ? drawing : <Icon size="md">{drawing}</Icon>;
}

export function Logo() {
  return (
    <HStack gap={3}>
      <svg
        className="brand-mark"
        width="38"
        height="38"
        viewBox="0 0 38 38"
        aria-hidden="true"
      >
        <rect width="38" height="38" rx="12" fill="currentColor" />
        <path
          d="M25 11H15a5 5 0 0 0 0 10h8a3 3 0 0 1 0 6H13 M22 8l4 3-4 3 M16 24l-4 3 4 3"
          fill="none"
          stroke="var(--coletas-surface)"
          strokeWidth="2.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
      <Text variant="title-md" weight="semibold">
        coletas
      </Text>
    </HStack>
  );
}

export function Metric({
  label,
  value,
  detail,
  icon,
}: {
  label: string;
  value: string;
  detail: string;
  icon: string;
}) {
  return (
    <HStack align="start" gap={4}>
      <Badge tone="accent" shape="circle" size="xl">
        <Glyph name={icon} />
      </Badge>
      <VStack gap={1}>
        <Text variant="body-sm" tone="secondary">
          {label}
        </Text>
        <Text variant="title-lg" className="numeric">
          {value}
        </Text>
        <Text variant="body-sm" tone="secondary">
          {detail}
        </Text>
      </VStack>
    </HStack>
  );
}

export function RouteSummary({
  pickup,
  destination,
}: {
  pickup: string;
  destination: string;
}) {
  return (
    <VStack as="ol" gap={5} className="route-summary" aria-label="Percurso">
      <HStack as="li" align="start" gap={3}>
        <Badge tone="accent" shape="circle">
          A
        </Badge>
        <VStack gap={1}>
          <Text variant="body-sm" tone="secondary">
            Coleta
          </Text>
          <Text weight="medium">{pickup}</Text>
        </VStack>
      </HStack>
      <HStack as="li" align="start" gap={3}>
        <Badge tone="neutral" shape="circle">
          B
        </Badge>
        <VStack gap={1}>
          <Text variant="body-sm" tone="secondary">
            Destino
          </Text>
          <Text weight="medium">{destination}</Text>
        </VStack>
      </HStack>
    </VStack>
  );
}

export function ConnectionStatus() {
  const [status, setStatus] = useState("Conexão ainda não verificada");
  const [busy, setBusy] = useState(false);
  const active = useRef<AbortController | null>(null);
  useEffect(
    () => () => {
      active.current?.abort();
      active.current = null;
    },
    [],
  );

  async function checkConnection() {
    if (active.current) return;
    const controller = new AbortController();
    active.current = controller;
    setBusy(true);
    setStatus("Verificando conexão…");
    // Motivo: prontidão testa dependências, não habilita entregas; timeout finito
    // e identidade da requisição impedem feedback obsoleto após desmontagem.
    // Mudança: docs/mudancas/2026-09-10-01-interface-operacional.md
    const timeout = window.setTimeout(() => controller.abort(), 10_000);
    try {
      const response = await fetch("/health/ready", {
        signal: controller.signal,
      });
      if (!response.ok) throw new Error("unavailable");
      if (active.current === controller) setStatus("Conexão disponível");
    } catch {
      if (active.current === controller)
        setStatus("Serviço indisponível. Tente novamente em instantes.");
    } finally {
      window.clearTimeout(timeout);
      if (active.current === controller) {
        active.current = null;
        setBusy(false);
      }
    }
  }
  return (
    <Surface
      as="section"
      aria-label="Conexão com o servidor"
      level="base"
      inset="md"
      radius="surface"
      bordered
    >
      <Stack
        direction={{ initial: "column", md: "row" }}
        justify="between"
        gap={4}
        align={{ initial: "stretch", md: "center" }}
      >
        <HStack gap={3} align="start">
          <Glyph name="signal" />
          <VStack gap={1}>
            <Text weight="semibold">Conexão com o servidor</Text>
            <Paragraph
              role="status"
              aria-live="polite"
              variant="body-sm"
              tone="secondary"
              className="connection-message"
            >
              {status}
            </Paragraph>
            <Text variant="body-sm" tone="secondary">
              O teste não habilita operações nesta demonstração.
            </Text>
          </VStack>
        </HStack>
        <Button
          onClick={checkConnection}
          disabled={busy}
          aria-busy={busy}
          variant="outline"
          tone="neutral"
        >
          Verificar conexão
        </Button>
      </Stack>
    </Surface>
  );
}
