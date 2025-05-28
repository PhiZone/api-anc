using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class EventUpdate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_EventDivisions_EventDivisionId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Events_EventId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ApplicationServiceRecords");

            migrationBuilder.DropTable(
                name: "ApplicationServices");

            migrationBuilder.DropIndex(
                name: "IX_Users_EventDivisionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EventId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EventDivisionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "EventTeams",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "EventTeams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnveiled",
                table: "EventTeams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "Preserved",
                table: "EventTeams",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "Preserved",
                table: "EventDivisions",
                type: "text[]",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "EventResources",
                columns: table => new
                {
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: true),
                    EventTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChartId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    SongId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventResources", x => new { x.DivisionId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_EventResources_Charts_ChartId",
                        column: x => x.ChartId,
                        principalTable: "Charts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventResources_EventDivisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "EventDivisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventResources_EventTeams_EventTeamId",
                        column: x => x.EventTeamId,
                        principalTable: "EventTeams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventResources_Records_RecordId",
                        column: x => x.RecordId,
                        principalTable: "Records",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventResources_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Hostship",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hostship", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Hostship_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Hostship_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceScripts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Parameters = table.Column<List<string>>(type: "text[]", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceScripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceScriptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_ServiceScripts_ServiceScriptId",
                        column: x => x.ServiceScriptId,
                        principalTable: "ServiceScripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventResources_ChartId",
                table: "EventResources",
                column: "ChartId");

            migrationBuilder.CreateIndex(
                name: "IX_EventResources_EventTeamId",
                table: "EventResources",
                column: "EventTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_EventResources_RecordId",
                table: "EventResources",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_EventResources_ResourceId",
                table: "EventResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_EventResources_SongId",
                table: "EventResources",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Hostship_UserId",
                table: "Hostship",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_OwnerId",
                table: "ServiceRecords",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ResourceId",
                table: "ServiceRecords",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServiceScriptId",
                table: "ServiceRecords",
                column: "ServiceScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceScripts_ResourceId",
                table: "ServiceScripts",
                column: "ResourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventResources");

            migrationBuilder.DropTable(
                name: "Hostship");

            migrationBuilder.DropTable(
                name: "ServiceRecords");

            migrationBuilder.DropTable(
                name: "ServiceScripts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "EventTeams");

            migrationBuilder.DropColumn(
                name: "IsUnveiled",
                table: "EventTeams");

            migrationBuilder.DropColumn(
                name: "Preserved",
                table: "EventTeams");

            migrationBuilder.DropColumn(
                name: "Preserved",
                table: "EventDivisions");

            migrationBuilder.AddColumn<Guid>(
                name: "EventDivisionId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "EventTeams",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Parameters = table.Column<List<string>>(type: "text[]", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationServices_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationServiceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationServiceRecords_ApplicationServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "ApplicationServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationServiceRecords_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EventDivisionId",
                table: "Users",
                column: "EventDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EventId",
                table: "Users",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationServiceRecords_OwnerId",
                table: "ApplicationServiceRecords",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationServiceRecords_ResourceId",
                table: "ApplicationServiceRecords",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationServiceRecords_ServiceId",
                table: "ApplicationServiceRecords",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationServices_ApplicationId",
                table: "ApplicationServices",
                column: "ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_EventDivisions_EventDivisionId",
                table: "Users",
                column: "EventDivisionId",
                principalTable: "EventDivisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Events_EventId",
                table: "Users",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");
        }
    }
}
