using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coletas.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase1AccessAndRegistrations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "compliance");

        migrationBuilder.EnsureSchema(
            name: "couriers");

        migrationBuilder.EnsureSchema(
            name: "establishments");

        migrationBuilder.EnsureSchema(
            name: "identity");

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Couriers",
            schema: "couriers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                PhoneWhatsApp = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Couriers", x => x.Id);
                table.ForeignKey(
                    name: "FK_Couriers_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Establishments",
            schema: "establishments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                TradeName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                TaxId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                PhoneWhatsApp = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Establishments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Establishments_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CourierDocuments",
            schema: "compliance",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CourierDocuments", x => x.Id);
                table.ForeignKey(
                    name: "FK_CourierDocuments_Couriers_CourierId",
                    column: x => x.CourierId,
                    principalSchema: "couriers",
                    principalTable: "Couriers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Vehicles",
            schema: "couriers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Plate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Vehicles", x => x.Id);
                table.ForeignKey(
                    name: "FK_Vehicles_Couriers_CourierId",
                    column: x => x.CourierId,
                    principalSchema: "couriers",
                    principalTable: "Couriers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CourierDocuments_CourierId_Type_Status",
            schema: "compliance",
            table: "CourierDocuments",
            columns: new[] { "CourierId", "Type", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_Couriers_UserId",
            schema: "couriers",
            table: "Couriers",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Establishments_TaxId",
            schema: "establishments",
            table: "Establishments",
            column: "TaxId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Establishments_UserId",
            schema: "establishments",
            table: "Establishments",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            schema: "identity",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Vehicles_CourierId",
            schema: "couriers",
            table: "Vehicles",
            column: "CourierId");

        migrationBuilder.CreateIndex(
            name: "IX_Vehicles_Plate",
            schema: "couriers",
            table: "Vehicles",
            column: "Plate",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CourierDocuments",
            schema: "compliance");

        migrationBuilder.DropTable(
            name: "Establishments",
            schema: "establishments");

        migrationBuilder.DropTable(
            name: "Vehicles",
            schema: "couriers");

        migrationBuilder.DropTable(
            name: "Couriers",
            schema: "couriers");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "identity");
    }
}
