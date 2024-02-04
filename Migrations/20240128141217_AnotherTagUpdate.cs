using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class AnotherTagUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChartTags");

            migrationBuilder.DropTable(
                name: "SongTags");

            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "SongSubmissions",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "ChartSubmissions",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateTable(
                name: "ChartTag",
                columns: table => new
                {
                    ChartsId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartTag", x => new { x.ChartsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_ChartTag_Charts_ChartsId",
                        column: x => x.ChartsId,
                        principalTable: "Charts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChartTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SongTag",
                columns: table => new
                {
                    SongsId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongTag", x => new { x.SongsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_SongTag_Songs_SongsId",
                        column: x => x.SongsId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChartTag_TagsId",
                table: "ChartTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_SongTag_TagsId",
                table: "SongTag",
                column: "TagsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChartTag");

            migrationBuilder.DropTable(
                name: "SongTag");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "SongSubmissions");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "ChartSubmissions");

            migrationBuilder.CreateTable(
                name: "ChartTags",
                columns: table => new
                {
                    ChartId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartTags", x => new { x.ChartId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ChartTags_Charts_ChartId",
                        column: x => x.ChartId,
                        principalTable: "Charts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChartTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SongTags",
                columns: table => new
                {
                    SongId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongTags", x => new { x.SongId, x.TagId });
                    table.ForeignKey(
                        name: "FK_SongTags_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChartTags_TagId",
                table: "ChartTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_SongTags_TagId",
                table: "SongTags",
                column: "TagId");
        }
    }
}
