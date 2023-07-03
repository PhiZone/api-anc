using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class ModifyUserModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Experience",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Experience",
                table: "Users",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
