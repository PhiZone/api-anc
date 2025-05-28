using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class EventUpdate5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventResources_Charts_ChartId",
                table: "EventResources");

            migrationBuilder.DropForeignKey(
                name: "FK_EventResources_EventTeams_EventTeamId",
                table: "EventResources");

            migrationBuilder.DropForeignKey(
                name: "FK_EventResources_Songs_SongId",
                table: "EventResources");

            migrationBuilder.DropForeignKey(
                name: "FK_Participations_EventTeams_EventTeamId",
                table: "Participations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Participations",
                table: "Participations");

            migrationBuilder.DropIndex(
                name: "IX_Participations_ParticipantId",
                table: "Participations");

            migrationBuilder.DropIndex(
                name: "IX_EventResources_ChartId",
                table: "EventResources");

            migrationBuilder.DropColumn(
                name: "ChartId",
                table: "EventResources");

            migrationBuilder.RenameColumn(
                name: "EventTeamId",
                table: "Participations",
                newName: "TeamId");

            migrationBuilder.RenameColumn(
                name: "SongId",
                table: "EventResources",
                newName: "TeamId");

            migrationBuilder.RenameColumn(
                name: "EventTeamId",
                table: "EventResources",
                newName: "SignificantResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_EventResources_SongId",
                table: "EventResources",
                newName: "IX_EventResources_TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_EventResources_EventTeamId",
                table: "EventResources",
                newName: "IX_EventResources_SignificantResourceId");

            migrationBuilder.AddColumn<bool>(
                name: "Anonymization",
                table: "EventDivisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TagName",
                table: "EventDivisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Participations",
                table: "Participations",
                columns: new[] { "ParticipantId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_Participations_TeamId",
                table: "Participations",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventResources_EventTeams_TeamId",
                table: "EventResources",
                column: "TeamId",
                principalTable: "EventTeams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Participations_EventTeams_TeamId",
                table: "Participations",
                column: "TeamId",
                principalTable: "EventTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventResources_EventTeams_TeamId",
                table: "EventResources");

            migrationBuilder.DropForeignKey(
                name: "FK_Participations_EventTeams_TeamId",
                table: "Participations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Participations",
                table: "Participations");

            migrationBuilder.DropIndex(
                name: "IX_Participations_TeamId",
                table: "Participations");

            migrationBuilder.DropColumn(
                name: "Anonymization",
                table: "EventDivisions");

            migrationBuilder.DropColumn(
                name: "TagName",
                table: "EventDivisions");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "Participations",
                newName: "EventTeamId");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "EventResources",
                newName: "SongId");

            migrationBuilder.RenameColumn(
                name: "SignificantResourceId",
                table: "EventResources",
                newName: "EventTeamId");

            migrationBuilder.RenameIndex(
                name: "IX_EventResources_TeamId",
                table: "EventResources",
                newName: "IX_EventResources_SongId");

            migrationBuilder.RenameIndex(
                name: "IX_EventResources_SignificantResourceId",
                table: "EventResources",
                newName: "IX_EventResources_EventTeamId");

            migrationBuilder.AddColumn<Guid>(
                name: "ChartId",
                table: "EventResources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Participations",
                table: "Participations",
                columns: new[] { "EventTeamId", "ParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Participations_ParticipantId",
                table: "Participations",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_EventResources_ChartId",
                table: "EventResources",
                column: "ChartId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventResources_Charts_ChartId",
                table: "EventResources",
                column: "ChartId",
                principalTable: "Charts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventResources_EventTeams_EventTeamId",
                table: "EventResources",
                column: "EventTeamId",
                principalTable: "EventTeams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventResources_Songs_SongId",
                table: "EventResources",
                column: "SongId",
                principalTable: "Songs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Participations_EventTeams_EventTeamId",
                table: "Participations",
                column: "EventTeamId",
                principalTable: "EventTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
