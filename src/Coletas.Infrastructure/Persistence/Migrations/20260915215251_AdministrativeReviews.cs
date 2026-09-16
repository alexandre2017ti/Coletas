using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coletas.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AdministrativeReviews : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ReviewStatus",
            schema: "identity",
            table: "Users",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "Pending");

        migrationBuilder.AddColumn<long>(
            name: "ReviewVersion",
            schema: "identity",
            table: "Users",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        // Preserva aprovações anteriores sem confundir bloqueio com reprovação.
        // Mudança: docs/mudancas/2026-09-15-01-analise-administrativa.md
        migrationBuilder.Sql("UPDATE identity.\"Users\" SET \"ReviewStatus\" = 'Approved' WHERE \"Status\" = 'Active';");

        migrationBuilder.CreateTable(
            name: "ReviewEvents",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false),
                Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                PublicReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                InternalNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReviewEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReviewEvents_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ReviewEvents_UserId_Version",
            schema: "identity",
            table: "ReviewEvents",
            columns: new[] { "UserId", "Version" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ReviewEvents",
            schema: "identity");

        migrationBuilder.DropColumn(
            name: "ReviewStatus",
            schema: "identity",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "ReviewVersion",
            schema: "identity",
            table: "Users");
    }
}
