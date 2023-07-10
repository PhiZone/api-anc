using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class PlayServiceModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "Records",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Illustration = table.Column<string>(type: "text", nullable: false),
                    Illustrator = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Homepage = table.Column<string>(type: "text", nullable: false),
                    ApiEndpoint = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    PerfectJudgment = table.Column<int>(type: "integer", nullable: false),
                    GoodJudgment = table.Column<int>(type: "integer", nullable: false),
                    AspectRatio = table.Column<List<int>>(type: "integer[]", nullable: false),
                    NoteSize = table.Column<double>(type: "double precision", nullable: false),
                    ChartMirroring = table.Column<int>(type: "integer", nullable: false),
                    BackgroundLuminance = table.Column<double>(type: "double precision", nullable: false),
                    BackgroundBlur = table.Column<double>(type: "double precision", nullable: false),
                    SimultaneousNoteHint = table.Column<bool>(type: "boolean", nullable: false),
                    FcApIndicator = table.Column<bool>(type: "boolean", nullable: false),
                    ChartOffset = table.Column<int>(type: "integer", nullable: false),
                    HitSoundVolume = table.Column<double>(type: "double precision", nullable: false),
                    MusicVolume = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayConfigurations_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Records_ApplicationId",
                table: "Records",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OwnerId",
                table: "Applications",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayConfigurations_OwnerId",
                table: "PlayConfigurations",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Applications_ApplicationId",
                table: "Records",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Applications_ApplicationId",
                table: "Records");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "PlayConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_Records_ApplicationId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "Records");
        }
    }
}
