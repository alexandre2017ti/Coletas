using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coletas.Infrastructure.Identity;

/// <summary>Regra única de propriedade compartilhada por veículo e documentos.</summary>
internal static class CourierAccess
{
    internal static async Task<bool> CanEditAsync(ColetasDbContext db, Guid actor, Guid courierId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actor, ct);
        return user is not null && user.Status != UserStatus.Blocked &&
            (user.Role == UserRole.Admin && user.Status == UserStatus.Active || await db.Couriers.AnyAsync(x => x.Id == courierId && x.UserId == actor, ct));
    }

    internal static Task<bool> IsAdminAsync(ColetasDbContext db, Guid actor, CancellationToken ct) =>
        db.Users.AnyAsync(x => x.Id == actor && x.Role == UserRole.Admin && x.Status == UserStatus.Active, ct);
}
