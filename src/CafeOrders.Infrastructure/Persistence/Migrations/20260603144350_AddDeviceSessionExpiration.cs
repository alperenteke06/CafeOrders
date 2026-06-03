using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOrders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceSessionExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SessionExpiresAtUtc",
                table: "Devices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionExpiresAtUtc",
                table: "Devices");
        }
    }
}
