using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCryptApi = BCrypt.Net.BCrypt;
using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Domain.Establishments;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Coletas.Infrastructure.Identity;

/// <summary>Implementa identidade e cadastros sobre o PostgreSQL.</summary>
public sealed class IdentityService(ColetasDbContext database, IOptions<JwtOptions> jwtOptions) : IIdentityService
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
        var taxId = NormalizeDocument(request.TaxId);
        if (await database.Users.AnyAsync(x => x.Email == email, cancellationToken)
            || await database.Establishments.AnyAsync(x => x.TaxId == taxId, cancellationToken))
        {
            return IdentityResult<RegistrationResponse>.Conflict("Não foi possível concluir o cadastro com os dados informados.");
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
            PhoneWhatsApp = request.PhoneWhatsApp.Trim()
        });
        await database.SaveChangesAsync(cancellationToken);
        return IdentityResult<RegistrationResponse>.Ok(new(user.Id, user.Role, user.Status));
    }

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
        var plate = NormalizePlate(request.Plate);
        if (await database.Users.AnyAsync(x => x.Email == email, cancellationToken)
            || await database.Vehicles.AnyAsync(x => x.Plate == plate, cancellationToken))
        {
            return IdentityResult<RegistrationResponse>.Conflict("Não foi possível concluir o cadastro com os dados informados.");
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
            PhoneWhatsApp = request.PhoneWhatsApp.Trim()
        };
        database.Users.Add(user);
        database.Couriers.Add(courier);
        database.Vehicles.Add(new Vehicle { CourierId = courier.Id, Type = request.VehicleType, Plate = plate });
        await database.SaveChangesAsync(cancellationToken);
        return IdentityResult<RegistrationResponse>.Ok(new(user.Id, user.Role, user.Status));
    }

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

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwtOptions.Value.Issuer,
            jwtOptions.Value.Audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);
        return IdentityResult<AuthResponse>.Ok(new(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, user.Role));
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
        database.CourierDocuments.Add(document);
        await database.SaveChangesAsync(cancellationToken);
        return IdentityResult<CourierDocumentResponse>.Ok(new(document.Id, document.Type, document.Status, document.ExpiresAt));
    }

    /// <inheritdoc />
    public async Task<IdentityResult<RegistrationResponse>> SetUserStatusAsync(
        UserRole actorRole,
        Guid userId,
        UserStatus status,
        CancellationToken cancellationToken)
    {
        if (actorRole != UserRole.Admin)
        {
            return IdentityResult<RegistrationResponse>.Forbidden("Apenas administradores podem alterar a aprovação.");
        }

        var user = await database.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return IdentityResult<RegistrationResponse>.NotFound("Usuário não encontrado.");
        }

        user.Status = status;
        await database.SaveChangesAsync(cancellationToken);
        return IdentityResult<RegistrationResponse>.Ok(new(user.Id, user.Role, user.Status));
    }

    private static string? ValidateCommonRegistration(string email, string password, string phoneWhatsApp)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal) || email.Length > 254)
        {
            return "Informe um e-mail válido.";
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128)
        {
            return "A senha deve ter entre 12 e 128 caracteres.";
        }

        return string.IsNullOrWhiteSpace(phoneWhatsApp) || phoneWhatsApp.Length > 30
            ? "Informe um telefone WhatsApp válido."
            : null;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeDocument(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string NormalizePlate(string value) => NormalizeDocument(value);
}
