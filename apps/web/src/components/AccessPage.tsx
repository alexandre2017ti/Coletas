import { useEffect, useState, type FormEvent } from "react";
import { Form } from "@flowstack-ui/brick/form";
import { Button } from "@flowstack-ui/brick/button";
import { Surface } from "@flowstack-ui/brick/surface";
import { VStack, HStack } from "@flowstack-ui/brick/stack";
import { Paragraph } from "@flowstack-ui/brick/text";
import { api, jsonBody, signIn } from "../api/client";
import { navigate, updateLocation } from "../navigation";
import { AccountInput, AccountFeedback } from "./AccountControls";
import { useAccountAction } from "../api/useAccountAction";

export function AccessPage({ mode }: { mode: "login" | "recovery" | "reset-password" }) {
  const action = useAccountAction();
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState("");
  const [token] = useState(() => {
    if (mode !== "reset-password") return "";
    const value = new URLSearchParams(location.hash.slice(1)).get("token") ?? "";
    // Retirar imediatamente o segredo do histórico; nunca persistir nem enviar em URL.
    // Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
    return value;
  });
  useEffect(() => { if (mode === "reset-password") history.replaceState(null, "", location.pathname + location.search); }, [mode]);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const email = String(data.get("email") ?? "").trim();
    const password = String(data.get("password") ?? "");
    const next: Record<string, string> = {};
    if (mode !== "reset-password" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) next.email = "Informe um e-mail válido.";
    if (mode !== "recovery" && !password) next.password = "Informe a senha.";
    if (mode === "reset-password" && (password.length < 12 || new TextEncoder().encode(password).length > 72)) next.password = "Use pelo menos 12 caracteres e no máximo 72 bytes.";
    if (mode === "reset-password" && password !== data.get("confirmation")) next.confirmation = "As senhas precisam ser iguais.";
    setErrors(next); setMessage("");
    if (Object.keys(next).length) { (form.elements.namedItem(Object.keys(next)[0]) as HTMLElement)?.focus(); return; }
    await action.run(async signal => {
      if (mode === "login") {
        await signIn(email, password, signal);
        if (!signal.aborted) updateLocation(new URLSearchParams({ view: "account" }));
      } else if (mode === "recovery") {
        await api("/auth/recovery", { method: "POST", body: jsonBody({ email }), signal }, false);
        if (!signal.aborted) setMessage("Se houver uma conta elegível, você receberá as instruções.");
      } else {
        await api("/auth/reset-password", { method: "POST", body: jsonBody({ token, password }), signal }, false);
        if (!signal.aborted) { form.reset(); setMessage("Senha redefinida. Entre com a nova senha."); }
      }
    });
  }
  return <Surface level="base" bordered radius="surface" inset="lg"><VStack gap={5}>
    <Paragraph>{mode === "login" ? "Acesse sua conta para acompanhar o cadastro e os documentos." : mode === "recovery" ? "Informe o e-mail cadastrado. O envio depende da configuração do serviço de e-mail." : "Crie uma nova senha com pelo menos 12 caracteres. O código do link é de uso único."}</Paragraph>
    <Form noValidate validationBehavior="inline" onSubmit={submit} aria-busy={action.busy}>
      {mode !== "reset-password" && <AccountInput label="E-mail" name="email" type="email" autoComplete="email" placeholder="voce@exemplo.com.br" maxLength={254} required disabled={action.busy} error={errors.email} />}
      {mode !== "recovery" && <AccountInput label="Senha" name="password" secret autoComplete={mode === "login" ? "current-password" : "new-password"} placeholder="Digite sua senha" maxLength={128} required disabled={action.busy} error={errors.password} />}
      {mode === "reset-password" && <AccountInput label="Confirmar senha" name="confirmation" secret autoComplete="new-password" placeholder="Repita a nova senha" maxLength={128} required disabled={action.busy} error={errors.confirmation} />}
      <Button size="lg" type="submit" disabled={action.busy || (mode === "reset-password" && !token)} aria-busy={action.busy}>{mode === "login" ? "Entrar" : mode === "recovery" ? "Solicitar recuperação" : "Redefinir senha"}</Button>
      <AccountFeedback error={action.error || (mode === "reset-password" && !token ? "Abra o link recebido por e-mail para redefinir a senha." : "")} message={action.busy ? "Aguarde…" : message} />
    </Form>
    <HStack gap={3} wrap>
      <Button href={mode === "login" ? "?view=recovery" : "?view=login"} onClick={navigate} variant="outline">{mode === "login" ? "Esqueci minha senha" : "Voltar ao login"}</Button>
    </HStack>
  </VStack></Surface>;
}
