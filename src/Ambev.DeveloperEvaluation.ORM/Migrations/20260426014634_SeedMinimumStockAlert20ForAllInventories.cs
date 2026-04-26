using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class SeedMinimumStockAlert20ForAllInventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Define MinimumStockAlert = 20 para todos os registros de estoque
            // que ainda estão com o valor padrão (0 = alerta desativado).
            migrationBuilder.Sql(
                "UPDATE \"Inventories\" SET \"MinimumStockAlert\" = 20 WHERE \"MinimumStockAlert\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverte: volta todos os registros com MinimumStockAlert = 20 para 0 (desativado).
            migrationBuilder.Sql(
                "UPDATE \"Inventories\" SET \"MinimumStockAlert\" = 0 WHERE \"MinimumStockAlert\" = 20;");
        }
    }
}
