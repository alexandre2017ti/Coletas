import { Component, type ReactNode } from "react";
import { Button } from "@flowstack-ui/brick/button";
import { Container } from "@flowstack-ui/brick/container";
import { Section } from "@flowstack-ui/brick/section";
import { VStack } from "@flowstack-ui/brick/stack";
import { Heading, Paragraph } from "@flowstack-ui/brick/text";

export class AppErrorBoundary extends Component<
  { children: ReactNode },
  { failed: boolean }
> {
  state = { failed: false };
  static getDerivedStateFromError() {
    return { failed: true };
  }
  componentDidCatch() {
    // Motivo: nunca expor erro bruto ou conteúdo de formulário na tela/log.
    // Mudança: docs/mudancas/2026-09-10-01-interface-operacional.md
    document.title = "Falha ao exibir a prévia — Coletas";
  }
  render() {
    if (!this.state.failed) return this.props.children;
    return (
      <Container as="main" measure="narrow">
        <Section>
          <VStack gap={5}>
            <Heading level={1}>Não foi possível exibir a prévia</Heading>
            <Paragraph>
              Reabra a visão geral para tentar novamente. Campos temporários
              poderão ser perdidos.
            </Paragraph>
            <Button href="?view=overview">Reabrir visão geral</Button>
          </VStack>
        </Section>
      </Container>
    );
  }
}
