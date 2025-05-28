using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiZoneApi.Migrations
{
    /// <inheritdoc />
    public partial class PrivilegeEscalationTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PetAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Question1 = table.Column<Guid>(type: "uuid", nullable: false),
                    Answer1 = table.Column<string>(type: "text", nullable: false),
                    Question2 = table.Column<Guid>(type: "uuid", nullable: false),
                    Answer2 = table.Column<string>(type: "text", nullable: false),
                    Question3 = table.Column<Guid>(type: "uuid", nullable: false),
                    Answer3 = table.Column<string>(type: "text", nullable: false),
                    Chart = table.Column<string>(type: "text", nullable: false),
                    ObjectiveScore = table.Column<int>(type: "integer", nullable: false),
                    SubjectiveScore = table.Column<int>(type: "integer", nullable: true),
                    TotalScore = table.Column<int>(type: "integer", nullable: true),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    AssessorId = table.Column<int>(type: "integer", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetAnswers_Users_AssessorId",
                        column: x => x.AssessorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PetAnswers_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PetQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PetChoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetChoices_PetQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "PetQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetAnswers_AssessorId",
                table: "PetAnswers",
                column: "AssessorId");

            migrationBuilder.CreateIndex(
                name: "IX_PetAnswers_OwnerId",
                table: "PetAnswers",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PetChoices_QuestionId",
                table: "PetChoices",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetAnswers");

            migrationBuilder.DropTable(
                name: "PetChoices");

            migrationBuilder.DropTable(
                name: "PetQuestions");
        }
    }
}
