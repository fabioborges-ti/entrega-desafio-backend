using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class SeedBranchesByAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CreatedByUserId = primeiro usuário com Role 'Admin' (menor Id), FK Branches -> Users.
            // Idempotente por CNPJ único. Se não existir Admin, nenhuma linha é inserida.
            migrationBuilder.Sql(
                """
                INSERT INTO "Branches" ("Name", "Cnpj", "CreatedByUserId", "CreatedAt", "LastModifiedAt")
                SELECT 'Filial Matriz Sao Paulo', '92100045000181', a."Id",
                    TIMESTAMPTZ '2026-04-26T20:15:00Z', TIMESTAMPTZ '2026-04-26T20:15:00Z'
                FROM (SELECT "Id" FROM "Users" WHERE "Role" = 'Admin' ORDER BY "Id" ASC LIMIT 1) AS a
                WHERE EXISTS (SELECT 1 FROM "Users" WHERE "Role" = 'Admin')
                  AND NOT EXISTS (SELECT 1 FROM "Branches" WHERE "Cnpj" = '92100045000181');

                INSERT INTO "Branches" ("Name", "Cnpj", "CreatedByUserId", "CreatedAt", "LastModifiedAt")
                SELECT 'Filial Rio de Janeiro', '92100076000205', a."Id",
                    TIMESTAMPTZ '2026-04-26T20:15:01Z', TIMESTAMPTZ '2026-04-26T20:15:01Z'
                FROM (SELECT "Id" FROM "Users" WHERE "Role" = 'Admin' ORDER BY "Id" ASC LIMIT 1) AS a
                WHERE EXISTS (SELECT 1 FROM "Users" WHERE "Role" = 'Admin')
                  AND NOT EXISTS (SELECT 1 FROM "Branches" WHERE "Cnpj" = '92100076000205');

                INSERT INTO "Branches" ("Name", "Cnpj", "CreatedByUserId", "CreatedAt", "LastModifiedAt")
                SELECT 'Filial Belo Horizonte', '92100096000362', a."Id",
                    TIMESTAMPTZ '2026-04-26T20:15:02Z', TIMESTAMPTZ '2026-04-26T20:15:02Z'
                FROM (SELECT "Id" FROM "Users" WHERE "Role" = 'Admin' ORDER BY "Id" ASC LIMIT 1) AS a
                WHERE EXISTS (SELECT 1 FROM "Users" WHERE "Role" = 'Admin')
                  AND NOT EXISTS (SELECT 1 FROM "Branches" WHERE "Cnpj" = '92100096000362');

                INSERT INTO "Branches" ("Name", "Cnpj", "CreatedByUserId", "CreatedAt", "LastModifiedAt")
                SELECT 'Filial Curitiba', '92100080000162', a."Id",
                    TIMESTAMPTZ '2026-04-26T20:15:03Z', TIMESTAMPTZ '2026-04-26T20:15:03Z'
                FROM (SELECT "Id" FROM "Users" WHERE "Role" = 'Admin' ORDER BY "Id" ASC LIMIT 1) AS a
                WHERE EXISTS (SELECT 1 FROM "Users" WHERE "Role" = 'Admin')
                  AND NOT EXISTS (SELECT 1 FROM "Branches" WHERE "Cnpj" = '92100080000162');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Branches"
                WHERE "Cnpj" IN (
                    '92100045000181',
                    '92100076000205',
                    '92100096000362',
                    '92100080000162'
                );
                """);
        }
    }
}
