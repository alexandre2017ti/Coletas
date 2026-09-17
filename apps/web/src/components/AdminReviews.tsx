import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Button } from "@flowstack-ui/brick/button";
import { Form } from "@flowstack-ui/brick/form";
import { RadioGroup } from "@flowstack-ui/brick/radio-group";
import { AlertDialog } from "@flowstack-ui/brick/alert-dialog";
import { Surface } from "@flowstack-ui/brick/surface";
import { VStack, HStack } from "@flowstack-ui/brick/stack";
import { Heading, Paragraph, Text } from "@flowstack-ui/brick/text";
import { api, jsonBody, useSession, type Profile } from "../api/client";
import { navigate } from "../navigation";
import { AccountInput, AccountFeedback } from "./AccountControls";
import { useAccountAction } from "../api/useAccountAction";
import { DocumentList, RegistrationSummary } from "./AccountPage";

type Review = { userId: string; role: string; accountStatus: string; reviewStatus: string; version: number };
type ReviewDetail = { review: Review; history: Array<{ id: string; action: string; reason: string; internalNote: string | null; createdAt: string; version: number }> };
type Page = { items: Review[]; total: number; page: number; pageSize: number };
const labels: Record<string, string> = { Pending: "Pendente", InReview: "Em análise", NeedsCorrection: "Correção solicitada", Approved: "Aprovado", Rejected: "Rejeitado", Start: "Iniciar análise", RequestCorrection: "Solicitar correção", Approve: "Aprovar cadastro", Reject: "Rejeitar cadastro", Block: "Bloquear acesso", Unblock: "Desbloquear acesso", Courier: "Entregador", Establishment: "Estabelecimento" };

function ReviewEditor({ userId, onBack }: { userId: string; onBack: () => void }) {
  const action = useAccountAction();
  const [detail, setDetail] = useState<ReviewDetail | null>(null);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [reason, setReason] = useState("");
  const [note, setNote] = useState("");
  const [pending, setPending] = useState<{ action: string; documentId?: string } | null>(null);
  const [fieldError, setFieldError] = useState("");
  const reload = useCallback(async (signal?: AbortSignal) => {
    const [review, account] = await Promise.all([
      api<ReviewDetail>(`/admin/reviews/${userId}`, { signal }),
      api<Profile>(`/admin/users/${userId}/profile`, { signal }),
    ]);
    if (!signal?.aborted) { setDetail(review); setProfile(account); }
  }, [userId]);
  const { run, cancel } = action;
  useEffect(() => { void run(reload); return cancel; }, [run, cancel, reload]);
  function propose(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const button = (event.nativeEvent as SubmitEvent).submitter as HTMLButtonElement | null;
    if (!reason.trim()) { setFieldError("Informe um motivo público."); event.currentTarget.querySelector<HTMLInputElement>('[name="reason"]')?.focus(); return; }
    setFieldError("");
    setPending({ action: button?.value ?? "Start", documentId: button?.dataset.document });
  }
  async function decide() {
    if (!pending || !detail) return;
    await action.run(async signal => {
      const path = pending.documentId ? `/admin/documents/${pending.documentId}/decision` : `/admin/reviews/${userId}/decisions`;
      const body = pending.documentId ? { status: pending.action, reason, expectedVersion: detail.review.version }
        : { action: pending.action, reason, internalNote: note, expectedVersion: detail.review.version };
      await api(path, { method: "POST", body: jsonBody(body), signal });
      if (signal.aborted) return;
      setPending(null); setReason(""); setNote("");
      await reload(signal);
    });
  }
  const review = detail?.review;
  const actions = review?.accountStatus === "Blocked" ? ["Unblock"] : [
    ...(review && ["Pending", "NeedsCorrection"].includes(review.reviewStatus) ? ["Start"] : []),
    ...(review?.reviewStatus === "InReview" ? ["Approve", "RequestCorrection", "Reject"] : []), "Block",
  ];
  return <VStack gap={5}>
    <HStack gap={3} wrap><Button variant="outline" onClick={onBack} disabled={action.busy}>Voltar à fila</Button><Button onClick={() => void action.run(reload)} disabled={action.busy}>Recarregar cadastro</Button></HStack>
    <AccountFeedback error={action.error} message={action.busy ? "Carregando…" : ""} />
    {profile && detail && <>
      <Heading level={2}>Conferência do cadastro</Heading><RegistrationSummary profile={profile} />
      <Paragraph>Análise: {labels[detail.review.reviewStatus]} · Versão {detail.review.version}</Paragraph>
      {profile.vehicles.map(vehicle => <Paragraph key={vehicle.id}>Veículo: {vehicle.type === "Car" ? "Carro" : "Motocicleta"} · {vehicle.plate}</Paragraph>)}
      <Heading level={3}>Documentos privados</Heading><DocumentList profile={profile} />
      <Form noValidate onSubmit={propose}>
        <AccountInput name="reason" label="Motivo público" value={reason} onChange={e => setReason(e.target.value)} maxLength={500} placeholder="Explique ao titular o resultado ou a correção necessária" required error={fieldError} disabled={action.busy} />
        <AccountInput name="internalNote" label="Observação interna (opcional)" value={note} onChange={e => setNote(e.target.value)} maxLength={1000} placeholder="Visível somente para administradores" disabled={action.busy} />
        <HStack gap={3} wrap>{actions.map(value => <Button key={value} type="submit" value={value} tone={value === "Block" || value === "Reject" ? "danger" : "accent"} disabled={action.busy}>{labels[value]}</Button>)}</HStack>
        {profile.documents.map(doc => <HStack key={doc.id} gap={3} wrap>
          <Text>{doc.type === "DriverLicense" ? "CNH" : "Documento do veículo"} — {doc.status}</Text>
          <Button type="submit" variant="outline" value="Approved" data-document={doc.id} disabled={action.busy || !doc.hasFile}>Aprovar documento</Button>
          <Button type="submit" variant="outline" tone="danger" value="Rejected" data-document={doc.id} disabled={action.busy}>Reprovar documento</Button>
        </HStack>)}
      </Form>
      <Heading level={3}>Histórico</Heading>
      {!detail.history.length && <Paragraph>Nenhuma decisão registrada.</Paragraph>}
      {detail.history.map(item => <Surface key={item.id} bordered inset="md"><VStack gap={2}>
        <Text>{labels[item.action] ?? item.action} · Versão {item.version}</Text>
        <Text>{new Date(item.createdAt).toLocaleString("pt-BR")} (horário deste dispositivo)</Text>
        <Paragraph>{item.reason}</Paragraph>{item.internalNote && <Paragraph>Interno: {item.internalNote}</Paragraph>}
      </VStack></Surface>)}
    </>}
    <AlertDialog.Root open={!!pending} onOpenChange={open => { if (!open && !action.busy) setPending(null); }}>
      <AlertDialog.Portal><AlertDialog.Overlay /><AlertDialog.Content>
        <AlertDialog.Header><AlertDialog.Title>Confirmar decisão</AlertDialog.Title><AlertDialog.Description>
          {pending?.documentId ? "A decisão documental exige nova análise do cadastro." : labels[pending?.action ?? ""]} O motivo será visível ao titular. A decisão ficará no histórico e poderá revogar sessões.
        </AlertDialog.Description></AlertDialog.Header>
        <AlertDialog.Body><Paragraph>{reason}</Paragraph><AccountFeedback error={action.error} /></AlertDialog.Body>
        <AlertDialog.Footer><AlertDialog.Cancel asChild><Button variant="outline" disabled={action.busy}>Cancelar</Button></AlertDialog.Cancel>
          <AlertDialog.Action asChild><Button disabled={action.busy} onClick={event => { event.preventDefault(); void decide(); }}>Confirmar decisão</Button></AlertDialog.Action>
        </AlertDialog.Footer>
      </AlertDialog.Content></AlertDialog.Portal>
    </AlertDialog.Root>
  </VStack>;
}

export function AdminReviews() {
  const { profile } = useSession();
  const action = useAccountAction();
  const [status, setStatus] = useState("Pending");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<Page | null>(null);
  const [selected, setSelected] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);
  const { run, cancel } = action;
  useEffect(() => {
    if (profile?.role !== "Admin") return;
    void run(async signal => {
      const result = await api<Page>(`/admin/reviews?page=${page}&pageSize=20${status ? `&status=${status}` : ""}`, { signal });
      if (!signal.aborted) setData(result);
    });
    return cancel;
  }, [profile, page, status, revision, run, cancel]);
  if (!profile) return <Button href="?view=login" onClick={navigate}>Entrar para acessar a administração</Button>;
  if (profile.role !== "Admin") return <Paragraph role="alert">Acesso restrito a administradores.</Paragraph>;
  return <Surface level="base" bordered inset="lg" radius="surface">
    {selected ? <ReviewEditor key={selected} userId={selected} onBack={() => { setSelected(null); setRevision(revision + 1); }} /> : <VStack gap={5}>
      <Paragraph>Confira os dados e documentos antes de liberar o cadastro. Aprovação cadastral não comprova licença ou capacitação.</Paragraph>
      <RadioGroup.Root aria-label="Filtrar análise" value={status} onValueChange={value => { setData(null); setStatus(value); setPage(1); }} disabled={action.busy}>
        {["Pending", "InReview", "NeedsCorrection", "Approved", "Rejected", ""].map(value => <RadioGroup.Item key={value} value={value}>{labels[value] ?? "Todos"}</RadioGroup.Item>)}
      </RadioGroup.Root>
      <AccountFeedback error={action.error} message={action.busy ? "Carregando fila…" : data ? `${data.total} cadastros encontrados` : ""} />
      <Button variant="outline" onClick={() => setRevision(revision + 1)} disabled={action.busy}>Atualizar fila</Button>
      {data?.items.map(item => <Surface key={item.userId} bordered inset="md"><VStack gap={3}>
        <Text>{labels[item.role]} · {labels[item.reviewStatus]}{item.accountStatus === "Blocked" ? " · Acesso bloqueado" : ""}</Text>
        <Text variant="body-sm">Cadastro {item.userId}</Text>
        <Button onClick={() => setSelected(item.userId)}>Analisar cadastro</Button>
      </VStack></Surface>)}
      {data && !data.items.length && <Paragraph>Nenhum cadastro nesta página. Altere o filtro ou volte à página anterior.</Paragraph>}
      <HStack gap={3} wrap><Button variant="outline" disabled={action.busy || page === 1} onClick={() => setPage(page - 1)}>Página anterior</Button>
        <Text>Página {page}</Text><Button variant="outline" disabled={action.busy || !data || page * data.pageSize >= data.total} onClick={() => setPage(page + 1)}>Próxima página</Button></HStack>
    </VStack>}
  </Surface>;
}
