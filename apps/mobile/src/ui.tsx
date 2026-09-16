import { forwardRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View, type TextInputProps } from 'react-native';

// Identidade derivada de DESIGN.md; campos têm borda mais forte para uso no celular.
// Mudança: docs/mudancas/2026-09-10-17-mobile-fase1.md
export const colors = { primary: '#155f49', ink: '#153731', canvas: '#f3f6f2', surface: '#ffffff', muted: '#52645a', border: '#71877b', danger: '#a32127', soft: '#e4eee8' };

export const Field = forwardRef<TextInput, TextInputProps & { label: string; error?: string; hint?: string }>(function Field({ label, error, hint, secureTextEntry, ...props }, ref) {
  const [focused, setFocused] = useState(false);
  const [visible, setVisible] = useState(false);
  return <View style={ui.field}>
    <Text style={ui.label}>{label}</Text>
    <TextInput {...props} ref={ref} accessibilityLabel={label} accessibilityHint={error || hint}
      placeholderTextColor={colors.muted} secureTextEntry={secureTextEntry && !visible}
      onFocus={() => setFocused(true)} onBlur={() => setFocused(false)}
      style={[ui.input, focused && ui.focused, !!error && ui.invalid]} />
    {secureTextEntry && <Pressable accessibilityRole="button" accessibilityLabel={visible ? 'Ocultar senha ou código' : 'Mostrar senha ou código'} onPress={() => setVisible(!visible)} style={ui.reveal}><Text style={ui.link}>{visible ? 'Ocultar' : 'Mostrar'}</Text></Pressable>}
    {!!(error || hint) && <Text accessibilityLiveRegion="polite" style={error ? ui.error : ui.hint}>{error || hint}</Text>}
  </View>;
});

export function Button({ label, onPress, secondary = false, disabled = false, busy = false }: { label: string; onPress: () => void; secondary?: boolean; disabled?: boolean; busy?: boolean }) {
  return <Pressable accessibilityRole="button" accessibilityState={{ disabled: disabled || busy, busy }} disabled={disabled || busy} onPress={onPress}
    style={({ pressed }) => [ui.button, secondary && ui.secondary, pressed && ui.pressed, (disabled || busy) && ui.disabled]}>
    {busy && <ActivityIndicator color={secondary ? colors.primary : colors.surface} />}
    <Text style={[ui.buttonText, secondary && ui.secondaryText]}>{label}</Text>
  </Pressable>;
}

export function Choice<T extends string>({ label, value, choices, onChange, disabled }: { label: string; value: T; choices: ReadonlyArray<{ value: T; label: string }>; onChange: (value: T) => void; disabled?: boolean }) {
  return <View style={ui.field}><Text style={ui.label}>{label}</Text><View style={ui.choices}>
    {choices.map(option => <Pressable key={option.value} accessibilityRole="radio" accessibilityLabel={option.label} accessibilityState={{ checked: value === option.value, disabled }} disabled={disabled} onPress={() => onChange(option.value)} style={[ui.choice, value === option.value && ui.choiceActive]}>
      <Text style={ui.label}>{value === option.value ? '● ' : '○ '}{option.label}</Text>
    </Pressable>)}
  </View></View>;
}

export const ui = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.canvas }, flex: { flex: 1 },
  content: { padding: 24, gap: 16, width: '100%', maxWidth: 620, alignSelf: 'center', paddingBottom: 40 },
  brand: { color: colors.primary, fontSize: 18, fontWeight: '800', letterSpacing: 1 },
  eyebrow: { color: colors.muted, fontSize: 13, letterSpacing: 1, fontWeight: '700' },
  title: { color: colors.ink, fontSize: 30, fontWeight: '700' }, heading: { color: colors.ink, fontSize: 20, fontWeight: '700' },
  text: { color: colors.muted, fontSize: 16, lineHeight: 24 },
  card: { backgroundColor: colors.surface, borderRadius: 16, padding: 20, gap: 16, borderWidth: 1, borderColor: '#d5dfd6' },
  field: { gap: 7 }, label: { color: colors.ink, fontSize: 16, fontWeight: '600' },
  input: { color: colors.ink, backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 2, borderRadius: 10, paddingHorizontal: 14, paddingVertical: 12, minHeight: 52, fontSize: 16 },
  focused: { borderColor: colors.primary, backgroundColor: '#f0f8f3' }, invalid: { borderColor: colors.danger },
  hint: { color: colors.muted, fontSize: 14, lineHeight: 20 }, error: { color: colors.danger, fontSize: 14, lineHeight: 20 },
  link: { color: colors.primary, fontSize: 15, fontWeight: '700' }, reveal: { alignSelf: 'flex-end', minHeight: 44, justifyContent: 'center', paddingHorizontal: 12 },
  button: { minHeight: 52, borderRadius: 10, backgroundColor: colors.primary, borderWidth: 2, borderColor: colors.primary, alignItems: 'center', justifyContent: 'center', flexDirection: 'row', gap: 10, padding: 12 },
  buttonText: { color: colors.surface, fontSize: 16, fontWeight: '700', textAlign: 'center' }, secondary: { backgroundColor: colors.surface, borderColor: colors.border }, secondaryText: { color: colors.primary },
  pressed: { opacity: 0.75 }, disabled: { opacity: 0.5 }, choices: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  choice: { borderColor: colors.border, borderWidth: 2, borderRadius: 10, padding: 12, minHeight: 48 }, choiceActive: { backgroundColor: colors.soft, borderColor: colors.primary },
  notice: { borderLeftWidth: 4, borderLeftColor: colors.primary, padding: 14, backgroundColor: colors.soft, gap: 6 },
  document: { paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: colors.border, gap: 6 },
});
