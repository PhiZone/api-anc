using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRelations_Users_FolloweeId",
                table: "UserRelations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRelations_Users_FollowerId",
                table: "UserRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRelations",
                table: "UserRelations");

            migrationBuilder.DropIndex(
                name: "IX_UserRelations_FolloweeId",
                table: "UserRelations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRelations",
                table: "UserRelations",
                columns: new[] { "FolloweeId", "FollowerId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRelations_FollowerId",
                table: "UserRelations",
                column: "FollowerId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRelations_Users_FolloweeId",
                table: "UserRelations",
                column: "FolloweeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRelations_Users_FollowerId",
                table: "UserRelations",
                column: "FollowerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRelations_Users_FolloweeId",
                table: "UserRelations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRelations_Users_FollowerId",
                table: "UserRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRelations",
                table: "UserRelations");

            migrationBuilder.DropIndex(
                name: "IX_UserRelations_FollowerId",
                table: "UserRelations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRelations",
                table: "UserRelations",
                columns: new[] { "FollowerId", "FolloweeId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRelations_FolloweeId",
                table: "UserRelations",
                column: "FolloweeId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRelations_Users_FolloweeId",
                table: "UserRelations",
                column: "FolloweeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRelations_Users_FollowerId",
                table: "UserRelations",
                column: "FollowerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
