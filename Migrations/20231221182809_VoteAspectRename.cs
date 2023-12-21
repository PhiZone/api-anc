using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class VoteAspectRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Feel",
                table: "Votes",
                newName: "Gameplay");

            migrationBuilder.RenameColumn(
                name: "RatingOnFeel",
                table: "Charts",
                newName: "RatingOnGameplay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Gameplay",
                table: "Votes",
                newName: "Feel");

            migrationBuilder.RenameColumn(
                name: "RatingOnGameplay",
                table: "Charts",
                newName: "RatingOnFeel");
        }
    }
}
