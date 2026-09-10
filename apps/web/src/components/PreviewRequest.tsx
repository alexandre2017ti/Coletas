import { useRef, useState, type FormEvent } from "react";
import { Button } from "@flowstack-ui/brick/button";
import { Dialog } from "@flowstack-ui/brick/dialog";
import { Field } from "@flowstack-ui/brick/field";
import { Form } from "@flowstack-ui/brick/form";
import { Input } from "@flowstack-ui/brick/input";
import { VStack } from "@flowstack-ui/brick/stack";
import { Surface } from "@flowstack-ui/brick/surface";
import { Heading, Paragraph } from "@flowstack-ui/brick/text";
import { Glyph, RouteSummary } from "./ui";

export interface Draft {
  pickup: string;
  destination: string;
}
export function PreviewRequest({
  draft,
  onDraftChange,
}: {
  draft: Draft;
  onDraftChange: (draft: Draft) => void;
}) {
  const [open, setOpen] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [review, setReview] = useState(false);
  const pickup = useRef<HTMLInputElement>(null);
  const destination = useRef<HTMLInputElement>(null);
  const summary = useRef<HTMLElement>(null);
  const pickupInvalid = submitted && !draft.pickup.trim();
  const destinationInvalid = submitted && !draft.destination.trim();

  function updatePickup(value: string) {
    onDraftChange({ ...draft, pickup: value });
    setReview(false);
  }
  function updateDestination(value: string) {
    onDraftChange({ ...draft, destination: value });
    setReview(false);
  }
  function changeOpen(value: boolean) {
    setOpen(value);
    setReview(false);
    setSubmitted(false);
  }
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitted(true);
    if (!draft.pickup.trim()) {
      pickup.current?.focus();
      return;
    }
    if (!draft.destination.trim()) {
      destination.current?.focus();
      return;
    }
    // Motivo: ausência de API impede cotar ou confirmar; revisão é só local.
    // Mudança: docs/mudancas/2026-09-10-01-interface-operacional.md
    setReview(true);
  }
  function focusSummary(element: HTMLElement | null) {
    summary.current = element;
    element?.focus();
  }

  return (
    <Dialog.Root open={open} onOpenChange={changeOpen}>
      <Dialog.Trigger asChild>
        <Button size="lg" startIcon={<Glyph name="plus" raw />}>
          Prévia de solicitação
        </Button>
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Overlay />
        <Dialog.Content size="md" initialFocus={pickup}>
          <Dialog.Header>
            <Dialog.Title>Prévia de solicitação</Dialog.Title>
            <Dialog.Description>
              Use endereços de exemplo. Nada será enviado ao servidor ou salvo
              após recarregar a página.
            </Dialog.Description>
          </Dialog.Header>
          <Dialog.Body>
            <VStack gap={6}>
              <Form noValidate onSubmit={submit} validationBehavior="inline">
                <Field.Root required invalid={pickupInvalid}>
                  <Field.Label>Endereço de coleta</Field.Label>
                  <Input
                    ref={pickup}
                    name="pickup"
                    size="lg"
                    value={draft.pickup}
                    onValueChange={updatePickup}
                    autoComplete="off"
                    placeholder="Ex.: Rua das Flores, 120"
                  />
                  {pickupInvalid && (
                    <Field.Error>
                      Informe um endereço de exemplo para a coleta.
                    </Field.Error>
                  )}
                </Field.Root>
                <Field.Root required invalid={destinationInvalid}>
                  <Field.Label>Endereço de entrega</Field.Label>
                  <Input
                    ref={destination}
                    name="destination"
                    size="lg"
                    value={draft.destination}
                    onValueChange={updateDestination}
                    autoComplete="off"
                    placeholder="Ex.: Rua do Parque, 85"
                  />
                  {destinationInvalid && (
                    <Field.Error>
                      Informe um endereço de exemplo para a entrega.
                    </Field.Error>
                  )}
                </Field.Root>
                <Button type="submit" size="lg" fullWidth>
                  Revisar prévia
                </Button>
              </Form>
              {review && (
                <Surface level="subtle" inset="lg" radius="surface">
                  <VStack gap={4}>
                    <Heading
                      ref={focusSummary}
                      tabIndex={-1}
                      level={2}
                      variant="title-sm"
                    >
                      Prévia pronta — não enviada
                    </Heading>
                    <RouteSummary
                      pickup={draft.pickup}
                      destination={draft.destination}
                    />
                    <Paragraph variant="body-sm">
                      Taxa, distância, rota e prazo dependem da API. A
                      solicitação real e a inclusão de outros destinos ainda não
                      estão disponíveis.
                    </Paragraph>
                  </VStack>
                </Surface>
              )}
            </VStack>
          </Dialog.Body>
          <Dialog.Footer>
            <Dialog.Close asChild>
              <Button variant="outline" tone="neutral">
                Fechar prévia
              </Button>
            </Dialog.Close>
          </Dialog.Footer>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
