using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class ChartAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChartSubmissions_Songs_SongId",
                table: "ChartSubmissions");

            migrationBuilder.DropColumn(
                name: "CollabStatus",
                table: "SongSubmissions");

            migrationBuilder.DropColumn(
                name: "VolunteerStatus",
                table: "SongSubmissions");

            migrationBuilder.DropColumn(
                name: "CollabStatus",
                table: "ChartSubmissions");

            migrationBuilder.AlterColumn<List<int>>(
                name: "AspectRatio",
                table: "PlayConfigurations",
                type: "integer[]",
                nullable: true,
                oldClrType: typeof(List<int>),
                oldType: "integer[]");

            migrationBuilder.AlterColumn<Guid>(
                name: "SongId",
                table: "ChartSubmissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "SongSubmissionId",
                table: "ChartSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Secret",
                table: "Applications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "ChartAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    File = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChartAssets_Charts_ChartId",
                        column: x => x.ChartId,
                        principalTable: "Charts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChartAssets_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChartAssetSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChartSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    File = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartAssetSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChartAssetSubmissions_ChartSubmissions_ChartSubmissionId",
                        column: x => x.ChartSubmissionId,
                        principalTable: "ChartSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChartAssetSubmissions_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Collaborations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviterId = table.Column<int>(type: "integer", nullable: false),
                    InviteeId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collaborations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collaborations_Users_InviteeId",
                        column: x => x.InviteeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Collaborations_Users_InviterId",
                        column: x => x.InviterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChartSubmissions_SongSubmissionId",
                table: "ChartSubmissions",
                column: "SongSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartAssets_ChartId",
                table: "ChartAssets",
                column: "ChartId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartAssets_OwnerId",
                table: "ChartAssets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartAssetSubmissions_ChartSubmissionId",
                table: "ChartAssetSubmissions",
                column: "ChartSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartAssetSubmissions_OwnerId",
                table: "ChartAssetSubmissions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborations_InviteeId",
                table: "Collaborations",
                column: "InviteeId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborations_InviterId",
                table: "Collaborations",
                column: "InviterId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborations_SubmissionId",
                table: "Collaborations",
                column: "SubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChartSubmissions_SongSubmissions_SongSubmissionId",
                table: "ChartSubmissions",
                column: "SongSubmissionId",
                principalTable: "SongSubmissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChartSubmissions_Songs_SongId",
                table: "ChartSubmissions",
                column: "SongId",
                principalTable: "Songs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChartSubmissions_SongSubmissions_SongSubmissionId",
                table: "ChartSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ChartSubmissions_Songs_SongId",
                table: "ChartSubmissions");

            migrationBuilder.DropTable(
                name: "ChartAssets");

            migrationBuilder.DropTable(
                name: "ChartAssetSubmissions");

            migrationBuilder.DropTable(
                name: "Collaborations");

            migrationBuilder.DropIndex(
                name: "IX_ChartSubmissions_SongSubmissionId",
                table: "ChartSubmissions");

            migrationBuilder.DropColumn(
                name: "SongSubmissionId",
                table: "ChartSubmissions");

            migrationBuilder.AddColumn<int>(
                name: "CollabStatus",
                table: "SongSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VolunteerStatus",
                table: "SongSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<List<int>>(
                name: "AspectRatio",
                table: "PlayConfigurations",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(List<int>),
                oldType: "integer[]",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SongId",
                table: "ChartSubmissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CollabStatus",
                table: "ChartSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Secret",
                table: "Applications",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChartSubmissions_Songs_SongId",
                table: "ChartSubmissions",
                column: "SongId",
                principalTable: "Songs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
