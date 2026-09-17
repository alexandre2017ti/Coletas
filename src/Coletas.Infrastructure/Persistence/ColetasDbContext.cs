using Coletas.Domain.Couriers;
using Coletas.Domain.Establishments;
using Coletas.Domain.Identity;
using Coletas.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace Coletas.Infrastructure.Persistence;
/// <summary>Persistência do monólito modular.</summary>
public sealed class ColetasDbContext(DbContextOptions<ColetasDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_ReviewEvents_UserId_Version" })
        {
            // O INSERT do histórico pode detectar a mesma corrida antes do UPDATE versionado.
            // Preservar o tratamento 409 de todos os chamadores, sem ocultar outras constraints.
            // Mudança: docs/mudancas/2026-09-17-02-conflito-revisao-postgres.md
            throw new DbUpdateConcurrencyException("A revisão cadastral foi alterada por outra operação.", exception);
        }
    }

    public DbSet<SecurityToken> SecurityTokens => Set<SecurityToken>();
    public DbSet<IdentityAudit> IdentityAudits => Set<IdentityAudit>();
    public DbSet<ReviewEvent> ReviewEvents => Set<ReviewEvent>();
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
        users.Property(x => x.SecurityVersion).IsConcurrencyToken();
        users.Property(x => x.ReviewVersion).IsConcurrencyToken();
        users.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(30);
        var reviews = modelBuilder.Entity<ReviewEvent>();
        reviews.ToTable("ReviewEvents", "identity");
        reviews.HasKey(x => x.Id);
        reviews.HasIndex(x => new { x.UserId, x.Version }).IsUnique();
        reviews.Property(x => x.Action).HasMaxLength(40);
        reviews.Property(x => x.PublicReason).HasMaxLength(500);
        reviews.Property(x => x.InternalNote).HasMaxLength(1000);
        reviews.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var tokens = modelBuilder.Entity<SecurityToken>();
        tokens.ToTable("SecurityTokens", "identity");
        tokens.HasKey(x => x.Id);
        tokens.HasIndex(x => x.Hash).IsUnique();
        tokens.Property(x => x.Hash).HasMaxLength(64);
        tokens.Property(x => x.Purpose).HasMaxLength(20);
        tokens.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        var audits = modelBuilder.Entity<IdentityAudit>();
        audits.ToTable("IdentityAudits", "identity");
        audits.HasKey(x => x.Id);
        audits.Property(x => x.Action).HasMaxLength(60);
        audits.Property(x => x.Detail).HasMaxLength(500);
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
        // Regra: um telefone normalizado pertence a apenas uma empresa, inclusive em requisições simultâneas.
        // Mudança: docs/mudancas/2026-09-14-03-identificadores-exclusivos-empresa.md
        establishments.HasIndex(x => x.PhoneWhatsApp).IsUnique();
        establishments.Property(x => x.LegalName).HasMaxLength(200);
        establishments.Property(x => x.TradeName).HasMaxLength(120);
        establishments.Property(x => x.TaxId).HasMaxLength(30);
        establishments.Property(x => x.PhoneWhatsApp).HasMaxLength(30);

        var couriers = modelBuilder.Entity<Courier>();
        couriers.ToTable("Couriers", "couriers");
        couriers.HasKey(x => x.Id);
        couriers.HasIndex(x => x.UserId).IsUnique();
        // CPF legado pode ser nulo; novos cadastros exigem CPF válido no serviço.
        // Mudança: docs/mudancas/2026-09-14-04-identificadores-exclusivos-entregador.md
        couriers.Property(x => x.Cpf).HasMaxLength(11);
        couriers.HasIndex(x => x.Cpf).IsUnique();
        couriers.HasIndex(x => x.PhoneWhatsApp).IsUnique();
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
        documents.Property(x => x.StorageKey).HasMaxLength(64);
        documents.Property(x => x.ContentType).HasMaxLength(100);
        documents.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        documents.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        documents.HasIndex(x => new { x.CourierId, x.Type, x.Status });
        documents.HasOne<Courier>().WithMany(x => x.Documents).HasForeignKey(x => x.CourierId).OnDelete(DeleteBehavior.Cascade);

        establishments.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        couriers.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
