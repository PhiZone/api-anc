using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class EventTeamUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "EventTeams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "EventTeams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EventTeams_DivisionId",
                table: "EventTeams",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventTeams_EventDivisions_DivisionId",
                table: "EventTeams",
                column: "DivisionId",
                principalTable: "EventDivisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventTeams_EventDivisions_DivisionId",
                table: "EventTeams");

            migrationBuilder.DropIndex(
                name: "IX_EventTeams_DivisionId",
                table: "EventTeams");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "EventTeams");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "EventTeams");
        }
    }
}
