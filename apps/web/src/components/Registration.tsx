import { useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import { api, jsonBody } from "../api/client";
import { cpfValue, formatCpf, validCpf } from "../registrationValidation";
import { cnpjValue, formatCnpj, formatPhone, formatPlate, phoneDigits, plateValue, validCnpj, validPhone, validPlate } from "../registrationValidation";
import "../App.css";

type RegistrationKind = "establishment" | "courier";

export function Registration({ kind }: { kind: RegistrationKind }) {
  const courier = kind === "courier";
  const [phone, setPhone] = useState("");
  const [plate, setPlate] = useState("");
  const [cpf, setCpf] = useState("");
  const [cnpj, setCnpj] = useState("");
  const [legalName, setLegalName] = useState("");
  const [tradeName, setTradeName] = useState("");
  const [message, setMessage] = useState("");
  const [lookupMessage, setLookupMessage] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [sending, setSending] = useState(false);
  const [looking, setLooking] = useState(false);
  const lookup = useRef<AbortController | null>(null);
  const submission = useRef<AbortController | null>(null);
  const revision = useRef(0);
  const formRef = useRef<HTMLFormElement>(null);

  useEffect(() => () => { revision.current++; lookup.current?.abort(); submission.current?.abort(); }, []);

  function updateCnpj(value: string) {
    revision.current++; lookup.current?.abort(); setLooking(false);
    setCnpj(formatCnpj(value)); setLookupMessage("");
  }

  async function findCompany() {
    const id = cnpjValue(cnpj);
    if (!validCnpj(id)) { setLookupMessage("Confira o CNPJ e seus dígitos verificadores."); return; }
    if (!/^[0-9]{14}$/.test(id)) {
      setLookupMessage("A consulta aceita apenas CNPJ numérico. Preencha os dados manualmente para CNPJ alfanumérico."); return;
    }
    lookup.current?.abort();
    const controller = new AbortController();
    lookup.current = controller;
    const current = ++revision.current;
    setLooking(true); setLookupMessage("Consultando CNPJ…");
    const timeout = window.setTimeout(() => controller.abort(), 10000);
    try {
      // Motivo: consulta explícita envia somente CNPJ público; edição invalida respostas atrasadas.
      // Mudança: docs/mudancas/2026-09-11-01-validacao-cadastros.md
      const response = await fetch(`https://brasilapi.com.br/api/cnpj/v1/${id}`, { signal: controller.signal });
      if (!response.ok) throw new Error(response.status === 404 ? "CNPJ não encontrado. Confira o número ou preencha manualmente." : "Consulta indisponível. Tente novamente ou preencha manualmente.");
      const data = await response.json();
      if (revision.current !== current) return;
      if (cnpjValue(String(data.cnpj ?? "")) !== id || typeof data.razao_social !== "string" || !data.razao_social.trim()) throw new Error("A consulta retornou dados incompletos. Preencha manualmente.");
      setLegalName(data.razao_social.slice(0, 200));
      setTradeName(typeof data.nome_fantasia === "string" ? data.nome_fantasia.slice(0, 120) : "");
      setLookupMessage("Dados encontrados. Confira a razão social e o nome comercial antes de cadastrar.");
    } catch (error) {
      if (revision.current === current) setLookupMessage(error instanceof Error && error.name !== "AbortError" && !(error instanceof TypeError) ? error.message : "Não foi possível consultar agora. Tente novamente ou preencha manualmente.");
    } finally {
      window.clearTimeout(timeout);
      if (revision.current === current) setLooking(false);
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submission.current) return;
    const form = event.currentTarget;
    const payload: Record<string, string> = Object.fromEntries(new FormData(form)) as Record<string, string>;
    const next: Record<string, string> = {};
    for (const element of Array.from(form.elements)) {
      if (element instanceof HTMLInputElement && element.required && !element.value.trim()) next[element.name] = "Preencha este campo.";
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(payload.email ?? "")) next.email = "Informe um e-mail válido.";
    if ((payload.password ?? "").length < 12 || (payload.password ?? "").length > 128) next.password = "Use de 12 a 128 caracteres.";
    if (!validPhone(phone)) next.phoneWhatsApp = "Informe DDD e um telefone fixo de 8 ou celular de 9 dígitos.";
    if (courier && !validPlate(plate)) next.plate = "Use ABC-1234 (antiga) ou ABC1D23 (Mercosul).";
    if (courier && !validCpf(cpf)) next.cpf = "Informe um CPF com dígitos verificadores válidos.";
    if (!courier && !validCnpj(cnpj)) next.taxId = "Informe um CNPJ com dígitos verificadores válidos.";
    setErrors(next); setMessage("");
    if (Object.keys(next).length) {
      const element = form.elements.namedItem(Object.keys(next)[0]); if (element instanceof HTMLElement) element.focus(); return;
    }
    payload.phoneWhatsApp = phoneDigits(phone);
    if (courier) payload.cpf = cpfValue(cpf);
    if (courier) payload.plate = plateValue(plate); else payload.taxId = cnpjValue(cnpj);
    setSending(true);
    // Trava síncrona e cancelamento evitam envio duplicado/resposta em tela desmontada.
    // Motivo: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md
    const controller = new AbortController();
    submission.current = controller;
    try {
      await api(`/auth/register/${courier ? "couriers" : "establishments"}`, {
        method: "POST", body: jsonBody(payload), signal: controller.signal,
      }, false);
      if (controller.signal.aborted) return;
      form.reset(); setPhone(""); setPlate(""); setCnpj(""); setLegalName(""); setTradeName(""); setLookupMessage("");
      setCpf("");
      setMessage("Cadastro enviado. Aguarde a análise da equipe.");
    } catch (error) {
      if (controller.signal.aborted) return;
      setErrors({ form: error instanceof TypeError ? "Sem conexão com o servidor. Seus dados foram mantidos; tente novamente." : error instanceof Error ? error.message : "Não foi possível cadastrar." });
    } finally {
      submission.current = null;
      if (!controller.signal.aborted) setSending(false);
    }
  }

  function errorFor(name: string) { return errors[name] ? <span className="field-error" id={`error-${name}`}>{errors[name]}</span> : null; }
  function accessibility(name: string) {
    // O nome do campo permanece estável quando ajuda e erros são exibidos no label.
    // Mudança: docs/mudancas/2026-09-14-01-aceite-formularios.md
    const labels: Record<string, string> = { fullName: "Nome completo", phoneWhatsApp: "WhatsApp", taxId: "CNPJ", legalName: "Razão social", tradeName: "Nome comercial", email: "E-mail", password: "Senha", plate: "Placa" };
    return { "aria-label": labels[name], "aria-invalid": !!errors[name], "aria-describedby": errors[name] ? `error-${name}` : undefined };
  }

  return (
    <section className="registration-card" aria-labelledby="registration-title">
      <p className="eyebrow">{courier ? "Para quem entrega" : "Para quem envia"}</p>
      <h2 id="registration-title">{courier ? "Cadastre-se como entregador" : "Cadastre seu estabelecimento"}</h2>
      <p className="registration-intro">Preencha os dados para análise. Todos os campos são obrigatórios.</p>
      <form ref={formRef} onSubmit={submit} className="registration-form" noValidate>
        <fieldset disabled={sending} className="registration-fields">
          {courier && <label>CPF<input name="cpf" aria-label="CPF" required inputMode="numeric" value={cpf} onChange={e => setCpf(formatCpf(e.target.value))} maxLength={14} placeholder="000.000.000-00" aria-invalid={!!errors.cpf} aria-describedby={errors.cpf ? "error-cpf" : undefined} />{errorFor("cpf")}</label>}
          {!courier && <div className="cnpj-lookup">
            <label>CNPJ<input name="taxId" required value={cnpj} onChange={e => updateCnpj(e.target.value)} maxLength={18} placeholder="00.000.000/0001-00" autoCapitalize="characters" {...accessibility("taxId")} />{errorFor("taxId")}</label>
            <button type="button" disabled={looking || !validCnpj(cnpj)} onClick={findCompany}>{looking ? "Consultando…" : "Buscar CNPJ"}</button>
            <p className="field-help">Consulta pública pela BrasilAPI. Confira os dados retornados; você pode preenchê-los manualmente.</p>
            <p role="status">{lookupMessage}</p>
          </div>}
          {courier ? <label>Nome completo<input name="fullName" required maxLength={200} placeholder="Seu nome completo" autoComplete="name" {...accessibility("fullName")} />{errorFor("fullName")}</label> : <>
            <label>Razão social<input name="legalName" required maxLength={200} value={legalName} onChange={e => setLegalName(e.target.value)} disabled={looking} placeholder="Nome registrado da empresa" autoComplete="organization" {...accessibility("legalName")} />{errorFor("legalName")}</label>
            <label>Nome comercial<input name="tradeName" required maxLength={120} value={tradeName} onChange={e => setTradeName(e.target.value)} disabled={looking} placeholder="Como seus clientes conhecem a empresa" {...accessibility("tradeName")} />{errorFor("tradeName")}</label>
          </>}
          <label>WhatsApp<input name="phoneWhatsApp" type="tel" inputMode="numeric" required value={phone} onChange={e => setPhone(formatPhone(e.target.value))} onPaste={e => { e.preventDefault(); setPhone(formatPhone(e.clipboardData.getData("text"))); }} maxLength={15} autoComplete="tel-national" placeholder="(65) 99999-9999" {...accessibility("phoneWhatsApp")} />{errorFor("phoneWhatsApp")}<span className="field-help">Brasil (+55). Digite o DDD e o número: 10 ou 11 dígitos.</span></label>
          <label>E-mail<input name="email" type="email" required maxLength={254} autoComplete="email" placeholder="voce@exemplo.com.br" {...accessibility("email")} />{errorFor("email")}</label>
          <label>Senha<input name="password" type="password" required minLength={12} maxLength={128} autoComplete="new-password" placeholder="Pelo menos 12 caracteres" {...accessibility("password")} />{errorFor("password")}</label>
          {courier && <>
            <label>Tipo de veículo<select name="vehicleType" defaultValue="Motorcycle"><option value="Motorcycle">Motocicleta</option><option value="Car">Carro</option></select></label>
            <label>Placa<input name="plate" required value={plate} onChange={e => setPlate(formatPlate(e.target.value))} maxLength={8} autoCapitalize="characters" spellCheck={false} placeholder="ABC-1234 ou ABC1D23" {...accessibility("plate")} />{errorFor("plate")}<span className="field-help">Placa antiga ou Mercosul, com 7 letras e números.</span></label>
          </>}
          <button type="submit" disabled={looking || sending} aria-busy={sending}>{sending ? "Enviando…" : "Enviar cadastro"}</button>
        </fieldset>
      </form>
      {errors.form && <p role="alert" className="form-message error">{errors.form}</p>}
      {message && <p role="status" className="form-message">{message}</p>}
    </section>
  );
}
