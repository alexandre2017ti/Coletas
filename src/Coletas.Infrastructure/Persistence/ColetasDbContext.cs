using Coletas.Domain.Couriers;
using Coletas.Domain.Establishments;
using Coletas.Domain.Identity;
using Coletas.Domain.Settings;
using Microsoft.EntityFrameworkCore;
namespace Coletas.Infrastructure.Persistence;
/// <summary>Persistência do monólito modular.</summary>
public sealed class ColetasDbContext(DbContextOptions<ColetasDbContext> options) : DbContext(options)
{
    /// <summary>Configurações operacionais.</summary>
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    /// <summary>Usuários autenticáveis.</summary>
    public DbSet<User> Users => Set<User>();
    /// <summary>Estabelecimentos cadastrados.</summary>
    public DbSet<Establishment> Establishments => Set<Establishment>();
    /// <summary>Entregadores cadastrados.</summary>
    public DbSet<Courier> Couriers => Set<Courier>();
    /// <summary>Veículos cadastrados.</summary>
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    /// <summary>Documentos dos entregadores.</summary>
    public DbSet<CourierDocument> CourierDocuments => Set<CourierDocument>();
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        var settings = modelBuilder.Entity<SystemSetting>();
        settings.ToTable("SystemSettings", "configuration");
        settings.HasKey(x => x.Key);
        settings.Property(x => x.Key).HasMaxLength(120);
        settings.Property(x => x.Value).HasMaxLength(2000);
        // Motivo: 5 km é somente o valor inicial solicitado, persistido para futura edição administrativa.
        // Mudança: docs/mudancas/2026-09-09-02-fase-zero.md
        settings.HasData(new SystemSetting { Key = "DeliveryGrouping.RadiusMeters", Value = "5000" });

        var users = modelBuilder.Entity<User>();
        users.ToTable("Users", "identity");
        users.HasKey(x => x.Id);
        users.HasIndex(x => x.Email).IsUnique();
        users.Property(x => x.Email).HasMaxLength(254);
        users.Property(x => x.PasswordHash).HasMaxLength(200);
        users.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
        users.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        var establishments = modelBuilder.Entity<Establishment>();
        establishments.ToTable("Establishments", "establishments");
        establishments.HasKey(x => x.Id);
        establishments.HasIndex(x => x.UserId).IsUnique();
        establishments.HasIndex(x => x.TaxId).IsUnique();
        establishments.Property(x => x.LegalName).HasMaxLength(200);
        establishments.Property(x => x.TradeName).HasMaxLength(120);
        establishments.Property(x => x.TaxId).HasMaxLength(30);
        establishments.Property(x => x.PhoneWhatsApp).HasMaxLength(30);

        var couriers = modelBuilder.Entity<Courier>();
        couriers.ToTable("Couriers", "couriers");
        couriers.HasKey(x => x.Id);
        couriers.HasIndex(x => x.UserId).IsUnique();
        couriers.Property(x => x.FullName).HasMaxLength(200);
        couriers.Property(x => x.PhoneWhatsApp).HasMaxLength(30);

        var vehicles = modelBuilder.Entity<Vehicle>();
        vehicles.ToTable("Vehicles", "couriers");
        vehicles.HasKey(x => x.Id);
        vehicles.HasIndex(x => x.Plate).IsUnique();
        vehicles.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        vehicles.Property(x => x.Plate).HasMaxLength(10);
        vehicles.HasOne<Courier>().WithMany(x => x.Vehicles).HasForeignKey(x => x.CourierId).OnDelete(DeleteBehavior.Cascade);

        var documents = modelBuilder.Entity<CourierDocument>();
        documents.ToTable("CourierDocuments", "compliance");
        documents.HasKey(x => x.Id);
        documents.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        documents.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        documents.HasIndex(x => new { x.CourierId, x.Type, x.Status });
        documents.HasOne<Courier>().WithMany(x => x.Documents).HasForeignKey(x => x.CourierId).OnDelete(DeleteBehavior.Cascade);

        establishments.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        couriers.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
