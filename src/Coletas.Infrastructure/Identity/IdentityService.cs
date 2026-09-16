using BCryptApi = BCrypt.Net.BCrypt;
using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Domain.Establishments;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coletas.Infrastructure.Identity;

/// <summary>Implementa identidade e cadastros sobre o PostgreSQL.</summary>
public sealed class IdentityService(ColetasDbContext database, SessionService sessions, RegistrationReviewService reviews) : IIdentityService
{
    /// <inheritdoc />
    public async Task<IdentityResult<RegistrationResponse>> RegisterEstablishmentAsync(
        EstablishmentRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommonRegistration(request.Email, request.Password, request.PhoneWhatsApp);
        if (validation is not null)
        {
            return IdentityResult<RegistrationResponse>.Invalid(validation);
        }

        if (string.IsNullOrWhiteSpace(request.LegalName) || string.IsNullOrWhiteSpace(request.TradeName) || string.IsNullOrWhiteSpace(request.TaxId))
        {
            return IdentityResult<RegistrationResponse>.Invalid("Razão social, nome comercial e documento são obrigatórios.");
        }

        var email = Normalize(request.Email);
        if (!RegistrationValidation.IsCnpj(request.TaxId))
            return IdentityResult<RegistrationResponse>.Invalid("Informe um CNPJ válido, incluindo os dígitos verificadores.");
        var taxId = RegistrationValidation.NormalizeCnpj(request.TaxId);
        var phone = RegistrationValidation.NormalizePhone(request.PhoneWhatsApp);
        if (await database.Users.AnyAsync(x => x.Email == email, cancellationToken)
            || await database.Establishments.AnyAsync(x => x.TaxId == taxId || x.PhoneWhatsApp == phone, cancellationToken))
        {
            return EstablishmentConflict();
        }

        var user = new User
        {
            Email = email,
            PasswordHash = BCryptApi.HashPassword(request.Password),
            Role = UserRole.Establishment,
            Status = UserStatus.Pending
        };
        database.Users.Add(user);
        database.Establishments.Add(new Establishment
        {
            UserId = user.Id,
            LegalName = request.LegalName.Trim(),
            TradeName = request.TradeName.Trim(),
            TaxId = taxId,
            PhoneWhatsApp = phone
        });
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException
        { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Users_Email" or "IX_Establishments_TaxId" or "IX_Establishments_PhoneWhatsApp" })
        {
            // A consulta prévia não elimina a corrida: o índice decide e a transação evita usuário órfão.
            // Mudança: docs/mudancas/2026-09-14-03-identificadores-exclusivos-empresa.md
            return EstablishmentConflict();
        }
        return IdentityResult<RegistrationResponse>.Ok(new(user.Id, user.Role, user.Status));
    }

    private static IdentityResult<RegistrationResponse> EstablishmentConflict() =>
        IdentityResult<RegistrationResponse>.Conflict("Cadastro não concluído: CNPJ, e-mail ou telefone já utilizado. Confira seus dados ou procure o suporte.");

    /// <inheritdoc />
    public async Task<IdentityResult<RegistrationResponse>> RegisterCourierAsync(
        CourierRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommonRegistration(request.Email, request.Password, request.PhoneWhatsApp);
        if (validation is not null)
        {
            return IdentityResult<RegistrationResponse>.Invalid(validation);
        }

        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Plate))
        {
            return IdentityResult<RegistrationResponse>.Invalid("Nome completo e placa são obrigatórios.");
        }

        if (!Enum.IsDefined(request.VehicleType))
        {
            return IdentityResult<RegistrationResponse>.Invalid("Informe um tipo de veículo válido.");
        }

        var email = Normalize(request.Email);
        if (!RegistrationValidation.IsPlate(request.Plate))
            return IdentityResult<RegistrationResponse>.Invalid("Informe uma placa como ABC-1234 ou ABC1D23.");
        var plate = RegistrationValidation.NormalizePlate(request.Plate);
        if (!RegistrationValidation.IsCpf(request.Cpf))
            return IdentityResult<RegistrationResponse>.Invalid("Informe um CPF válido, incluindo os dígitos verificadores.");
        var cpf = RegistrationValidation.NormalizeCpf(request.Cpf);
        var phone = RegistrationValidation.NormalizePhone(request.PhoneWhatsApp);
        if (await database.Users.AnyAsync(x => x.Email == email, cancellationToken)
            || await database.Vehicles.AnyAsync(x => x.Plate == plate, cancellationToken)
            || await database.Couriers.AnyAsync(x => x.Cpf == cpf || x.PhoneWhatsApp == phone, cancellationToken))
        {
            return CourierConflict();
        }

        var user = new User
        {
            Email = email,
            PasswordHash = BCryptApi.HashPassword(request.Password),
            Role = UserRole.Courier,
            Status = UserStatus.Pending
        };
        var courier = new Courier
        {
            UserId = user.Id,
            FullName = request.FullName.Trim(),
            PhoneWhatsApp = phone,
            Cpf = cpf
        };
        database.Users.Add(user);
        database.Couriers.Add(courier);
        database.Vehicles.Add(new Vehicle { CourierId = courier.Id, Type = request.VehicleType, Plate = plate });
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException
        { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Users_Email" or "IX_Vehicles_Plate" or "IX_Couriers_Cpf" or "IX_Couriers_PhoneWhatsApp" })
        {
            // Índices garantem exclusividade mesmo após consultas prévias concorrentes.
            // Mudança: docs/mudancas/2026-09-14-04-identificadores-exclusivos-entregador.md
            return CourierConflict();
        }
        return IdentityResult<RegistrationResponse>.Ok(new(user.Id, user.Role, user.Status));
    }

    private static IdentityResult<RegistrationResponse> CourierConflict() =>
        IdentityResult<RegistrationResponse>.Conflict("Cadastro não concluído: CPF, telefone, e-mail ou placa já utilizado. Confira seus dados ou procure o suporte.");

    /// <inheritdoc />
    public async Task<IdentityResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return IdentityResult<AuthResponse>.Unauthorized("Credenciais inválidas.");
        }

        var email = Normalize(request.Email);
        var user = await database.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        // Regra: a mesma resposta para e-mail inexistente, senha inválida e conta não liberada
        // reduz enumeração de contas e não revela o motivo da falha de autenticação.
        // Mudança: docs/mudancas/2026-09-10-03-fase-1-acesso-cadastros.md
        if (user is null || user.Status != UserStatus.Active || !BCryptApi.Verify(request.Password, user.PasswordHash))
        {
            return IdentityResult<AuthResponse>.Unauthorized("Credenciais inválidas.");
        }

        // Toda emissão precisa da sessão persistida (sid/scope) exigida pela autenticação.
        // Mudança: docs/mudancas/2026-09-16-01-refatoracao-identidade.md
        return IdentityResult<AuthResponse>.Ok(await sessions.CreateAsync(user, false, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IdentityResult<CourierDocumentResponse>> AddCourierDocumentAsync(
        Guid actorId,
        UserRole actorRole,
        Guid courierId,
        CourierDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var courier = await database.Couriers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == courierId, cancellationToken);
        if (courier is null)
        {
            return IdentityResult<CourierDocumentResponse>.NotFound("Entregador não encontrado.");
        }

        if (actorRole != UserRole.Admin && actorRole != UserRole.Operator && courier.UserId != actorId)
        {
            return IdentityResult<CourierDocumentResponse>.Forbidden("Você não pode alterar este cadastro.");
        }

        if (request.ExpiresAt is { } expiresAt && expiresAt <= DateTimeOffset.UtcNow)
        {
            return IdentityResult<CourierDocumentResponse>.Invalid("A data de vencimento deve estar no futuro.");
        }

        if (!Enum.IsDefined(request.Type))
        {
            return IdentityResult<CourierDocumentResponse>.Invalid("Informe um tipo de documento válido.");
        }

        var document = new CourierDocument
        {
            CourierId = courierId,
            Type = request.Type,
            ExpiresAt = request.ExpiresAt,
            Status = CourierDocumentStatus.Pending
        };
        var owner = await database.Users.SingleAsync(x => x.Id == courier.UserId, cancellationToken);
        database.CourierDocuments.Add(document);
        await reviews.InvalidateAsync(owner, actorId, "Documento cadastrado; análise precisa ser refeita.", cancellationToken);
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            database.ChangeTracker.Clear();
            return IdentityResult<CourierDocumentResponse>.Conflict("Cadastro alterado; recarregue antes de reenviar.");
        }
        return IdentityResult<CourierDocumentResponse>.Ok(new(document.Id, document.Type, document.Status, document.ExpiresAt));
    }

    private static string? ValidateCommonRegistration(string email, string password, string phoneWhatsApp)
    {
        if (!RegistrationValidation.IsEmail(email))
        {
            return "Informe um e-mail válido.";
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128)
        {
            return "A senha deve ter entre 12 e 128 caracteres.";
        }

        return phoneWhatsApp is null || phoneWhatsApp.Length > 30 || !RegistrationValidation.IsPhone(phoneWhatsApp)
            ? "Informe um WhatsApp com DDD e 10 ou 11 dígitos."
            : null;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeDocument(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string NormalizePlate(string value) => NormalizeDocument(value);
}
