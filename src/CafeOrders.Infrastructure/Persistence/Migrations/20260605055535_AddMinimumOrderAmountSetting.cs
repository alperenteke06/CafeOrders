using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOrders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMinimumOrderAmountSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumOrderAmount",
                table: "AppSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumOrderAmount",
                table: "AppSettings");
        }
    }
}
