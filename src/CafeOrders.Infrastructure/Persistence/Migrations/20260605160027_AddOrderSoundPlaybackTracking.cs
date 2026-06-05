using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOrders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSoundPlaybackTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSoundPlayed",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SoundPlayedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSoundPlayed",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SoundPlayedAt",
                table: "Orders");
        }
    }
}
