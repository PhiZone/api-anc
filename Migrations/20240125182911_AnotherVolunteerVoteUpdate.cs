using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class AnotherVolunteerVoteUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admissions_Chapters_ChapterId",
                table: "Admissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Admissions_Collections_CollectionId",
                table: "Admissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Admissions_Songs_SongId",
                table: "Admissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Charts_Collections_CollectionId",
                table: "Charts");

            migrationBuilder.DropTable(
                name: "ChapterSong");

            migrationBuilder.DropIndex(
                name: "IX_Charts_CollectionId",
                table: "Charts");

            migrationBuilder.DropIndex(
                name: "IX_Admissions_ChapterId",
                table: "Admissions");

            migrationBuilder.DropIndex(
                name: "IX_Admissions_CollectionId",
                table: "Admissions");

            migrationBuilder.DropIndex(
                name: "IX_Admissions_SongId",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "Charts");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "SongId",
                table: "Admissions");

            migrationBuilder.AddColumn<double>(
                name: "SuggestedDifficulty",
                table: "VolunteerVotes",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuggestedDifficulty",
                table: "VolunteerVotes");

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "Charts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChapterId",
                table: "Admissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "Admissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SongId",
                table: "Admissions",
                type: "uuid",
                nullable: true);

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
                name: "IX_Charts_CollectionId",
                table: "Charts",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_ChapterId",
                table: "Admissions",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_CollectionId",
                table: "Admissions",
                column: "CollectionId");

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
                name: "FK_Admissions_Collections_CollectionId",
                table: "Admissions",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Admissions_Songs_SongId",
                table: "Admissions",
                column: "SongId",
                principalTable: "Songs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Charts_Collections_CollectionId",
                table: "Charts",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id");
        }
    }
}
