using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoUsersThreeRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Senhas em BCrypt (alinhado a BCryptPasswordHasher). Idempotente por Username.
            migrationBuilder.Sql(
                """
                INSERT INTO "Users" (
                    "Username", "NameFirstName", "NameLastName",
                    "AddressCity", "AddressStreet", "AddressNumber", "AddressZipcode", "AddressGeoLat", "AddressGeoLong",
                    "Email", "Phone", "Password", "Role", "Status", "CreatedAt", "UpdatedAt"
                )
                SELECT 'fabioborges', 'Fabio', 'Nascimento',
                    'São Paulo', 'Av. Brigadeiro Faria Lima', 3477, '04538-133', '-23.587416', '-46.657634',
                    'fabioborges.ti@gmail.com.br', '(11) 98123-7744',
                    $pwd1$$2a$11$OeQrer5Qqj6O9zBlPg9md.zUdoCdGyuNuc71l35IbVxLJBb2n.QKG$pwd1$,
                    'Admin', 'Active', TIMESTAMPTZ '2026-04-26T18:00:00Z', NULL
                WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'fabioborges');

                INSERT INTO "Users" (
                    "Username", "NameFirstName", "NameLastName",
                    "AddressCity", "AddressStreet", "AddressNumber", "AddressZipcode", "AddressGeoLat", "AddressGeoLong",
                    "Email", "Phone", "Password", "Role", "Status", "CreatedAt", "UpdatedAt"
                )
                SELECT 'pauloroberto', 'Paulo', 'Nascimento',
                    'Rio de Janeiro', 'Rua Barata Ribeiro', 502, '22051-001', '-22.971106', '-43.182230',
                    'paulo@teste.com.br', '(21) 98765-2211',
                    $pwd2$$2a$11$lzQCkW7qGeOkoARib7gnR.UUkrrdf86TDj30NOnVnxdGG5Qi4RHH.$pwd2$,
                    'Manager', 'Active', TIMESTAMPTZ '2026-04-26T18:00:01Z', NULL
                WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'pauloroberto');

                INSERT INTO "Users" (
                    "Username", "NameFirstName", "NameLastName",
                    "AddressCity", "AddressStreet", "AddressNumber", "AddressZipcode", "AddressGeoLat", "AddressGeoLong",
                    "Email", "Phone", "Password", "Role", "Status", "CreatedAt", "UpdatedAt"
                )
                SELECT 'alinedeus', 'Aline', 'Nascimento',
                    'Belo Horizonte', 'Av. Afonso Pena', 1500, '30130-007', '-19.920800', '-43.937800',
                    'aline@teste.com.br', '(31) 99234-8890',
                    $pwd3$$2a$11$3cqnvuDw0Fo0cJ/e00f5JuxpzwYN2ERZq3FqFgVc1LXLpBCpgZxN2$pwd3$,
                    'Customer', 'Active', TIMESTAMPTZ '2026-04-26T18:00:02Z', NULL
                WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'alinedeus');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Users"
                WHERE "Username" IN ('fabioborges', 'pauloroberto', 'alinedeus');
                """);
        }
    }
}
