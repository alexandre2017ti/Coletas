using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coletas.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class UniqueRegistrationIdentifiers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Normaliza legado antes dos índices; colisões exigem revisão sem expor contatos no erro.
        // Mudanças: docs/mudancas/2026-09-14-03-identificadores-exclusivos-empresa.md e 2026-09-14-04-identificadores-exclusivos-entregador.md
        foreach (var table in new[] { "establishments.\"Establishments\"", "couriers.\"Couriers\"" })
        {
            migrationBuilder.Sql($$"""
                DO $body$
                BEGIN
                  IF EXISTS (
                    SELECT 1 FROM (
                      SELECT CASE WHEN length(p) IN (12,13) AND left(p,2)='55' THEN substring(p FROM 3) ELSE p END AS phone
                      FROM (SELECT regexp_replace("PhoneWhatsApp", '[[:space:]()+-]', '', 'g') AS p FROM {{table}}) raw
                    ) normalized GROUP BY phone HAVING count(*) > 1
                  ) THEN RAISE EXCEPTION 'Telefones duplicados no legado: revisar cadastros antes de aplicar exclusividade.';
                  END IF;
                END $body$;
                UPDATE {{table}} SET "PhoneWhatsApp" = CASE
                  WHEN length(regexp_replace("PhoneWhatsApp", '[[:space:]()+-]', '', 'g')) IN (12,13)
                    AND left(regexp_replace("PhoneWhatsApp", '[[:space:]()+-]', '', 'g'),2)='55'
                  THEN substring(regexp_replace("PhoneWhatsApp", '[[:space:]()+-]', '', 'g') FROM 3)
                  ELSE regexp_replace("PhoneWhatsApp", '[[:space:]()+-]', '', 'g') END;
                """);
        }
        migrationBuilder.AddColumn<string>(
            name: "Cpf",
            schema: "couriers",
            table: "Couriers",
            type: "character varying(11)",
            maxLength: 11,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Establishments_PhoneWhatsApp",
            schema: "establishments",
            table: "Establishments",
            column: "PhoneWhatsApp",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Couriers_Cpf",
            schema: "couriers",
            table: "Couriers",
            column: "Cpf",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Couriers_PhoneWhatsApp",
            schema: "couriers",
            table: "Couriers",
            column: "PhoneWhatsApp",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Establishments_PhoneWhatsApp",
            schema: "establishments",
            table: "Establishments");

        migrationBuilder.DropIndex(
            name: "IX_Couriers_Cpf",
            schema: "couriers",
            table: "Couriers");

        migrationBuilder.DropIndex(
            name: "IX_Couriers_PhoneWhatsApp",
            schema: "couriers",
            table: "Couriers");

        migrationBuilder.DropColumn(
            name: "Cpf",
            schema: "couriers",
            table: "Couriers");
    }
}
