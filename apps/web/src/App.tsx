import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import { Badge } from "@flowstack-ui/brick/badge";
import { Button } from "@flowstack-ui/brick/button";
import { Container } from "@flowstack-ui/brick/container";
import { Grid } from "@flowstack-ui/brick/grid";
import { NavList } from "@flowstack-ui/brick/nav-list";
import { Section } from "@flowstack-ui/brick/section";
import { Show } from "@flowstack-ui/brick/show";
import { HStack, Stack, VStack } from "@flowstack-ui/brick/stack";
import { Surface } from "@flowstack-ui/brick/surface";
import { Heading, Paragraph, Text } from "@flowstack-ui/brick/text";
import { ConnectionStatus, Glyph, Logo } from "./components/ui";
import { DeliveryDetails, DeliveryList } from "./components/DeliveryList";
import { PreviewRequest } from "./components/PreviewRequest";
import { Overview, CourierPreview } from "./components/Screens";
import { Registration } from "./components/Registration";
import { AccessPage } from "./components/AccessPage";
import { AccountPage } from "./components/AccountPage";
import { AdminReviews } from "./components/AdminReviews";
import { useSession } from "./api/client";
import { navigate, snapshot, subscribe } from "./navigation";
import type { DeliveryExample } from "./data/demo";

const destinations = [
  { view: "overview", label: "Visão geral", icon: "dashboard" },
  { view: "deliveries", label: "Entregas", icon: "box" },
  { view: "courier", label: "Visão do entregador", icon: "bike" },
  { view: "register-establishment", label: "Cadastrar estabelecimento", icon: "store" },
  { view: "register-courier", label: "Cadastrar entregador", icon: "bike" },
  { view: "login", label: "Entrar", icon: "shield" },
  { view: "account", label: "Minha conta", icon: "shield" },
  { view: "admin-reviews", label: "Análise administrativa", icon: "shield" },
] as const;

export default function App() {
  const query = useSyncExternalStore(subscribe, snapshot);
  const parameters = new URLSearchParams(query);
  const view = parameters.get("view") ?? "overview";
  const { profile } = useSession();
  const current = destinations.find((destination) => destination.view === view)
    ?? (view === "recovery" ? { label: "Recuperar acesso" } : view === "reset-password" ? { label: "Redefinir senha" } : undefined);
  const [draft, setDraft] = useState({ pickup: "", destination: "" });
  const [details, setDetails] = useState<DeliveryExample | null>(null);
  const detailTrigger = useRef<HTMLElement | null>(null);

  const title = current?.label ?? "Página não encontrada";
  useEffect(() => { document.title = `${title} — Coletas`; }, [title]);

  // Motivo: endereços ficam apenas na memória; sair do documento perde a prévia.
  // Mudança: docs/mudancas/2026-09-10-01-interface-operacional.md
  useEffect(() => {
    if (!draft.pickup && !draft.destination) return;
    function warn(event: BeforeUnloadEvent) {
      event.preventDefault();
      event.returnValue = "";
    }
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [draft]);

  function showDetails(delivery: DeliveryExample, trigger: HTMLElement) {
    detailTrigger.current = trigger;
    setDetails(delivery);
  }
  function closeDetails(open: boolean) {
    if (!open) setDetails(null);
  }

  return (
    <Container measure="max" gutter="md" className="app-container">
      <Grid.Root columns={{ initial: 1, lg: 12 }} gap={{ initial: 5, lg: 8 }}>
        <Grid.Item columnSpan={{ initial: 1, lg: 2 }}>
          <Section as="div" spacing="sm">
            <VStack as="aside" gap={8} aria-label="Menu principal">
              <Logo />
              <NavList.Root
                aria-label="Navegação principal"
                size="lg"
                variant="soft"
                tone="accent"
              >
                <NavList.List asChild>
                  <Stack
                    as="ul"
                    direction={{ initial: "row", lg: "column" }}
                    gap={2}
                    wrap
                  >
                    {destinations.filter(destination => destination.view !== "admin-reviews" || profile?.role === "Admin").map((destination) => (
                      <NavList.Item key={destination.view}>
                        <NavList.Link
                          href={`?view=${destination.view}`}
                          onClick={navigate}
                          active={view === destination.view}
                          startIcon={<Glyph name={destination.icon} raw />}
                        >
                          {destination.label}
                        </NavList.Link>
                      </NavList.Item>
                    ))}
                  </Stack>
                </NavList.List>
              </NavList.Root>
              <Show from="lg">
                <Surface level="subtle" inset="md" radius="surface">
                  <VStack gap={3}>
                    <Glyph name="shield" />
                    <Text weight="semibold">Tudo começa com confiança.</Text>
                    <Paragraph variant="body-sm" tone="secondary">
                      Cadastro, documentos e capacitação serão verificados antes
                      de operar.
                    </Paragraph>
                    <Badge tone="neutral" variant="outline">
                      Em preparação
                    </Badge>
                  </VStack>
                </Surface>
              </Show>
            </VStack>
          </Section>
        </Grid.Item>
        <Grid.Item columnSpan={{ initial: 1, lg: 10 }}>
          <Section as="div" spacing="sm">
            <VStack gap={7}>
              <Stack
                as="header"
                direction={{ initial: "column", md: "row" }}
                justify="between"
                align={{ initial: "start", md: "center" }}
                gap={3}
              >
                <HStack gap={3}>
                  <Badge size="lg" shape="circle" tone="accent">
                    <Glyph name={view === "courier" ? "bike" : "store"} />
                  </Badge>
                  <VStack gap={0}>
                    <Text weight="semibold">
                      {view === "courier"
                        ? "Experiência do entregador"
                        : "Meu estabelecimento"}
                    </Text>
                    <Text variant="body-sm" tone="secondary">
                      Central de entregas e coletas
                    </Text>
                  </VStack>
                </HStack>
                <Badge tone="warning" variant="soft">
                  Ambiente de demonstração
                </Badge>
              </Stack>
              {/* Região nomeada permite localizar o aviso por leitor de tela.
                  Mudança: docs/mudancas/2026-09-17-01-aceite-fase-1.md */}
              <Surface level="subtle" inset="md" radius="subtle" role="region" aria-label="Limites da demonstração">
                <HStack align="start" gap={3}>
                  <Glyph name="info" />
                  <Paragraph variant="body-sm">
                    Explore a interface com dados fictícios. Nenhuma entrega
                    será solicitada e nenhum entregador será acionado.
                  </Paragraph>
                </HStack>
              </Surface>
              <main id="main-content">
                <VStack gap={7}>
                  <Stack
                    direction={{ initial: "column", md: "row" }}
                    justify="between"
                    align={{ initial: "start", md: "center" }}
                    gap={4}
                  >
                    <VStack gap={2}>
                      <Heading
                        id="page-title"
                        tabIndex={-1}
                        level={1}
                        variant="title-lg"
                      >
                        {current?.label ?? "Página não encontrada"}
                      </Heading>
                      <Paragraph tone="secondary">
                        {view === "overview"
                          ? "Da sua porta ao destino. Tudo no mesmo lugar."
                          : view === "deliveries"
                            ? "Cada entrega, com o próximo passo bem claro."
                            : view === "courier"
                              ? "As informações que importam antes de sair."
                              : view === "register-courier" || view === "register-establishment"
                                ? "Preencha os dados abaixo para enviar seu cadastro para análise."
                                : current ? "Acesso seguro e acompanhamento do cadastro." : "Este endereço não corresponde a uma tela da prévia."}
                      </Paragraph>
                    </VStack>
                    {["overview", "deliveries"].includes(view) && (
                      <PreviewRequest draft={draft} onDraftChange={setDraft} />
                    )}
                  </Stack>
                  {view === "overview" && <Overview onDetails={showDetails} />}
                  {view === "deliveries" && (
                    <DeliveryList
                      parameters={parameters}
                      onDetails={showDetails}
                    />
                  )}
                  {view === "courier" && <CourierPreview />}
                  {view === "register-establishment" && <Registration key="establishment" kind="establishment" />}
                  {view === "register-courier" && <Registration key="courier" kind="courier" />}
                  {(view === "login" || view === "recovery" || view === "reset-password") && <AccessPage key={view} mode={view} />}
                  {view === "account" && <AccountPage />}
                  {view === "admin-reviews" && <AdminReviews />}
                  {!current && (
                    <Button href="?view=overview" onClick={navigate}>
                      Voltar à visão geral
                    </Button>
                  )}
                </VStack>
              </main>
              <ConnectionStatus />
              <HStack as="footer" justify="between" gap={3} wrap>
                <Text variant="body-sm" tone="secondary">
                  Coletas · Mais clareza em cada percurso.
                </Text>
                <Text variant="body-sm" tone="secondary">
                  Prévia web · Sem operação real
                </Text>
              </HStack>
            </VStack>
          </Section>
        </Grid.Item>
      </Grid.Root>
      <DeliveryDetails
        delivery={details}
        onOpenChange={closeDetails}
        triggerRef={detailTrigger}
      />
    </Container>
  );
}
