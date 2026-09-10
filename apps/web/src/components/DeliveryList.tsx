import {
  useRef,
  useState,
  type CompositionEvent,
  type MouseEvent,
  type RefObject,
} from "react";
import { Badge } from "@flowstack-ui/brick/badge";
import { Button } from "@flowstack-ui/brick/button";
import { Card } from "@flowstack-ui/brick/card";
import { Dialog } from "@flowstack-ui/brick/dialog";
import { Field } from "@flowstack-ui/brick/field";
import { Input } from "@flowstack-ui/brick/input";
import { Grid } from "@flowstack-ui/brick/grid";
import { HStack, VStack } from "@flowstack-ui/brick/stack";
import { Heading, Paragraph, Text } from "@flowstack-ui/brick/text";
import {
  demoDeliveries,
  formatMoney,
  statusFilters,
  type DeliveryExample,
} from "../data/demo";
import { navigate, updateLocation } from "../navigation";
import { Glyph, RouteSummary } from "./ui";

const statusTone = {
  "Aguardando coleta": "warning",
  "Em rota": "info",
  Entregue: "success",
} as const;
type DetailsAction = (delivery: DeliveryExample, trigger: HTMLElement) => void;

export function DeliveryList({
  parameters,
  onDetails,
}: {
  parameters: URLSearchParams;
  onDetails: DetailsAction;
}) {
  const committed = parameters.get("q") ?? "";
  const [compositionText, setCompositionText] = useState<string | null>(null);
  const search = compositionText ?? committed;
  const input = useRef<HTMLInputElement>(null);
  const composing = useRef(false);
  const requestedStatus = parameters.get("status") ?? "Todas";
  const status =
    statusFilters.find((item) => item === requestedStatus) ?? "Todas";

  function commit(value: string) {
    const next = new URLSearchParams(parameters);
    if (value) next.set("q", value);
    else next.delete("q");
    // Motivo: filtros só pesquisam fixtures públicas, nunca endereço real digitado.
    // replace evita uma entrada de histórico por tecla. Mudança: 2026-09-10-01-interface-operacional.md.
    updateLocation(next, true);
  }
  function changeSearch(value: string) {
    if (composing.current) setCompositionText(value);
    else commit(value);
  }
  function startComposition() {
    composing.current = true;
    setCompositionText(committed);
  }
  function endComposition(event: CompositionEvent<HTMLInputElement>) {
    composing.current = false;
    setCompositionText(null);
    commit(event.currentTarget.value);
  }
  function clearSearch() {
    composing.current = false;
    setCompositionText(null);
    commit("");
    input.current?.focus();
  }
  function resetFilters() {
    updateLocation(new URLSearchParams({ view: "deliveries" }), true);
    input.current?.focus();
  }
  function filterHref(value: string) {
    const next = new URLSearchParams(parameters);
    next.set("view", "deliveries");
    if (value === "Todas") next.delete("status");
    else next.set("status", value);
    return `?${next.toString()}`;
  }
  const normalized = committed.trim().toLocaleLowerCase("pt-BR");
  const matches = demoDeliveries.filter(
    (delivery) =>
      (status === "Todas" || delivery.status === status) &&
      `${delivery.id} ${delivery.pickup} ${delivery.destination} ${delivery.district}`
        .toLocaleLowerCase("pt-BR")
        .includes(normalized),
  );

  return (
    <VStack gap={5}>
      <Field.Root>
        <Field.Label>Buscar nos exemplos</Field.Label>
        <Input
          ref={input}
          size="lg"
          value={search}
          onValueChange={changeSearch}
          onCompositionStart={startComposition}
          onCompositionEnd={endComposition}
          clearable
          clearLabel="Limpar busca"
          onClear={clearSearch}
          placeholder="Código, estabelecimento ou bairro"
          startAdornment={<Glyph name="search" />}
        />
        <Field.Description>
          Busca apenas nos seis registros fictícios desta demonstração.
        </Field.Description>
      </Field.Root>
      <HStack as="nav" aria-label="Filtrar entregas por status" gap={2} wrap>
        {statusFilters.map((value) => (
          <Button
            key={value}
            href={filterHref(value)}
            onClick={navigate}
            aria-current={status === value ? "true" : undefined}
            variant={status === value ? "solid" : "outline"}
            tone={status === value ? "accent" : "neutral"}
          >
            {value}
          </Button>
        ))}
      </HStack>
      <Text role="status" aria-live="polite" tone="secondary" variant="body-sm">
        {matches.length} de {demoDeliveries.length} exemplos
      </Text>
      {matches.length === 0 ? (
        <Card.Root size="lg">
          <Card.Header>
            <Card.Title>Nenhuma entrega encontrada</Card.Title>
            <Card.Description>
              Tente outro código, estabelecimento ou bairro, ou remova os
              filtros.
            </Card.Description>
          </Card.Header>
          <Card.Footer>
            <Button onClick={resetFilters} variant="outline" tone="neutral">
              Limpar filtros
            </Button>
          </Card.Footer>
        </Card.Root>
      ) : (
        <Grid.Root
          as="ul"
          aria-label="Entregas demonstrativas"
          columns={{ initial: 1, md: 2 }}
          gap={5}
        >
          {matches.map((delivery) => (
            <DeliveryCard
              key={delivery.id}
              delivery={delivery}
              onDetails={onDetails}
            />
          ))}
        </Grid.Root>
      )}
    </VStack>
  );
}

function DeliveryCard({
  delivery,
  onDetails,
}: {
  delivery: DeliveryExample;
  onDetails: DetailsAction;
}) {
  function open(event: MouseEvent<HTMLElement>) {
    onDetails(delivery, event.currentTarget);
  }
  return (
    <Card.Root as="li" size="lg">
      <Card.Header>
        <HStack justify="between" wrap>
          <Card.Title as="h2">#{delivery.id}</Card.Title>
          <Badge tone={statusTone[delivery.status]}>{delivery.status}</Badge>
        </HStack>
        <Card.Description>
          {delivery.district} · Dados fictícios
        </Card.Description>
      </Card.Header>
      <Card.Content>
        <VStack gap={5}>
          <RouteSummary
            pickup={delivery.pickup}
            destination={delivery.destination}
          />
          <HStack justify="between" wrap>
            <Text weight="semibold" className="numeric">
              {formatMoney(delivery.fee)} · taxa ilustrativa
            </Text>
            <Text tone="secondary">{delivery.distance} km</Text>
          </HStack>
        </VStack>
      </Card.Content>
      <Card.Footer>
        <Button
          onClick={open}
          variant="outline"
          tone="neutral"
          fullWidth
          aria-label={`Ver detalhes de ${delivery.id}`}
          endIcon={<Glyph name="arrow" raw />}
        >
          Ver detalhes
        </Button>
      </Card.Footer>
    </Card.Root>
  );
}

export function DeliveryDetails({
  delivery,
  onOpenChange,
  triggerRef,
}: {
  delivery: DeliveryExample | null;
  onOpenChange: (open: boolean) => void;
  triggerRef: RefObject<HTMLElement | null>;
}) {
  return (
    <Dialog.Root open={delivery !== null} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay />
        <Dialog.Content size="md" finalFocus={triggerRef}>
          <Dialog.Header>
            <Dialog.Title>Entrega {delivery?.id}</Dialog.Title>
            <Dialog.Description>
              Registro fictício para explorar a interface. Não representa uma
              entrega real.
            </Dialog.Description>
          </Dialog.Header>
          <Dialog.Body>
            {delivery && (
              <VStack gap={6}>
                <Badge tone={statusTone[delivery.status]}>
                  {delivery.status}
                </Badge>
                <RouteSummary
                  pickup={delivery.pickup}
                  destination={delivery.destination}
                />
                <Grid.Root columns={2} gap={5}>
                  <VStack gap={1}>
                    <Text tone="secondary" variant="body-sm">
                      Taxa ilustrativa
                    </Text>
                    <Text variant="title-md" className="numeric">
                      {formatMoney(delivery.fee)}
                    </Text>
                  </VStack>
                  <VStack gap={1}>
                    <Text tone="secondary" variant="body-sm">
                      Distância ilustrativa
                    </Text>
                    <Text variant="title-md">{delivery.distance} km</Text>
                  </VStack>
                </Grid.Root>
                <Paragraph>
                  {delivery.volumes} volume(s) · {delivery.district}
                </Paragraph>
                <Heading level={3} variant="title-sm">
                  O próximo passo será conectado
                </Heading>
                <Paragraph tone="secondary">
                  Acompanhamento, eventos e agrupamento serão disponibilizados
                  quando os serviços da API estiverem implementados.
                </Paragraph>
              </VStack>
            )}
          </Dialog.Body>
          <Dialog.Footer>
            <Dialog.Close asChild>
              <Button variant="outline" tone="neutral">
                Fechar detalhes
              </Button>
            </Dialog.Close>
          </Dialog.Footer>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
