using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class AdmissionPrimaryKeyUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admissions_Chapters_AdmitterId",
                table: "Admissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Admissions_Songs_AdmitteeId",
                table: "Admissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Admissions",
                table: "Admissions");

            migrationBuilder.DropIndex(
                name: "IX_Admissions_AdmitterId",
                table: "Admissions");

            migrationBuilder.AddColumn<Guid>(
                name: "ChapterId",
                table: "Admissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SongId",
                table: "Admissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Admissions",
                table: "Admissions",
                columns: new[] { "AdmitterId", "AdmitteeId" });

            migrationBuilder.CreateTable(
                name: "ChapterSong",
                columns: table => new
                {
                    ChaptersId = table.Column<Guid>(type: "uuid", nullable: false),
                    SongsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterSong", x => new { x.ChaptersId, x.SongsId });
                    table.ForeignKey(
                        name: "FK_ChapterSong_Chapters_ChaptersId",
                        column: x => x.ChaptersId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterSong_Songs_SongsId",
                        column: x => x.SongsId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_AdmitteeId",
                table: "Admissions",
                column: "AdmitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_ChapterId",
                table: "Admissions",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_SongId",
                table: "Admissions",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterSong_SongsId",
                table: "ChapterSong",
                column: "SongsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Admissions_Chapters_ChapterId",
                table: "Admissions",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Admissions_Songs_SongId",
                table: "Admissions",
                column: "SongId",
                principalTable: "Songs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admissions_Chapters_ChapterId",
                table: "Admissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Admissions_Songs_SongId",
                table: "Admissions");

            migrationBuilder.DropTable(
                name: "ChapterSong");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Admissions",
                table: "Admissions");

            migrationBuilder.DropIndex(
                name: "IX_Admissions_AdmitteeId",
                table: "Admissions");

            migrationBuilder.DropIndex(
                name: "IX_Admissions_ChapterId",
                table: "Admissions");

            migrationBuilder.DropIndex(
                name: "IX_Admissions_SongId",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "SongId",
                table: "Admissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Admissions",
                table: "Admissions",
                columns: new[] { "AdmitteeId", "AdmitterId" });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_AdmitterId",
                table: "Admissions",
                column: "AdmitterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Admissions_Chapters_AdmitterId",
                table: "Admissions",
                column: "AdmitterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Admissions_Songs_AdmitteeId",
                table: "Admissions",
                column: "AdmitteeId",
                principalTable: "Songs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
