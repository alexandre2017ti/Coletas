import type { MouseEvent } from "react";
import { Badge } from "@flowstack-ui/brick/badge";
import { Button } from "@flowstack-ui/brick/button";
import { Card } from "@flowstack-ui/brick/card";
import { Frame } from "@flowstack-ui/brick/frame";
import { Grid } from "@flowstack-ui/brick/grid";
import { HStack, Stack, VStack } from "@flowstack-ui/brick/stack";
import { Surface } from "@flowstack-ui/brick/surface";
import { Heading, Paragraph, Text } from "@flowstack-ui/brick/text";
import { Glyph, Metric, RouteSummary } from "./ui";
import { RouteMap } from "./RouteMap";
import {
  demoDeliveries,
  formatMoney,
  type DeliveryExample,
} from "../data/demo";
import { navigate } from "../navigation";

export function Overview({
  onDetails,
}: {
  onDetails: (delivery: DeliveryExample, trigger: HTMLElement) => void;
}) {
  const next = demoDeliveries[0];
  function openNext(event: MouseEvent<HTMLElement>) {
    onDetails(next, event.currentTarget);
  }
  return (
    <VStack gap={6}>
      <Surface level="base" radius="surface" bordered inset="lg">
        <Grid.Root columns={{ initial: 1, md: 3 }} gap={6}>
          <Metric
            label="Aguardando coleta"
            value="02"
            detail="Exemplos prontos para retirada"
            icon="box"
          />
          <Metric
            label="Em rota"
            value="02"
            detail="Exemplos a caminho do destino"
            icon="route"
          />
          <Metric
            label="Entregues"
            value="02"
            detail="Exemplos de entregas concluídas"
            icon="check"
          />
        </Grid.Root>
      </Surface>
      <Grid.Root columns={{ initial: 1, md: 2, lg: 3 }} gap={5}>
        <Grid.Item columnSpan={{ initial: 1, md: 1, lg: 2 }}>
          <Card.Root size="lg">
            <Card.Header>
              <Card.Title>Seu percurso, em perspectiva</Card.Title>
              <Card.Description>
                Visualização ilustrativa · sem GPS ou mapa real
              </Card.Description>
            </Card.Header>
            <Card.Content>
              <RouteMap />
            </Card.Content>
            <Card.Footer>
              <HStack gap={4} wrap>
                <Badge tone="accent">A · Coleta</Badge>
                <Text variant="body-sm" tone="secondary">
                  B · Primeira entrega
                </Text>
                <Text variant="body-sm" tone="secondary">
                  C · Entrega adicional
                </Text>
              </HStack>
            </Card.Footer>
          </Card.Root>
        </Grid.Item>
        <Grid.Item>
          <Card.Root size="lg">
            <Card.Header>
              <Card.Description>PARA CONHECER O FLUXO</Card.Description>
              <Card.Title>Próxima coleta</Card.Title>
            </Card.Header>
            <Card.Content>
              <VStack gap={5}>
                <HStack justify="between" wrap>
                  <Text className="identifier">#{next.id}</Text>
                  <Badge tone="warning">{next.status}</Badge>
                </HStack>
                <RouteSummary
                  pickup={next.pickup}
                  destination={next.destination}
                />
                <HStack justify="between" align="end">
                  <VStack gap={1}>
                    <Text variant="body-sm" tone="secondary">
                      Taxa ilustrativa
                    </Text>
                    <Text variant="title-md" className="numeric">
                      {formatMoney(next.fee)}
                    </Text>
                  </VStack>
                  <Text tone="secondary">{next.distance} km</Text>
                </HStack>
              </VStack>
            </Card.Content>
            <Card.Footer>
              <Button
                onClick={openNext}
                variant="outline"
                tone="neutral"
                fullWidth
                endIcon={<Glyph name="arrow" raw />}
              >
                Ver detalhes
              </Button>
            </Card.Footer>
          </Card.Root>
        </Grid.Item>
      </Grid.Root>
      <Surface level="subtle" radius="surface" inset="lg">
        <Stack
          direction={{ initial: "column", md: "row" }}
          gap={5}
          align={{ initial: "start", md: "center" }}
          justify="between"
        >
          <HStack gap={4} align="start">
            <Glyph name="route" />
            <VStack gap={2}>
              <Heading level={2} variant="title-sm">
                Mais destinos, uma coleta
              </Heading>
              <Paragraph variant="body-sm">
                O agrupamento seguirá o raio configurado e as validações de rota
                da API.
              </Paragraph>
            </VStack>
          </HStack>
          <Button
            href="?view=deliveries"
            onClick={navigate}
            variant="outline"
            tone="accent"
            endIcon={<Glyph name="arrow" raw />}
          >
            Explorar entregas
          </Button>
        </Stack>
      </Surface>
    </VStack>
  );
}

export function CourierPreview() {
  const offer = demoDeliveries[0];
  return (
    <Grid.Root columns={{ initial: 1, md: 2 }} gap={6} align="start">
      <Card.Root size="lg">
        <Card.Header>
          <HStack justify="between" wrap>
            <Card.Title>Uma oferta, sem surpresas</Card.Title>
            <Badge tone="warning">Exemplo</Badge>
          </HStack>
          <Card.Description>
            Prévia da informação que o entregador receberá.
          </Card.Description>
        </Card.Header>
        <Card.Content>
          <VStack gap={6}>
            <Surface level="subtle" inset="lg" radius="surface">
              <VStack gap={2}>
                <Text variant="body-sm">Taxa da entrega · exemplo</Text>
                <Text variant="display-sm" className="numeric">
                  {formatMoney(offer.fee)}
                </Text>
                <Text variant="body-sm">
                  1,2 km até a coleta · {offer.distance} km de coleta a destino
                </Text>
              </VStack>
            </Surface>
            <RouteSummary
              pickup={offer.pickup}
              destination={offer.destination}
            />
            <RouteMap compact />
            <Button
              size="lg"
              disabled
              fullWidth
              aria-describedby="offer-disabled"
            >
              Aceitar entrega
            </Button>
            <Paragraph id="offer-disabled" variant="body-sm" tone="secondary">
              Indisponível nesta prévia. Ofertas reais exigem cadastro aprovado,
              localização autorizada e confirmação da API.
            </Paragraph>
          </VStack>
        </Card.Content>
      </Card.Root>
      <VStack gap={6}>
        <VStack gap={2}>
          <Heading level={2} variant="title-md">
            Pronto para rodar?
          </Heading>
          <Paragraph tone="secondary">
            Estes serão os pontos de verificação do aplicativo. Nenhum documento
            foi analisado nesta demonstração.
          </Paragraph>
        </VStack>
        <VStack as="ul" gap={5}>
          <Requirement
            title="Cadastro e WhatsApp"
            description="Identificação e telefone para contato."
            icon="phone"
          />
          <Requirement
            title="CNH e veículo"
            description="Documentos válidos e veículo sem pendências, sujeitos à aprovação."
            icon="shield"
          />
          <Requirement
            title="Licença semanal"
            description="Situação e histórico da licença exibidos após integração de pagamentos."
            icon="calendar"
          />
          <Requirement
            title="Capacitação"
            description="Participação em palestras conforme periodicidade configurada."
            icon="training"
          />
        </VStack>
        <Frame maxInlineSize="34rem">
          <Paragraph variant="body-sm" tone="secondary">
            Esta é uma prévia web da experiência. A adaptação ao aplicativo
            Android/iOS e os testes em aparelhos ainda serão realizados.
          </Paragraph>
        </Frame>
      </VStack>
    </Grid.Root>
  );
}

function Requirement({
  title,
  description,
  icon,
}: {
  title: string;
  description: string;
  icon: string;
}) {
  return (
    <HStack as="li" align="start" gap={4}>
      <Badge size="lg" shape="circle" tone="neutral">
        <Glyph name={icon} />
      </Badge>
      <VStack gap={1}>
        <Text weight="semibold">{title}</Text>
        <Paragraph variant="body-sm" tone="secondary">
          {description}
        </Paragraph>
        <Text variant="body-sm" tone="secondary">
          Ainda não verificado
        </Text>
      </VStack>
    </HStack>
  );
}
