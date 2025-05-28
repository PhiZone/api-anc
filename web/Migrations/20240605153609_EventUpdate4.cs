using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class EventUpdate4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TagId",
                table: "EventResources",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventResources_TagId",
                table: "EventResources",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventResources_Tags_TagId",
                table: "EventResources",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventResources_Tags_TagId",
                table: "EventResources");

            migrationBuilder.DropIndex(
                name: "IX_EventResources_TagId",
                table: "EventResources");

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "EventResources");
        }
    }
}
