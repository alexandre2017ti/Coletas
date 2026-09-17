import { useEffect, useState, type FormEvent } from "react";
import { Button } from "@flowstack-ui/brick/button";
import { Form } from "@flowstack-ui/brick/form";
import { RadioGroup } from "@flowstack-ui/brick/radio-group";
import { Field } from "@flowstack-ui/brick/field";
import { Surface } from "@flowstack-ui/brick/surface";
import { VStack, HStack } from "@flowstack-ui/brick/stack";
import { Heading, Paragraph, Text } from "@flowstack-ui/brick/text";
import { api, apiResponse, jsonBody, loadProfile, registrationChanged, signOut, useSession, type Profile } from "../api/client";
import { navigate } from "../navigation";
import { formatPlate, validPlate, plateValue } from "../registrationValidation";
import { AccountFeedback, AccountInput } from "./AccountControls";
import { useAccountAction } from "../api/useAccountAction";

const accountLabels: Record<string, string> = { Pending: "Aguardando análise", Active: "Conta aprovada", Blocked: "Conta bloqueada", UnderReview: "Em análise", Approved: "Aprovado", Rejected: "Reprovado", Expired: "Vencido", DriverLicense: "CNH", VehicleRegistration: "Documento do veículo", Motorcycle: "Motocicleta", Car: "Carro" };

export function RegistrationSummary({ profile }: { profile: Profile }) {
  const data = profile.registration;
  return <VStack gap={2}><Paragraph>{profile.email}</Paragraph>
    {data && <><Text weight="semibold">{data.name}</Text>{data.tradeName && <Text>{data.tradeName}</Text>}<Text>CPF/CNPJ: {data.taxId ?? "Não informado"}</Text><Text>WhatsApp: {data.phoneWhatsApp}</Text></>}
    <Paragraph>{accountLabels[profile.status] ?? profile.status}</Paragraph>
    {profile.reason && <Paragraph>{profile.reason}</Paragraph>}
  </VStack>;
}

export function DocumentList({ profile }: { profile: Profile }) {
  const action = useAccountAction();
  async function download(id: string) {
    await action.run(async signal => {
      const response = await apiResponse(`/couriers/${profile.courierId}/documents/${id}/file`, { signal });
      const blob = await response.blob();
      if (signal.aborted) return;
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url; link.download = "documento";
      link.click();
      // Download autorizado vira URL temporária local, nunca link público com Bearer.
      // Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    });
  }
  return <VStack gap={4}>
    {!profile.documents.length && <Paragraph>Nenhum documento enviado.</Paragraph>}
    {profile.documents.map(doc => <Surface key={doc.id} bordered inset="md" radius="subtle"><VStack gap={2}>
      <Text weight="semibold">{accountLabels[doc.type] ?? doc.type}</Text>
      <Text>{accountLabels[doc.status] ?? doc.status}</Text>
      <Text>Validade: {doc.expiresAt ? new Date(doc.expiresAt).toLocaleDateString("pt-BR", { timeZone: "UTC" }) : "Não informada"}</Text>
      {doc.reason && <Paragraph>{doc.reason}</Paragraph>}
      <Button variant="outline" disabled={!doc.hasFile || action.busy} onClick={() => void download(doc.id)}>Baixar documento</Button>
    </VStack></Surface>)}
    <AccountFeedback error={action.error} />
  </VStack>;
}

function CourierChanges({ profile }: { profile: Profile }) {
  const action = useAccountAction();
  const vehicle = profile.vehicles[0];
  const [plate, setPlate] = useState(vehicle ? formatPlate(vehicle.plate) : "");
  const [vehicleType, setVehicleType] = useState(vehicle?.type ?? "Motorcycle");
  const [documentType, setDocumentType] = useState("DriverLicense");
  const [error, setError] = useState("");
  async function saveVehicle(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError("");
    if (!validPlate(plate)) { setError("Use ABC-1234 ou ABC1D23."); event.currentTarget.querySelector<HTMLInputElement>('[name="plate"]')?.focus(); return; }
    await action.run(async signal => {
      await api(`/couriers/${profile.courierId}/vehicles/${vehicle.id}`, { method: "PUT", body: jsonBody({ type: vehicleType, plate: plateValue(plate) }), signal });
      if (!signal.aborted) registrationChanged();
    });
  }
  async function upload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError("");
    const data = new FormData(event.currentTarget);
    const file = data.get("file");
    if (!(file instanceof File) || !file.size) { setError("Selecione um arquivo."); return; }
    const expiry = String(data.get("expiresAt") ?? "");
    if (!/^\d{4}-\d{2}-\d{2}$/.test(expiry) || !Number.isFinite(Date.parse(expiry)) || Date.parse(expiry + "T23:59:59Z") <= Date.now()) { setError("Informe validade futura no formato AAAA-MM-DD."); return; }
    data.set("expiresAt", expiry + "T23:59:59Z");
    data.set("type", documentType);
    await action.run(async signal => {
      await api(`/couriers/${profile.courierId}/documents/upload`, { method: "POST", body: data, signal });
      if (!signal.aborted) registrationChanged();
    });
  }
  return <VStack gap={6}>
    <Paragraph>Alterar veículo ou enviar documento revoga sua sessão e exige nova análise. Você precisará entrar novamente.</Paragraph>
    {vehicle && <Form noValidate onSubmit={saveVehicle}>
      <Heading level={3}>Veículo</Heading>
      <RadioGroup.Root aria-label="Tipo de veículo" value={vehicleType} onValueChange={setVehicleType} disabled={action.busy}>
        <RadioGroup.Item value="Motorcycle">Motocicleta</RadioGroup.Item><RadioGroup.Item value="Car">Carro</RadioGroup.Item>
      </RadioGroup.Root>
      <AccountInput label="Placa" name="plate" value={plate} onChange={e => setPlate(formatPlate(e.target.value))} maxLength={8} placeholder="ABC-1234 ou ABC1D23" error={error.includes("ABC") ? error : ""} disabled={action.busy} required />
      <Button type="submit" disabled={action.busy}>Salvar veículo e solicitar nova análise</Button>
    </Form>}
    <Form noValidate onSubmit={upload}>
      <Heading level={3}>Enviar documento</Heading>
      <RadioGroup.Root aria-label="Tipo de documento" value={documentType} onValueChange={setDocumentType} disabled={action.busy}>
        <RadioGroup.Item value="DriverLicense">CNH</RadioGroup.Item><RadioGroup.Item value="VehicleRegistration">Documento do veículo</RadioGroup.Item>
      </RadioGroup.Root>
      <AccountInput label="Validade (AAAA-MM-DD)" name="expiresAt" placeholder="2027-12-31" maxLength={10} required disabled={action.busy} />
      <Field.Root required disabled={action.busy}><Field.Label htmlFor="document-file">Arquivo PDF, JPEG ou PNG</Field.Label>
        {/* Picker nativo permitido pelo guia FileUpload: um arquivo, sem drop/preview/coleção. */}
        <input id="document-file" name="file" type="file" accept=".pdf,.jpg,.jpeg,.png" required disabled={action.busy} />
        <Field.Description>Arquivo privado. O servidor confere o formato e o limite configurado.</Field.Description>
      </Field.Root>
      <Button type="submit" disabled={action.busy}>Enviar documento e solicitar nova análise</Button>
    </Form>
    <AccountFeedback error={error || action.error} message={action.busy ? "Aguarde…" : ""} />
  </VStack>;
}

export function AccountPage() {
  const { profile, error } = useSession();
  const action = useAccountAction();
  const [history, setHistory] = useState<Array<{ id: string; reason: string }>>([]);
  useEffect(() => {
    if (!profile) return;
    const controller = new AbortController();
    void api<{ history: Array<{ id: string; reason: string }> }>("/account/review", { signal: controller.signal })
      .then(data => { if (!controller.signal.aborted) setHistory(data.history); }).catch(() => {});
    return () => controller.abort();
  }, [profile]);
  if (!profile) return <VStack gap={4}><AccountFeedback error={error} message="Entre para acessar sua conta." /><Button href="?view=login" onClick={navigate}>Ir para login</Button></VStack>;
  return <Surface level="base" bordered inset="lg" radius="surface"><VStack gap={6}>
    <RegistrationSummary profile={profile} />
    <HStack gap={3} wrap><Button variant="outline" onClick={() => void action.run(async signal => { await loadProfile(signal); })} disabled={action.busy}>Atualizar situação</Button>
      <Button variant="outline" onClick={() => void action.run(async () => { await signOut(); })} disabled={action.busy}>Sair da conta</Button>
      {profile.role === "Admin" && <Button href="?view=admin-reviews" onClick={navigate}>Analisar cadastros</Button>}
    </HStack>
    <AccountFeedback error={action.error} />
    {history.map(item => <Paragraph key={item.id}>{item.reason}</Paragraph>)}
    {profile.courierId && <><Heading level={2}>Meus documentos</Heading><DocumentList profile={profile} /><CourierChanges profile={profile} /></>}
  </VStack></Surface>;
}
