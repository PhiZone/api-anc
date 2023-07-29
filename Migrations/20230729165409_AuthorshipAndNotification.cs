using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class AuthorshipAndNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ChartSubmissions_ChartSubmissionId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_SongSubmissions_SongSubmissionId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ChartUser");

            migrationBuilder.DropTable(
                name: "SongUser");

            migrationBuilder.DropIndex(
                name: "IX_Users_ChartSubmissionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SongSubmissionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChartSubmissionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SongSubmissionId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "UserRelations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Authorships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Authorships_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OperatorId = table.Column<int>(type: "integer", nullable: true),
                    DateRead = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Authorships_AuthorId",
                table: "Authorships",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Authorships_ResourceId",
                table: "Authorships",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OperatorId",
                table: "Notifications",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OwnerId",
                table: "Notifications",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Authorships");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "UserRelations");

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

            migrationBuilder.CreateTable(
                name: "ChartUser",
                columns: table => new
                {
                    AuthorsId = table.Column<int>(type: "integer", nullable: false),
                    ChartsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartUser", x => new { x.AuthorsId, x.ChartsId });
                    table.ForeignKey(
                        name: "FK_ChartUser_Charts_ChartsId",
                        column: x => x.ChartsId,
                        principalTable: "Charts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChartUser_Users_AuthorsId",
                        column: x => x.AuthorsId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SongUser",
                columns: table => new
                {
                    AuthorsId = table.Column<int>(type: "integer", nullable: false),
                    SongsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongUser", x => new { x.AuthorsId, x.SongsId });
                    table.ForeignKey(
                        name: "FK_SongUser_Songs_SongsId",
                        column: x => x.SongsId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongUser_Users_AuthorsId",
                        column: x => x.AuthorsId,
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
                name: "IX_ChartUser_ChartsId",
                table: "ChartUser",
                column: "ChartsId");

            migrationBuilder.CreateIndex(
                name: "IX_SongUser_SongsId",
                table: "SongUser",
                column: "SongsId");

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
    }
}
