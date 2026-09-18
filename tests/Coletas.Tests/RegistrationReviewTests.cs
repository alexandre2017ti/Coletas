using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Coletas.Tests;

public sealed class RegistrationReviewTests
{
    private sealed class Mailer : IRecoveryMailer
    {
        public bool IsConfigured => false;
        public Task SendAsync(string email, string token, CancellationToken ct) => Task.CompletedTask;
    }

    private static RegistrationReviewService Service(ColetasDbContext db) => new(db,
        new SessionService(db, Options.Create(new JwtOptions()), Options.Create(new SessionOptions()), new Mailer()));
    private static User NewUser(UserRole role, UserStatus status = UserStatus.Pending) => new()
    { Email = $"{Guid.NewGuid():N}@example.test", PasswordHash = "not-used", Role = role, Status = status };
    private static ColetasDbContext Database() => new(new DbContextOptionsBuilder<ColetasDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Theory]
    [InlineData("metadata", false)]
    [InlineData("metadata", true)]
    [InlineData("vehicle", false)]
    [InlineData("vehicle", true)]
    [InlineData("upload", false)]
    [InlineData("upload", true)]
    [InlineData("decision", false)]
    [InlineData("decision", true)]
    public async Task RelevantChangesInvalidateReviewPreserveBlockAndRevokeSessions(string change, bool blocked)
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active);
        var owner = NewUser(UserRole.Courier, blocked ? UserStatus.Blocked : UserStatus.Active);
        owner.ReviewStatus = ReviewStatus.Approved;
        owner.ReviewVersion = 7;
        owner.StatusReason = "Bloqueio administrativo";
        var courier = new Courier { UserId = owner.Id, FullName = "Teste", Cpf = "52998224725", PhoneWhatsApp = "65999999999" };
        var vehicle = new Vehicle { CourierId = courier.Id, Type = VehicleType.Motorcycle, Plate = "ABC1234" };
        var document = new CourierDocument
        {
            CourierId = courier.Id,
            Type = CourierDocumentType.DriverLicense,
            Status = CourierDocumentStatus.Approved,
            StorageKey = "old",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(10)
        };
        db.Users.AddRange(admin, owner); db.Couriers.Add(courier); db.Vehicles.Add(vehicle); db.CourierDocuments.Add(document);
        db.SecurityTokens.Add(new SecurityToken { UserId = owner.Id, Hash = "test", Purpose = "refresh", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();
        var sessions = new SessionService(db, Options.Create(new JwtOptions()), Options.Create(new SessionOptions()), new Mailer());
        var reviews = new RegistrationReviewService(db, sessions);
        var directory = Path.Combine(Path.GetTempPath(), "coletas-review-" + Guid.NewGuid().ToString("N"));
        var profiles = new ProfileService(db, reviews);
        var documents = new CourierDocumentService(db, new PrivateDocumentStore(Options.Create(new PrivateDocumentOptions { RootPath = directory })), reviews);
        try
        {
            switch (change)
            {
                case "metadata":
                    Assert.True((await new IdentityService(db, sessions, reviews).AddCourierDocumentAsync(admin.Id, UserRole.Admin,
                        courier.Id, new(CourierDocumentType.VehicleRegistration, DateTimeOffset.UtcNow.AddDays(20)), default)).IsSuccess);
                    break;
                case "vehicle":
                    Assert.True((await profiles.EditVehicleAsync(admin.Id, courier.Id, vehicle.Id, new(VehicleType.Car, "DEF1G23"), default)).IsSuccess);
                    break;
                case "upload":
                    using (var stream = new MemoryStream("%PDF-test-content"u8.ToArray()))
                        Assert.True((await documents.UploadAsync(admin.Id, courier.Id, CourierDocumentType.DriverLicense, DateTimeOffset.UtcNow.AddDays(20), stream, default)).IsSuccess);
                    break;
                case "decision":
                    Assert.True((await documents.DecideDocumentAsync(admin.Id, document.Id, new(CourierDocumentStatus.Rejected, "Ilegível"), default)).IsSuccess);
                    break;
            }
            db.ChangeTracker.Clear();
            var saved = await db.Users.SingleAsync(x => x.Id == owner.Id);
            Assert.Equal(8, saved.ReviewVersion);
            Assert.Equal(ReviewStatus.Pending, saved.ReviewStatus);
            Assert.Equal(blocked ? UserStatus.Blocked : UserStatus.Pending, saved.Status);
            var profile = await profiles.GetAsync(owner.Id, default);
            Assert.NotNull(profile);
            Assert.Equal(saved.Status, profile.Status);
            Assert.Equal(saved.StatusReason, profile.Reason);
            Assert.Single(profile.Vehicles);
            Assert.NotEmpty(profile.Documents);
            Assert.Null(await profiles.GetAsync(Guid.NewGuid(), default));
            if (blocked) Assert.Equal("Bloqueio administrativo", saved.StatusReason);
            Assert.True((await db.SecurityTokens.SingleAsync()).Used);
            var history = await db.ReviewEvents.SingleAsync();
            Assert.Equal("Invalidate", history.Action);
            Assert.Equal(admin.Id, history.ActorId);
            Assert.Equal(8, history.Version);
            Assert.Equal(409, (await reviews.DecideAsync(admin.Id, owner.Id, new(7, ReviewAction.Approve, "Versão antiga"), default)).StatusCode);
        }
        finally
        {
            // Somente arquivos da pasta aleatória criada por este teste; não percorre outros diretórios.
            if (Directory.Exists(directory))
            {
                foreach (var file in Directory.GetFiles(directory)) File.Delete(file);
                Directory.Delete(directory);
            }
        }
    }

    [Fact]
    public async Task UnchangedVehicleDoesNotInvalidateReview()
    {
        await using var db = Database();
        var owner = NewUser(UserRole.Courier, UserStatus.Active);
        owner.ReviewStatus = ReviewStatus.Approved;
        var courier = new Courier { UserId = owner.Id, FullName = "Teste", PhoneWhatsApp = "65999999999" };
        var vehicle = new Vehicle { CourierId = courier.Id, Type = VehicleType.Motorcycle, Plate = "ABC1234" };
        db.Users.Add(owner); db.Couriers.Add(courier); db.Vehicles.Add(vehicle); await db.SaveChangesAsync();
        var profiles = new ProfileService(db, Service(db));
        Assert.True((await profiles.EditVehicleAsync(owner.Id, courier.Id, vehicle.Id, new(VehicleType.Motorcycle, "abc-1234"), default)).IsSuccess);
        Assert.Equal(ReviewStatus.Approved, owner.ReviewStatus);
        Assert.Equal(0, owner.ReviewVersion);
        Assert.Empty(db.ReviewEvents);
    }

    [Theory]
    [InlineData("metadata")]
    [InlineData("vehicle")]
    [InlineData("upload")]
    [InlineData("decision")]
    [InlineData("unauthorized-vehicle")]
    [InlineData("unauthorized-upload")]
    [InlineData("unauthorized-decision")]
    [InlineData("missing-vehicle")]
    [InlineData("missing-document")]
    [InlineData("expired-document")]
    public async Task RejectedChangesLeaveApprovalAndSessionUntouched(string change)
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active);
        var owner = NewUser(UserRole.Courier, UserStatus.Active);
        owner.ReviewStatus = ReviewStatus.Approved;
        var courier = new Courier { UserId = owner.Id, FullName = "Teste", PhoneWhatsApp = "65999999999" };
        var vehicle = new Vehicle { CourierId = courier.Id, Type = VehicleType.Motorcycle, Plate = "ABC1234" };
        var document = new CourierDocument
        {
            CourierId = courier.Id,
            Type = CourierDocumentType.DriverLicense,
            Status = CourierDocumentStatus.Pending,
            StorageKey = "file",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        db.Users.AddRange(admin, owner); db.Couriers.Add(courier); db.Vehicles.Add(vehicle); db.CourierDocuments.Add(document);
        db.SecurityTokens.Add(new SecurityToken { UserId = owner.Id, Hash = "test", Purpose = "refresh", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();
        var sessions = new SessionService(db, Options.Create(new JwtOptions()), Options.Create(new SessionOptions()), new Mailer());
        var reviews = new RegistrationReviewService(db, sessions);
        var profiles = new ProfileService(db, reviews);
        var documents = new CourierDocumentService(db, new PrivateDocumentStore(Options.Create(new PrivateDocumentOptions())), reviews);
        using var stream = new MemoryStream();
        var status = change switch
        {
            "metadata" => (await new IdentityService(db, sessions, reviews).AddCourierDocumentAsync(admin.Id, UserRole.Admin,
                courier.Id, new((CourierDocumentType)99, null), default)).StatusCode,
            "vehicle" => (await profiles.EditVehicleAsync(admin.Id, courier.Id, vehicle.Id, new(VehicleType.Car, "invalid"), default)).StatusCode,
            "upload" => (await documents.UploadAsync(admin.Id, courier.Id, (CourierDocumentType)99, null, stream, default)).StatusCode,
            "decision" => (await documents.DecideDocumentAsync(admin.Id, document.Id, new((CourierDocumentStatus)99, "Teste"), default)).StatusCode,
            "unauthorized-vehicle" => (await profiles.EditVehicleAsync(Guid.NewGuid(), courier.Id, vehicle.Id, new(VehicleType.Car, "DEF1G23"), default)).StatusCode,
            "unauthorized-upload" => (await documents.UploadAsync(Guid.NewGuid(), courier.Id, CourierDocumentType.DriverLicense, null, stream, default)).StatusCode,
            "unauthorized-decision" => (await documents.DecideDocumentAsync(owner.Id, document.Id, new(CourierDocumentStatus.Rejected, "Teste"), default)).StatusCode,
            "missing-vehicle" => (await profiles.EditVehicleAsync(admin.Id, courier.Id, Guid.NewGuid(), new(VehicleType.Car, "DEF1G23"), default)).StatusCode,
            "missing-document" => (await documents.DecideDocumentAsync(admin.Id, Guid.NewGuid(), new(CourierDocumentStatus.Rejected, "Teste"), default)).StatusCode,
            _ => (await documents.DecideDocumentAsync(admin.Id, document.Id, new(CourierDocumentStatus.Approved, "Conferido"), default)).StatusCode
        };
        Assert.Equal(change.StartsWith("unauthorized", StringComparison.Ordinal) ? 403
            : change.StartsWith("missing", StringComparison.Ordinal) ? 404 : 400, status);
        Assert.Equal(0, owner.ReviewVersion);
        Assert.Equal(ReviewStatus.Approved, owner.ReviewStatus);
        Assert.Equal(UserStatus.Active, owner.Status);
        Assert.False((await db.SecurityTokens.SingleAsync()).Used);
        Assert.Empty(db.ReviewEvents);
    }

    [Fact]
    public async Task DocumentDecisionUsesNeutralMessageWhenTheAdministratorDoesNotWriteOne()
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active); var owner = NewUser(UserRole.Courier);
        var courier = new Courier { UserId = owner.Id, FullName = "Teste", Cpf = "52998224725", PhoneWhatsApp = "65999999999" };
        var document = new CourierDocument { CourierId = courier.Id, Type = CourierDocumentType.DriverLicense, Status = CourierDocumentStatus.UnderReview, StorageKey = "file", ExpiresAt = DateTimeOffset.UtcNow.AddDays(20) };
        db.Users.AddRange(admin, owner); db.Couriers.Add(courier); db.CourierDocuments.Add(document); await db.SaveChangesAsync();
        var documents = new CourierDocumentService(db, new PrivateDocumentStore(Options.Create(new PrivateDocumentOptions())), Service(db));

        Assert.True((await documents.DecideDocumentAsync(admin.Id, document.Id, new(CourierDocumentStatus.Approved, null), default)).IsSuccess);
        Assert.Equal("Documento aprovado.", (await db.CourierDocuments.SingleAsync()).ReviewReason);
    }

    [Fact]
    public async Task TrackedOldReviewCannotOverwriteDocumentInvalidation()
    {
        var options = new DbContextOptionsBuilder<ColetasDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var changes = new ColetasDbContext(options);
        var admin = NewUser(UserRole.Admin, UserStatus.Active);
        var owner = NewUser(UserRole.Courier);
        owner.ReviewStatus = ReviewStatus.InReview;
        var courier = new Courier { UserId = owner.Id, FullName = "Teste", Cpf = "52998224725", PhoneWhatsApp = "65999999999" };
        changes.Users.AddRange(admin, owner); changes.Couriers.Add(courier); await changes.SaveChangesAsync();
        await using var oldReview = new ColetasDbContext(options);
        await oldReview.Users.SingleAsync(x => x.Id == owner.Id);
        var sessions = new SessionService(changes, Options.Create(new JwtOptions()), Options.Create(new SessionOptions()), new Mailer());
        var identity = new IdentityService(changes, sessions, new RegistrationReviewService(changes, sessions));
        Assert.True((await identity.AddCourierDocumentAsync(admin.Id, UserRole.Admin, courier.Id,
            new(CourierDocumentType.DriverLicense, DateTimeOffset.UtcNow.AddDays(20)), default)).IsSuccess);
        // Contexto administrativo mantém a versão anterior: o token EF deve impedir a gravação obsoleta.
        Assert.Equal(409, (await Service(oldReview).DecideAsync(admin.Id, owner.Id,
            new(0, ReviewAction.Reject, "Decisão antiga"), default)).StatusCode);
        changes.ChangeTracker.Clear();
        Assert.Equal(ReviewStatus.Pending, (await changes.Users.SingleAsync(x => x.Id == owner.Id)).ReviewStatus);
    }

    [Fact]
    public async Task LoginIssuesTokenWithPersistedSessionAndAccessScope()
    {
        await using var db = Database();
        var user = NewUser(UserRole.Establishment, UserStatus.Active);
        const string password = "Test-only-password-123";
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        db.Users.Add(user); await db.SaveChangesAsync();
        var settings = Options.Create(new JwtOptions { ExpirationMinutes = 15, SigningKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) });
        var sessions = new SessionService(db, settings, Options.Create(new SessionOptions()), new Mailer());
        var identity = new IdentityService(db, sessions, new RegistrationReviewService(db, sessions));
        var response = await identity.LoginAsync(new(user.Email, password), default);
        Assert.True(response.IsSuccess);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(response.Value!.AccessToken);
        Assert.Equal("access", jwt.Claims.Single(x => x.Type == "scope").Value);
        Assert.Equal((await db.SecurityTokens.SingleAsync()).Id.ToString(), jwt.Claims.Single(x => x.Type == "sid").Value);
    }

    [Theory]
    [InlineData(ReviewStatus.Pending, ReviewAction.Approve, false)]
    [InlineData(ReviewStatus.Pending, ReviewAction.Start, true)]
    [InlineData(ReviewStatus.InReview, ReviewAction.Approve, true)]
    [InlineData(ReviewStatus.InReview, ReviewAction.Reject, true)]
    [InlineData(ReviewStatus.InReview, ReviewAction.RequestCorrection, true)]
    [InlineData(ReviewStatus.Approved, ReviewAction.Start, false)]
    [InlineData(ReviewStatus.NeedsCorrection, ReviewAction.Start, true)]
    [InlineData(ReviewStatus.Rejected, ReviewAction.Approve, false)]
    [InlineData(ReviewStatus.Rejected, ReviewAction.Reopen, true)]
    [InlineData(ReviewStatus.Pending, ReviewAction.Reopen, false)]
    public void TransitionsAreExplicit(ReviewStatus status, ReviewAction action, bool expected)
        => Assert.Equal(expected, RegistrationReview.CanTransition(status, action));

    [Fact]
    public async Task ReopeningRejectedRegistrationPreservesHistoryAndAllowsApproval()
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active); var subject = NewUser(UserRole.Establishment);
        subject.ReviewStatus = ReviewStatus.Rejected;
        db.Users.AddRange(admin, subject); await db.SaveChangesAsync();
        var service = Service(db);

        Assert.True((await service.DecideAsync(admin.Id, subject.Id, new(0, ReviewAction.Reopen, null), default)).IsSuccess);
        Assert.Equal(ReviewStatus.InReview, subject.ReviewStatus);
        Assert.Equal(UserStatus.Pending, subject.Status);
        Assert.Equal("Cadastro reaberto para nova análise.", (await db.ReviewEvents.SingleAsync()).PublicReason);
        Assert.True((await service.DecideAsync(admin.Id, subject.Id, new(1, ReviewAction.Approve, null), default)).IsSuccess);
        Assert.Equal(ReviewStatus.Approved, subject.ReviewStatus);
        Assert.Equal(2, await db.ReviewEvents.CountAsync());
    }

    [Theory]
    [InlineData(UserRole.Courier)]
    [InlineData(UserRole.Establishment)]
    [InlineData(UserRole.Operator)]
    public async Task NonAdminCannotListOrDecide(UserRole role)
    {
        await using var db = Database();
        var actor = NewUser(role, UserStatus.Active); var subject = NewUser(UserRole.Establishment);
        db.Users.AddRange(actor, subject); await db.SaveChangesAsync();
        Assert.Equal(403, (await Service(db).ListAsync(actor.Id, null, null, 1, 20, default)).StatusCode);
        Assert.Equal(403, (await Service(db).DecideAsync(actor.Id, subject.Id, new(0, ReviewAction.Start, "Conferir"), default)).StatusCode);
        Assert.Empty(db.ReviewEvents);
    }

    [Fact]
    public async Task StaleDecisionIsRejectedAndInternalNoteIsPrivate()
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active); var subject = NewUser(UserRole.Establishment);
        db.Users.AddRange(admin, subject); await db.SaveChangesAsync();
        var service = Service(db);
        Assert.True((await service.DecideAsync(admin.Id, subject.Id, new(0, ReviewAction.Start, "Em análise", "Nota restrita"), default)).IsSuccess);
        Assert.Equal(409, (await service.DecideAsync(admin.Id, subject.Id, new(0, ReviewAction.Approve, "Conferido"), default)).StatusCode);
        Assert.Single(db.ReviewEvents);
        var own = (await service.GetAsync(subject.Id, subject.Id, default)).Value!;
        Assert.Null(own.History.Single().InternalNote);
        Assert.Null(own.History.Single().ActorId);
        Assert.Equal("Nota restrita", (await service.GetAsync(admin.Id, subject.Id, default)).Value!.History.Single().InternalNote);
        Assert.Equal(403, (await service.GetAsync(Guid.NewGuid(), subject.Id, default)).StatusCode);
    }

    [Fact]
    public async Task DecisionsUseNeutralMessageWhenTheAdministratorDoesNotWriteOne()
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active); var subject = NewUser(UserRole.Establishment);
        db.Users.AddRange(admin, subject); await db.SaveChangesAsync();
        Assert.True((await Service(db).DecideAsync(admin.Id, subject.Id, new(0, ReviewAction.Start, null, "Nota interna"), default)).IsSuccess);
        var review = await db.ReviewEvents.SingleAsync();
        Assert.Equal("Cadastro em análise.", review.PublicReason);
        Assert.Equal("Nota interna", review.InternalNote);
    }

    [Fact]
    public async Task ApprovalUsesNeutralMessageWhenTheAdministratorDoesNotWriteOne()
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active); var subject = NewUser(UserRole.Establishment);
        subject.ReviewStatus = ReviewStatus.InReview;
        db.Users.AddRange(admin, subject); await db.SaveChangesAsync();
        Assert.True((await Service(db).DecideAsync(admin.Id, subject.Id, new(0, ReviewAction.Approve, null), default)).IsSuccess);
        Assert.Equal("Cadastro aprovado.", (await db.ReviewEvents.SingleAsync()).PublicReason);
    }

    [Fact]
    public async Task BlockPreservesApprovalAndRevokesExistingSessions()
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active); var subject = NewUser(UserRole.Establishment, UserStatus.Active);
        subject.ReviewStatus = ReviewStatus.Approved;
        db.Users.AddRange(admin, subject);
        db.SecurityTokens.Add(new SecurityToken { UserId = subject.Id, Hash = "hash", Purpose = "refresh", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();
        Assert.True((await Service(db).DecideAsync(admin.Id, subject.Id, new(0, ReviewAction.Block, "Suspenso"), default)).IsSuccess);
        Assert.Equal(ReviewStatus.Approved, subject.ReviewStatus);
        Assert.True((await db.SecurityTokens.SingleAsync()).Used);
        Assert.True((await Service(db).DecideAsync(admin.Id, subject.Id, new(1, ReviewAction.Unblock, "Regularizado"), default)).IsSuccess);
        Assert.Equal(UserStatus.Active, subject.Status);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    [InlineData("replacement")]
    [InlineData("no-expiry")]
    public async Task CourierApprovalRequiresCurrentValidFiles(string scenario)
    {
        await using var db = Database();
        var admin = NewUser(UserRole.Admin, UserStatus.Active); var subject = NewUser(UserRole.Courier);
        subject.ReviewStatus = ReviewStatus.InReview;
        var courier = new Courier { UserId = subject.Id, FullName = "Teste", PhoneWhatsApp = "65999999999", Cpf = "52998224725" };
        db.Users.AddRange(admin, subject); db.Couriers.Add(courier);
        foreach (var type in Enum.GetValues<CourierDocumentType>())
            db.CourierDocuments.Add(new CourierDocument
            {
                CourierId = courier.Id,
                Type = type,
                Status = CourierDocumentStatus.Approved,
                StorageKey = scenario == "missing" ? null : "file",
                ExpiresAt = scenario == "no-expiry" ? null : DateTimeOffset.UtcNow.AddDays(scenario == "expired" ? -1 : 20),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            });
        if (scenario == "replacement") db.CourierDocuments.Add(new CourierDocument
        {
            CourierId = courier.Id,
            Type = CourierDocumentType.DriverLicense,
            Status = CourierDocumentStatus.UnderReview,
            StorageKey = "new-file",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(20)
        });
        await db.SaveChangesAsync();
        Assert.Equal(400, (await Service(db).DecideAsync(admin.Id, subject.Id, new(0, ReviewAction.Approve, "Conferido"), default)).StatusCode);
        Assert.Empty(db.ReviewEvents);
    }
}
