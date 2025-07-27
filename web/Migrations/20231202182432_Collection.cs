using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class Collection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TapUnionId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayCount",
                table: "Songs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "Charts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TapClientId",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "Admissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Collections",
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
                    Subtitle = table.Column<string>(type: "text", nullable: false),
                    Illustration = table.Column<string>(type: "text", nullable: false),
                    Illustrator = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TapUserRelations",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnionId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TapUserRelations", x => new { x.ApplicationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TapUserRelations_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TapUserRelations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Charts_CollectionId",
                table: "Charts",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_CollectionId",
                table: "Admissions",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_OwnerId",
                table: "Collections",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TapUserRelations_UserId",
                table: "TapUserRelations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Admissions_Collections_CollectionId",
                table: "Admissions",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Charts_Collections_CollectionId",
                table: "Charts",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admissions_Collections_CollectionId",
                table: "Admissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Charts_Collections_CollectionId",
                table: "Charts");

            migrationBuilder.DropTable(
                name: "Collections");

            migrationBuilder.DropTable(
                name: "TapUserRelations");

            migrationBuilder.DropIndex(
                name: "IX_Charts_CollectionId",
                table: "Charts");

            migrationBuilder.DropIndex(
                name: "IX_Admissions_CollectionId",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PlayCount",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "Charts");

            migrationBuilder.DropColumn(
                name: "TapClientId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "Admissions");

            migrationBuilder.AddColumn<string>(
                name: "TapUnionId",
                table: "Users",
                type: "text",
                nullable: true);
        }
    }
}
