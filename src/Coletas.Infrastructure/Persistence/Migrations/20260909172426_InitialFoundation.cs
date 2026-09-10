using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coletas.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialFoundation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "configuration");

        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:postgis", ",,");

        migrationBuilder.CreateTable(
            name: "SystemSettings",
            schema: "configuration",
            columns: table => new
            {
                Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SystemSettings", x => x.Key);
            });

        migrationBuilder.InsertData(
            schema: "configuration",
            table: "SystemSettings",
            columns: new[] { "Key", "Value" },
            values: new object[] { "DeliveryGrouping.RadiusMeters", "5000" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SystemSettings",
            schema: "configuration");
    }
}
