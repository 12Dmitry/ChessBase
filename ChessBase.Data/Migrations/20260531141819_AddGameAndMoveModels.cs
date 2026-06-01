using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessBase.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameAndMoveModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "Games");
        }
    }
}
