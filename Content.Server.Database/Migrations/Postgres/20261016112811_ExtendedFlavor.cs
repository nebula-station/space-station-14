using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ExtendedFlavor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "flavor_text",
                table: "profile");

            migrationBuilder.AddColumn<JsonDocument>(
                name: "detail_examinable",
                table: "profile",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "detail_examinable",
                table: "profile");

            migrationBuilder.AddColumn<string>(
                name: "flavor_text",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
