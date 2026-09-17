import type { ComponentProps } from "react";
import { Field } from "@flowstack-ui/brick/field";
import { Input } from "@flowstack-ui/brick/input";
import { PasswordToggleField } from "@flowstack-ui/brick/password-toggle-field";
import { Paragraph } from "@flowstack-ui/brick/text";

export function AccountInput({ label, error, secret, ...props }: Pick<ComponentProps<typeof Input>, "name" | "value" | "onChange" | "type" | "autoComplete" | "placeholder" | "maxLength" | "required" | "disabled"> & { label: string; error?: string; secret?: boolean }) {
  return <Field.Root required={props.required} disabled={props.disabled} invalid={!!error}>
    <Field.Label>{label}</Field.Label>
    {secret ? <PasswordToggleField.Root size="lg" fullWidth showLabel={`Mostrar ${label.toLowerCase()}`} hideLabel={`Ocultar ${label.toLowerCase()}`}>
      <PasswordToggleField.Input {...props} aria-label={label} /><PasswordToggleField.Toggle />
    </PasswordToggleField.Root> : <Input {...props} aria-label={label} size="lg" fullWidth />}
    {error && <Field.Error>{error}</Field.Error>}
  </Field.Root>;
}

export function AccountFeedback({ error, message }: { error?: string; message?: string }) {
  return <Paragraph role={error ? "alert" : "status"} tone={error ? "danger" : "secondary"}>{error || message || ""}</Paragraph>;
}
