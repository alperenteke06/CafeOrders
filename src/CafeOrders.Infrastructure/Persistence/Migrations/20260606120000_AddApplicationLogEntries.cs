using System;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOrders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CafeOrdersDbContext))]
    [Migration("20260606120000_AddApplicationLogEntries")]
    public partial class AddApplicationLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationLogEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DeviceKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TableId = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationLogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogEntries_CreatedAtUtc",
                table: "ApplicationLogEntries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogEntries_Level_CreatedAtUtc",
                table: "ApplicationLogEntries",
                columns: new[] { "Level", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogEntries_Source_CreatedAtUtc",
                table: "ApplicationLogEntries",
                columns: new[] { "Source", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationLogEntries");
        }
    }
}
