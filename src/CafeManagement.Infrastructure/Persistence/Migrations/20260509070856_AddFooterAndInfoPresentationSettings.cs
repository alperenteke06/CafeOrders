using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFooterAndInfoPresentationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "InfoMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AppDeveloperName",
                table: "AppSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AppDeveloperPhone",
                table: "AppSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientInfoBoxIcon",
                table: "AppSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ClientInfoBoxType",
                table: "AppSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "InfoMessages");

            migrationBuilder.DropColumn(
                name: "AppDeveloperName",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AppDeveloperPhone",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "ClientInfoBoxIcon",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "ClientInfoBoxType",
                table: "AppSettings");
        }
    }
}
