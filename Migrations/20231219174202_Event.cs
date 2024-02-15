using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class Event : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RepresentationId",
                table: "ChartAssetSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResourceId",
                table: "Announcements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResourceType",
                table: "Announcements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Accessibility = table.Column<int>(type: "integer", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Subtitle = table.Column<string>(type: "text", nullable: true),
                    Illustration = table.Column<string>(type: "text", nullable: false),
                    Illustrator = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ClaimedParticipantCount = table.Column<int>(type: "integer", nullable: true),
                    ClaimedSubmissionCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTeams_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventDivisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Accessibility = table.Column<int>(type: "integer", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Subtitle = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Illustration = table.Column<string>(type: "text", nullable: true),
                    Illustrator = table.Column<string>(type: "text", nullable: true),
                    MinTeamCount = table.Column<int>(type: "integer", nullable: true),
                    MaxTeamCount = table.Column<int>(type: "integer", nullable: true),
                    MinParticipantPerTeamCount = table.Column<int>(type: "integer", nullable: true),
                    MaxParticipantPerTeamCount = table.Column<int>(type: "integer", nullable: true),
                    MinSubmissionCount = table.Column<int>(type: "integer", nullable: true),
                    MaxSubmissionCount = table.Column<int>(type: "integer", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatePublicized = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventDivisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventDivisions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventDivisions_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participations",
                columns: table => new
                {
                    EventTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participations", x => new { x.EventTeamId, x.ParticipantId });
                    table.ForeignKey(
                        name: "FK_Participations_EventTeams_EventTeamId",
                        column: x => x.EventTeamId,
                        principalTable: "EventTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participations_Users_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateExecuted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTasks_EventDivisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "EventDivisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChartAssetSubmissions_RepresentationId",
                table: "ChartAssetSubmissions",
                column: "RepresentationId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ResourceId",
                table: "Announcements",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_EventDivisions_EventId",
                table: "EventDivisions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventDivisions_OwnerId",
                table: "EventDivisions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OwnerId",
                table: "Events",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTasks_DivisionId",
                table: "EventTasks",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTeams_OwnerId",
                table: "EventTeams",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_ParticipantId",
                table: "Participations",
                column: "ParticipantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChartAssetSubmissions_ChartAssets_RepresentationId",
                table: "ChartAssetSubmissions",
                column: "RepresentationId",
                principalTable: "ChartAssets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChartAssetSubmissions_ChartAssets_RepresentationId",
                table: "ChartAssetSubmissions");

            migrationBuilder.DropTable(
                name: "EventTasks");

            migrationBuilder.DropTable(
                name: "Participations");

            migrationBuilder.DropTable(
                name: "EventDivisions");

            migrationBuilder.DropTable(
                name: "EventTeams");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropIndex(
                name: "IX_ChartAssetSubmissions_RepresentationId",
                table: "ChartAssetSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_ResourceId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "RepresentationId",
                table: "ChartAssetSubmissions");

            migrationBuilder.DropColumn(
                name: "ResourceId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "ResourceType",
                table: "Announcements");
        }
    }
}
