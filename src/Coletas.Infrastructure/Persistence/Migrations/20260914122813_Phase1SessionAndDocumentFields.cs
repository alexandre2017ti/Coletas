using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coletas.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase1SessionAndDocumentFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Motivo: sincronizar o banco com campos da Fase 1 já usados pelo modelo,
        // preservando cadastros existentes com colunas opcionais ou valores iniciais.
        // Mudança: docs/mudancas/2026-09-14-02-cadastro-postgres-real.md
        migrationBuilder.AddColumn<long>(
            name: "SecurityVersion",
            schema: "identity",
            table: "Users",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "StatusReason",
            schema: "identity",
            table: "Users",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "ContentLength",
            schema: "compliance",
            table: "CourierDocuments",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "ContentType",
            schema: "compliance",
            table: "CourierDocuments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReviewReason",
            schema: "compliance",
            table: "CourierDocuments",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StorageKey",
            schema: "compliance",
            table: "CourierDocuments",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "IdentityAudits",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                Detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdentityAudits", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SecurityTokens",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Used = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecurityTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecurityTokens_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SecurityTokens_Hash",
            schema: "identity",
            table: "SecurityTokens",
            column: "Hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecurityTokens_UserId",
            schema: "identity",
            table: "SecurityTokens",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IdentityAudits",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "SecurityTokens",
            schema: "identity");

        migrationBuilder.DropColumn(
            name: "SecurityVersion",
            schema: "identity",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "StatusReason",
            schema: "identity",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "ContentLength",
            schema: "compliance",
            table: "CourierDocuments");

        migrationBuilder.DropColumn(
            name: "ContentType",
            schema: "compliance",
            table: "CourierDocuments");

        migrationBuilder.DropColumn(
            name: "ReviewReason",
            schema: "compliance",
            table: "CourierDocuments");

        migrationBuilder.DropColumn(
            name: "StorageKey",
            schema: "compliance",
            table: "CourierDocuments");
    }
}
