using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coletas.Infrastructure.Identity;

/// <summary>Consulta perfis e mantém os dados de veículo; documentos e bootstrap têm serviços próprios.</summary>
public sealed class ProfileService(ColetasDbContext db, RegistrationReviewService reviews)
{
    public async Task<AccountProfile?> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return null;
        var courier = await db.Couriers.AsNoTracking().Include(x => x.Vehicles).Include(x => x.Documents).SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var establishment = await db.Establishments.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        // Dados pessoais apenas no detalhe autorizado; a fila permanece sem identificadores sensíveis.
        // Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
        RegistrationDetails? registration = courier is not null
            ? new(courier.FullName, null, courier.Cpf, courier.PhoneWhatsApp)
            : establishment is not null ? new(establishment.LegalName, establishment.TradeName, establishment.TaxId, establishment.PhoneWhatsApp) : null;
        return new(user.Id, user.Email, user.Role, user.Status, courier?.Id,
            courier?.Vehicles.Select(x => new VehicleResponse(x.Id, x.Type, x.Plate)).ToArray() ?? [],
            courier?.Documents.OrderByDescending(x => x.CreatedAt).Select(x => new DocumentProfile(x.Id, x.Type, x.Status, x.ExpiresAt, x.StorageKey != null, x.ReviewReason)).ToArray() ?? [], user.StatusReason, registration);
    }

    public async Task<UserPage> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await db.Users.CountAsync(ct);
        var items = await db.Users.AsNoTracking().OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new UserSummary(x.Id, x.Email, x.Role, x.Status, db.Couriers.Where(c => c.UserId == x.Id).Select(c => (Guid?)c.Id).FirstOrDefault())).ToListAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task<IdentityResult<VehicleResponse>> EditVehicleAsync(Guid actor, Guid courierId, Guid vehicleId, VehicleRequest request, CancellationToken ct)
    {
        if (!await CourierAccess.CanEditAsync(db, actor, courierId, ct)) return IdentityResult<VehicleResponse>.Forbidden("Cadastro não autorizado.");
        var plate = RegistrationValidation.NormalizePlate(request.Plate);
        if (!Enum.IsDefined(request.Type) || !RegistrationValidation.IsPlate(request.Plate)) return IdentityResult<VehicleResponse>.Invalid("Informe tipo e placa como ABC-1234 ou ABC1D23.");
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(x => x.Id == vehicleId && x.CourierId == courierId, ct);
        if (vehicle is null) return IdentityResult<VehicleResponse>.NotFound("Veículo não encontrado.");
        if (await db.Vehicles.AnyAsync(x => x.Plate == plate && x.Id != vehicleId, ct)) return IdentityResult<VehicleResponse>.Conflict("Placa já cadastrada.");
        if (vehicle.Plate != plate || vehicle.Type != request.Type)
        {
            // Regra: troca de veículo invalida aprovação documental anterior e exige nova análise.
            // Mudança: docs/mudancas/2026-09-10-14-backend-fase-1.md
            var courier = await db.Couriers.SingleAsync(x => x.Id == courierId, ct);
            var owner = await db.Users.SingleAsync(x => x.Id == courier.UserId, ct);
            foreach (var doc in await db.CourierDocuments.Where(x => x.CourierId == courierId && x.Type == CourierDocumentType.VehicleRegistration).ToListAsync(ct))
            { doc.Status = CourierDocumentStatus.Pending; doc.ReviewReason = "Veículo alterado; envie documento atualizado."; }
            await reviews.InvalidateAsync(owner, actor, "Veículo alterado; cadastro aguarda nova análise.", ct);
        }
        vehicle.Type = request.Type; vehicle.Plate = plate;
        Audit(actor, courierId, "vehicle.edit", "Veículo atualizado.");
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return IdentityResult<VehicleResponse>.Conflict("Cadastro foi alterado; recarregue os dados."); }
        return IdentityResult<VehicleResponse>.Ok(new(vehicle.Id, vehicle.Type, vehicle.Plate));
    }

    private void Audit(Guid actor, Guid subject, string action, string detail) => db.IdentityAudits.Add(new IdentityAudit { ActorId = actor, SubjectId = subject, Action = action, Detail = detail });
}
