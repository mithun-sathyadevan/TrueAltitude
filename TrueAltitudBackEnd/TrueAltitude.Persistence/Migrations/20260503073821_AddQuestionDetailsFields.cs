using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueAltitude.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionDetailsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "LearningQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExplanationText",
                table: "LearningQuestions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "LearningQuestions",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "LearningQuestions");

            migrationBuilder.DropColumn(
                name: "ExplanationText",
                table: "LearningQuestions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "LearningQuestions");
        }
    }
}
