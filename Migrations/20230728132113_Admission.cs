using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class Admission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterSong");

            migrationBuilder.AddColumn<Guid>(
                name: "ChartSubmissionId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SongSubmissionId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SongSubmissionId",
                table: "Chapters",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Admissions",
                columns: table => new
                {
                    AdmitterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdmitteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    RequesterId = table.Column<int>(type: "integer", nullable: false),
                    RequesteeId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admissions", x => new { x.AdmitteeId, x.AdmitterId });
                    table.ForeignKey(
                        name: "FK_Admissions_Chapters_AdmitterId",
                        column: x => x.AdmitterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Admissions_Songs_AdmitteeId",
                        column: x => x.AdmitteeId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Admissions_Users_RequesteeId",
                        column: x => x.RequesteeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Admissions_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChartSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    LevelType = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<double>(type: "double precision", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    File = table.Column<string>(type: "text", nullable: true),
                    FileChecksum = table.Column<string>(type: "text", nullable: true),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    Illustration = table.Column<string>(type: "text", nullable: true),
                    Illustrator = table.Column<string>(type: "text", nullable: true),
                    IsRanked = table.Column<bool>(type: "boolean", nullable: false),
                    NoteCount = table.Column<int>(type: "integer", nullable: false),
                    SongId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Accessibility = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VolunteerStatus = table.Column<int>(type: "integer", nullable: false),
                    CollabStatus = table.Column<int>(type: "integer", nullable: false),
                    AdmissionStatus = table.Column<int>(type: "integer", nullable: false),
                    RepresentationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChartSubmissions_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChartSubmissions_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SongSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    EditionType = table.Column<int>(type: "integer", nullable: false),
                    Edition = table.Column<string>(type: "text", nullable: true),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    File = table.Column<string>(type: "text", nullable: true),
                    FileChecksum = table.Column<string>(type: "text", nullable: true),
                    Illustration = table.Column<string>(type: "text", nullable: false),
                    Illustrator = table.Column<string>(type: "text", nullable: false),
                    Lyrics = table.Column<string>(type: "text", nullable: true),
                    Bpm = table.Column<int>(type: "integer", nullable: false),
                    MinBpm = table.Column<int>(type: "integer", nullable: false),
                    MaxBpm = table.Column<int>(type: "integer", nullable: false),
                    Offset = table.Column<int>(type: "integer", nullable: false),
                    OriginalityProof = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    PreviewStart = table.Column<TimeSpan>(type: "interval", nullable: false),
                    PreviewEnd = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Accessibility = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VolunteerStatus = table.Column<int>(type: "integer", nullable: false),
                    CollabStatus = table.Column<int>(type: "integer", nullable: false),
                    AdmissionStatus = table.Column<int>(type: "integer", nullable: false),
                    RepresentationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongSubmissions_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerVotes_ChartSubmissions_ChartId",
                        column: x => x.ChartId,
                        principalTable: "ChartSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerVotes_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ChartSubmissionId",
                table: "Users",
                column: "ChartSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SongSubmissionId",
                table: "Users",
                column: "SongSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SongSubmissionId",
                table: "Chapters",
                column: "SongSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_AdmitterId",
                table: "Admissions",
                column: "AdmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_RequesteeId",
                table: "Admissions",
                column: "RequesteeId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_RequesterId",
                table: "Admissions",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartSubmissions_OwnerId",
                table: "ChartSubmissions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartSubmissions_RepresentationId",
                table: "ChartSubmissions",
                column: "RepresentationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartSubmissions_SongId",
                table: "ChartSubmissions",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_SongSubmissions_OwnerId",
                table: "SongSubmissions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SongSubmissions_RepresentationId",
                table: "SongSubmissions",
                column: "RepresentationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerVotes_ChartId",
                table: "VolunteerVotes",
                column: "ChartId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerVotes_OwnerId",
                table: "VolunteerVotes",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_SongSubmissions_SongSubmissionId",
                table: "Chapters",
                column: "SongSubmissionId",
                principalTable: "SongSubmissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ChartSubmissions_ChartSubmissionId",
                table: "Users",
                column: "ChartSubmissionId",
                principalTable: "ChartSubmissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_SongSubmissions_SongSubmissionId",
                table: "Users",
                column: "SongSubmissionId",
                principalTable: "SongSubmissions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_SongSubmissions_SongSubmissionId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_ChartSubmissions_ChartSubmissionId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_SongSubmissions_SongSubmissionId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Admissions");

            migrationBuilder.DropTable(
                name: "SongSubmissions");

            migrationBuilder.DropTable(
                name: "VolunteerVotes");

            migrationBuilder.DropTable(
                name: "ChartSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Users_ChartSubmissionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SongSubmissionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_SongSubmissionId",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "ChartSubmissionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SongSubmissionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SongSubmissionId",
                table: "Chapters");

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
                name: "IX_ChapterSong_SongsId",
                table: "ChapterSong",
                column: "SongsId");
        }
    }
}
