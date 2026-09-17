import { useEffect, useRef, useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { BackHandler, KeyboardAvoidingView, Platform, ScrollView, Text, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import * as DocumentPicker from 'expo-document-picker';
import { Field, Button, Choice, ui } from './src/ui';
import { accountStatus, documentChoices, documentStatus, expirationIso, registrationErrors, vehicleChoices, type Registration } from './src/validation';
import { cpfValue, formatCpf, formatPhone, formatPlate, phoneDigits, plateValue } from '../shared/registrationValidation';
import { clearSession, login, logout, request, type Profile } from './src/api';

const empty: Registration = { cpf: '', fullName: '', email: '', phoneWhatsApp: '', password: '', confirmation: '', vehicleType: 'Motorcycle', plate: '' };
export default function App() {
  const [screen, setScreen] = useState<'login' | 'register' | 'account'>('login');
  const [values, setValues] = useState<Registration>(empty);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [errors, setErrors] = useState<Record<string, string | undefined>>({});
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  const [docType, setDocType] = useState<'DriverLicense' | 'VehicleRegistration'>('DriverLicense');
  const [expiry, setExpiry] = useState('');
  const [file, setFile] = useState<DocumentPicker.DocumentPickerAsset | null>(null);
  const active = useRef(true);
  const lock = useRef(false);
  useEffect(() => { active.current = true; return () => { active.current = false; clearSession(); }; }, []);
  useEffect(() => {
    const subscription = BackHandler.addEventListener('hardwareBackPress', () => {
      if (busy) return true;
      if (screen === 'register') { setScreen('login'); return true; }
      return false;
    });
    return () => subscription.remove();
  }, [screen, busy]);
  function update<K extends keyof Registration>(key: K, value: Registration[K]) {
    setValues(previous => ({ ...previous, [key]: value }));
    setErrors(previous => ({ ...previous, [key]: undefined }));
  }
  async function run(action: () => Promise<void>) {
    if (lock.current) return;
    lock.current = true; setBusy(true); setMessage('');
    try { await action(); }
    catch (error) { if (active.current) setMessage(error instanceof Error ? error.message : 'Não foi possível concluir.'); }
    finally { lock.current = false; if (active.current) setBusy(false); }
  }
  function requireLogin() {
    clearSession(); setProfile(null); setScreen('login');
    setValues(previous => ({ ...previous, password: '', confirmation: '' }));
    setMessage('Alteração salva. Entre novamente para continuar a análise do cadastro.');
  }
  async function register() {
    const next = registrationErrors(values); setErrors(next);
    if (Object.values(next).some(Boolean)) return;
    await run(async () => {
      const { confirmation: _, ...payload } = values;
      await request('/auth/register/couriers', { method: 'POST', body: JSON.stringify({
        ...payload, cpf: cpfValue(values.cpf), phoneWhatsApp: phoneDigits(values.phoneWhatsApp), plate: plateValue(values.plate),
      }) }, false);
      if (!active.current) return;
      setValues({ ...empty, email: values.email }); setScreen('login'); setMessage('Cadastro enviado. Entre para enviar os documentos e acompanhar a análise.');
    });
  }
  async function enter() {
    await run(async () => {
      const account = await login(values.email.trim(), values.password);
      if (!active.current) return;
      if (account.role !== 'Courier') { clearSession(); throw new Error('Este aplicativo é exclusivo para entregadores. Use o portal web.'); }
      setProfile(account); setScreen('account'); setValues(previous => ({ ...previous, password: '' }));
    });
  }
  async function upload() {
    await run(async () => {
      if (!file || !profile?.courierId) throw new Error('Selecione um documento.');
      const date = expirationIso(expiry);
      if (!date) throw new Error('Informe a validade.');
      const body = new FormData();
      body.append('type', docType); body.append('expiresAt', date);
      // React Native envia URI local por multipart; não converter arquivo privado em base64/log.
      // Mudança: docs/mudancas/2026-09-16-04-mobile-acesso-cpf.md
      body.append('file', { uri: file.uri, name: file.name, type: file.mimeType ?? 'application/octet-stream' } as unknown as Blob);
      await request(`/couriers/${profile.courierId}/documents/upload`, { method: 'POST', body });
      if (active.current) { setFile(null); setExpiry(''); requireLogin(); }
    });
  }
  return <SafeAreaProvider><SafeAreaView style={ui.safe}><StatusBar style="dark" />
    <KeyboardAvoidingView style={ui.flex} behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
      <ScrollView contentContainerStyle={ui.content} keyboardShouldPersistTaps="handled">
        <Text style={ui.brand}>Coletas / entregador</Text>
        <Text style={ui.title}>{screen === 'register' ? 'Seu cadastro' : screen === 'account' ? 'Minha conta' : 'Acesse sua conta'}</Text>
        <Text style={ui.text}>Cadastro e documentos reais. Ofertas e entregas ainda não estão disponíveis.</Text>
        {!!message && <Text accessibilityLiveRegion="polite" style={ui.notice}>{message}</Text>}
        {screen === 'register' ? <View style={ui.card}>
          <Field label="CPF" value={values.cpf} onChangeText={value => update('cpf', formatCpf(value))} keyboardType="number-pad" maxLength={14} placeholder="000.000.000-00" error={errors.cpf} editable={!busy} />
          <Field label="Nome completo" value={values.fullName} onChangeText={value => update('fullName', value)} maxLength={200} placeholder="Seu nome completo" error={errors.fullName} editable={!busy} />
          <Field label="WhatsApp" value={values.phoneWhatsApp} onChangeText={value => update('phoneWhatsApp', formatPhone(value))} keyboardType="phone-pad" maxLength={15} placeholder="(65) 99999-9999" error={errors.phoneWhatsApp} editable={!busy} />
          <Field label="E-mail" value={values.email} onChangeText={value => update('email', value)} autoCapitalize="none" keyboardType="email-address" maxLength={254} placeholder="voce@exemplo.com.br" error={errors.email} editable={!busy} />
          <Field label="Senha" value={values.password} onChangeText={value => update('password', value)} secureTextEntry autoComplete="new-password" placeholder="Pelo menos 12 caracteres" error={errors.password} editable={!busy} />
          <Field label="Confirmar senha" value={values.confirmation} onChangeText={value => update('confirmation', value)} secureTextEntry autoComplete="new-password" placeholder="Repita sua senha" error={errors.confirmation} editable={!busy} />
          <Choice label="Tipo de veículo" value={values.vehicleType} choices={vehicleChoices} onChange={value => update('vehicleType', value)} disabled={busy} />
          <Field label="Placa" value={values.plate} onChangeText={value => update('plate', formatPlate(value))} maxLength={8} autoCapitalize="characters" placeholder="ABC-1234 ou ABC1D23" error={errors.plate} editable={!busy} />
          <Button label="Enviar cadastro" busy={busy} onPress={() => void register()} />
          <Button label="Voltar ao login" secondary disabled={busy} onPress={() => setScreen('login')} />
        </View> : screen === 'login' ? <View style={ui.card}>
          <Field label="E-mail" value={values.email} onChangeText={value => update('email', value)} autoCapitalize="none" keyboardType="email-address" placeholder="voce@exemplo.com.br" editable={!busy} />
          <Field label="Senha" value={values.password} onChangeText={value => update('password', value)} secureTextEntry autoComplete="current-password" placeholder="Sua senha" editable={!busy} />
          <Button label="Entrar" busy={busy} onPress={() => void enter()} />
          <Button label="Criar cadastro" secondary disabled={busy} onPress={() => setScreen('register')} />
          <Button label="Esqueci minha senha" secondary disabled={busy} onPress={() => void run(async () => {
            if (!values.email.trim()) throw new Error('Informe o e-mail antes de solicitar recuperação.');
            await request('/auth/recovery', { method: 'POST', body: JSON.stringify({ email: values.email.trim() }) }, false);
            setMessage('Se houver uma conta elegível, você receberá as instruções por e-mail.');
          })} />
        </View> : profile && <View style={ui.card}>
          <Text style={ui.heading}>{accountStatus[profile.status] ?? profile.status}</Text>
          <Text style={ui.text}>{profile.email}</Text>{!!profile.reason && <Text style={ui.text}>{profile.reason}</Text>}
          {profile.vehicles.map(vehicle => <Text key={vehicle.id} style={ui.text}>Veículo: {vehicle.plate}</Text>)}
          <Text style={ui.heading}>Documentos ({profile.documents.length})</Text>
          {profile.documents.slice(0, 10).map(doc => <Text key={doc.id} style={ui.text}>{doc.type === 'DriverLicense' ? 'CNH' : 'Veículo'}: {documentStatus[doc.status] ?? doc.status}</Text>)}
          {profile.documents.length > 10 && <Text style={ui.hint}>Mostrando os 10 mais recentes. Histórico completo no portal web.</Text>}
          <Text style={ui.text}>Enviar um documento exige nova análise e novo login.</Text>
          <Choice label="Tipo de documento" value={docType} choices={documentChoices} onChange={setDocType} disabled={busy} />
          <Field label="Validade" value={expiry} onChangeText={setExpiry} placeholder="DD/MM/AAAA" maxLength={10} editable={!busy} />
          <Button label={file ? 'Trocar arquivo selecionado' : 'Selecionar PDF ou imagem'} secondary disabled={busy} onPress={() => void run(async () => {
            const result = await DocumentPicker.getDocumentAsync({ type: ['application/pdf', 'image/jpeg', 'image/png'], copyToCacheDirectory: true, multiple: false });
            if (!result.canceled && active.current) setFile(result.assets[0]);
          })} />
          {file && <Text style={ui.hint}>{file.name}</Text>}
          <Button label="Enviar documento" disabled={!file} busy={busy} onPress={() => void upload()} />
          <Button label="Atualizar situação" secondary disabled={busy} onPress={() => void run(async () => { const next = await request<Profile>('/auth/me'); if (active.current) setProfile(next); })} />
          <Button label="Sair" secondary disabled={busy} onPress={() => void run(async () => { try { await logout(); } finally { if (active.current) { setProfile(null); setScreen('login'); } } })} />
        </View>}
      </ScrollView>
    </KeyboardAvoidingView>
  </SafeAreaView></SafeAreaProvider>;
}
