using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionModelUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongSubmissions_Users_ReviewerId",
                table: "SongSubmissions");

            migrationBuilder.AlterColumn<int>(
                name: "ReviewerId",
                table: "SongSubmissions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_SongSubmissions_Users_ReviewerId",
                table: "SongSubmissions",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongSubmissions_Users_ReviewerId",
                table: "SongSubmissions");

            migrationBuilder.AlterColumn<int>(
                name: "ReviewerId",
                table: "SongSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SongSubmissions_Users_ReviewerId",
                table: "SongSubmissions",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
