using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class ServiceRecordUpdate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRecords_ResourceId",
                table: "ServiceRecords");

            migrationBuilder.DropColumn(
                name: "ResourceId",
                table: "ServiceRecords");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ServiceRecords",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "ServiceRecords");

            migrationBuilder.AddColumn<Guid>(
                name: "ResourceId",
                table: "ServiceRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ResourceId",
                table: "ServiceRecords",
                column: "ResourceId");
        }
    }
}
