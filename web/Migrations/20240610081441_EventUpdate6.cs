using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class EventUpdate6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hostship_Events_EventId",
                table: "Hostship");

            migrationBuilder.DropForeignKey(
                name: "FK_Hostship_Users_UserId",
                table: "Hostship");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Hostship",
                table: "Hostship");

            migrationBuilder.RenameTable(
                name: "Hostship",
                newName: "Hostships");

            migrationBuilder.RenameIndex(
                name: "IX_Hostship_UserId",
                table: "Hostships",
                newName: "IX_Hostships_UserId");

            migrationBuilder.AddColumn<List<string>>(
                name: "Preserved",
                table: "EventResources",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "EventResources",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Hostships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnveiled",
                table: "Hostships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long[]>(
                name: "Permissions",
                table: "Hostships",
                type: "bigint[]",
                nullable: false,
                defaultValue: new long[0]);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Hostships",
                table: "Hostships",
                columns: new[] { "EventId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Hostships_Events_EventId",
                table: "Hostships",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Hostships_Users_UserId",
                table: "Hostships",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hostships_Events_EventId",
                table: "Hostships");

            migrationBuilder.DropForeignKey(
                name: "FK_Hostships_Users_UserId",
                table: "Hostships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Hostships",
                table: "Hostships");

            migrationBuilder.DropColumn(
                name: "Preserved",
                table: "EventResources");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "EventResources");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Hostships");

            migrationBuilder.DropColumn(
                name: "IsUnveiled",
                table: "Hostships");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Hostships");

            migrationBuilder.RenameTable(
                name: "Hostships",
                newName: "Hostship");

            migrationBuilder.RenameIndex(
                name: "IX_Hostships_UserId",
                table: "Hostship",
                newName: "IX_Hostship_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Hostship",
                table: "Hostship",
                columns: new[] { "EventId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Hostship_Events_EventId",
                table: "Hostship",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Hostship_Users_UserId",
                table: "Hostship",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
