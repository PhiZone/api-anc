using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class EventFieldRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Preserved",
                table: "EventTeams",
                newName: "Reserved");

            migrationBuilder.RenameColumn(
                name: "Preserved",
                table: "EventResources",
                newName: "Reserved");

            migrationBuilder.RenameColumn(
                name: "Preserved",
                table: "EventDivisions",
                newName: "Reserved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Reserved",
                table: "EventTeams",
                newName: "Preserved");

            migrationBuilder.RenameColumn(
                name: "Reserved",
                table: "EventResources",
                newName: "Preserved");

            migrationBuilder.RenameColumn(
                name: "Reserved",
                table: "EventDivisions",
                newName: "Preserved");
        }
    }
}
