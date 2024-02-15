using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationUserUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorizationPage",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TokenEndpoint",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "UnionId",
                table: "ApplicationUsers",
                newName: "TapUnionId");

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

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateUpdated",
                table: "ApplicationUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "Applications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EventDivisionId",
                table: "Users",
                column: "EventDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EventId",
                table: "Users",
                column: "EventId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_EventDivisions_EventDivisionId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Events_EventId",
                table: "Users");

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

            migrationBuilder.DropColumn(
                name: "DateUpdated",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "TapUnionId",
                table: "ApplicationUsers",
                newName: "UnionId");

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationPage",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenEndpoint",
                table: "Applications",
                type: "text",
                nullable: true);
        }
    }
}
