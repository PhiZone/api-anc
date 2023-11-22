using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class ResourceRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    EditionType = table.Column<int>(type: "integer", nullable: false),
                    Edition = table.Column<string>(type: "text", nullable: true),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Strategy = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    CopyrightOwner = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceRecords");
        }
    }
}
