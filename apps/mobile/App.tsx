import { useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { Pressable, StyleSheet, Text, View } from 'react-native';

export default function App() {
  const [status, setStatus] = useState('Você ainda não está disponível para entregas.');
  const [busy, setBusy] = useState(false);

  async function checkConnection() {
    const baseUrl = process.env.EXPO_PUBLIC_API_URL;
    if (!baseUrl) {
      setStatus('Conexão não configurada neste aplicativo.');
      return;
    }
    setBusy(true);
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10000);
    try {
      // Motivo: o telefone precisa de URL acessível na rede, nunca localhost do computador.
      // Mudança: docs/mudancas/2026-09-09-02-fase-zero.md
      const response = await fetch(baseUrl.replace(/\/$/, '') + '/health/ready', { signal: controller.signal });
      setStatus(response.ok ? 'Conexão disponível. Cadastro em breve.' : 'Serviço indisponível. Tente novamente.');
    } catch {
      setStatus('Não foi possível conectar. Verifique sua conexão.');
    } finally {
      clearTimeout(timeout);
      setBusy(false);
    }
  }

  return (
    <View style={styles.container}>
      <StatusBar style="dark" />
      <Text style={styles.brand}>Coletas / entregador</Text>
      <Text style={styles.title}>A cidade espera{ '\n' }por você.</Text>
      <Text style={styles.description}>Em breve, receba chamadas de estabelecimentos próximos e acompanhe suas entregas por aqui.</Text>
      <View style={styles.card}>
        <Text style={styles.heading}>Aplicativo em preparação</Text>
        <Text style={styles.description}>Cadastro, documentos e licença estarão disponíveis nas próximas etapas.</Text>
        <Pressable accessibilityRole="button" accessibilityState={{ disabled: busy }} disabled={busy} onPress={checkConnection} style={styles.button}>
          <Text style={styles.buttonText}>{busy ? 'Verificando…' : 'Verificar conexão'}</Text>
        </Pressable>
        <Text accessibilityLiveRegion="polite" style={styles.description}>{status}</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', backgroundColor: '#f3f6f2', padding: 28 },
  brand: { color: '#155f49', fontSize: 18, fontWeight: '700', marginBottom: 32 },
  title: { fontSize: 40, fontWeight: '700', color: '#153731', marginBottom: 20 },
  description: { color: '#52645a', fontSize: 16, lineHeight: 25, marginVertical: 10 },
  card: { backgroundColor: '#fff', borderRadius: 16, padding: 24, marginTop: 24 },
  heading: { fontSize: 20, color: '#153731', fontWeight: '600' },
  button: { backgroundColor: '#155f49', padding: 16, borderRadius: 9, marginVertical: 16 },
  buttonText: { color: '#fff', textAlign: 'center', fontSize: 16, fontWeight: '600' },
});
