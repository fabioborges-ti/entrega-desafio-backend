using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class SeedLowStockFiveRandomItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH picked AS (
                    SELECT "Id"
                    FROM "Inventories"
                    ORDER BY random()
                    LIMIT 5
                )
                UPDATE "Inventories" i
                SET
                    "AvailableQuantity" = floor(random() * 19 + 1)::int,
                    "MinimumStockAlert" = 20
                FROM picked
                WHERE i."Id" = picked."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Inventories"
                SET "AvailableQuantity" = 20
                WHERE "AvailableQuantity" < 20
                  AND "MinimumStockAlert" = 20;
                """);
        }
    }
}
