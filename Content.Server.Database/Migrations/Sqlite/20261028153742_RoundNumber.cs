using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RoundNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_round_server_id",
                table: "round");

            migrationBuilder.AddColumn<int>(
                name: "number",
                table: "round",
                type: "INTEGER",
            // Nebula edit start
                nullable: true);

            migrationBuilder.Sql("UPDATE round SET number = round_id;");

            migrationBuilder.AlterColumn<int>(
                name: "number",
                table: "round",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
            // Nebula edit end
            migrationBuilder.CreateIndex(
                name: "IX_round_server_id_number",
                table: "round",
                columns: new[] { "server_id", "number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_round_server_id_number",
                table: "round");

            migrationBuilder.DropColumn(
                name: "number",
                table: "round");

            migrationBuilder.CreateIndex(
                name: "IX_round_server_id",
                table: "round",
                column: "server_id");
        }
    }
}
