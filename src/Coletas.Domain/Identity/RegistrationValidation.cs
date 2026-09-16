using System.Text.RegularExpressions;

namespace Coletas.Domain.Identity;

/// <summary>Regras brasileiras dos identificadores de cadastro.</summary>
public static class RegistrationValidation
{
    /// <summary>Valida endereço de e-mail simples, sem nome de exibição ou espaços internos.</summary>
    public static bool IsEmail(string? value)
    {
        var email = value?.Trim();
        return !string.IsNullOrEmpty(email) && email.Length <= 254
            && !email.Any(char.IsWhiteSpace)
            && System.Net.Mail.MailAddress.TryCreate(email, out var address)
            && address.Address.Equals(email, StringComparison.OrdinalIgnoreCase)
            && address.Host.Contains('.', StringComparison.Ordinal)
            && !address.Host.StartsWith(".", StringComparison.Ordinal)
            && !address.Host.EndsWith(".", StringComparison.Ordinal);
    }

    /// <summary>Remove somente pontuação admitida do CPF.</summary>
    public static string NormalizeCpf(string? value) => Regex.Replace(value ?? "", @"[.\s-]", "");

    /// <summary>Verifica os dois dígitos do CPF, rejeitando sequências repetidas.</summary>
    public static bool IsCpf(string? value)
    {
        var cpf = NormalizeCpf(value);
        if (!Regex.IsMatch(cpf, @"\A[0-9]{11}\z") || cpf.Distinct().Count() == 1) return false;
        for (var length = 9; length <= 10; length++)
        {
            var sum = 0;
            for (var i = 0; i < length; i++) sum += (cpf[i] - '0') * (length + 1 - i);
            var digit = sum * 10 % 11;
            if (cpf[length] - '0' != (digit == 10 ? 0 : digit)) return false;
        }
        return true;
    }

    /// <summary>Remove somente a pontuação admitida, preservando erros de conteúdo para validação.</summary>
    public static string NormalizePhone(string? value)
    {
        var phone = Regex.Replace(value ?? "", @"[\s()+-]", "");
        if (phone.Length is 12 or 13 && phone.StartsWith("55", StringComparison.Ordinal)) phone = phone[2..];
        return phone;
    }

    /// <summary>Aceita telefone nacional com DDD e número fixo ou celular.</summary>
    public static bool IsPhone(string? value) => Regex.IsMatch(NormalizePhone(value), @"\A[1-9][0-9](?:[2-5][0-9]{7}|9[0-9]{8})\z");

    /// <summary>Normaliza placa sem converter caracteres de um modelo para outro.</summary>
    public static string NormalizePlate(string? value) => (value ?? "").Trim().Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();

    /// <summary>Placa antiga ABC1234 ou Mercosul ABC1D23.</summary>
    public static bool IsPlate(string? value) => Regex.IsMatch(NormalizePlate(value), @"\A[A-Z]{3}[0-9][A-Z0-9][0-9]{2}\z");

    /// <summary>Remove a máscara de CNPJ.</summary>
    public static string NormalizeCnpj(string? value) => Regex.Replace(value ?? "", @"[./\s-]", "").ToUpperInvariant();

    /// <summary>Verifica CNPJ tradicional e alfanumérico sem consultar situação cadastral.</summary>
    public static bool IsCnpj(string? value)
    {
        var cnpj = NormalizeCnpj(value);
        if (!Regex.IsMatch(cnpj, @"\A[A-Z0-9]{12}[0-9]{2}\z") || cnpj.Distinct().Count() == 1) return false;
        // Regra: Receita usa ASCII-48 e módulo 11 em ambos os formatos.
        // Mudança: docs/mudancas/2026-09-11-01-validacao-cadastros.md
        foreach (var length in new[] { 12, 13 })
        {
            var sum = 0;
            for (var i = 0; i < length; i++) sum += (cnpj[i] - '0') * ((length - 1 - i) % 8 + 2);
            var remainder = sum % 11;
            if (cnpj[length] - '0' != (remainder < 2 ? 0 : 11 - remainder)) return false;
        }
        return true;
    }
}
