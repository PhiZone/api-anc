using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class ServiceRecordUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRecords_ServiceScripts_ServiceScriptId",
                table: "ServiceRecords");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRecords_ServiceScriptId",
                table: "ServiceRecords");

            migrationBuilder.DropColumn(
                name: "ServiceScriptId",
                table: "ServiceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServiceId",
                table: "ServiceRecords",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRecords_ServiceScripts_ServiceId",
                table: "ServiceRecords",
                column: "ServiceId",
                principalTable: "ServiceScripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRecords_ServiceScripts_ServiceId",
                table: "ServiceRecords");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRecords_ServiceId",
                table: "ServiceRecords");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceScriptId",
                table: "ServiceRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServiceScriptId",
                table: "ServiceRecords",
                column: "ServiceScriptId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRecords_ServiceScripts_ServiceScriptId",
                table: "ServiceRecords",
                column: "ServiceScriptId",
                principalTable: "ServiceScripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
