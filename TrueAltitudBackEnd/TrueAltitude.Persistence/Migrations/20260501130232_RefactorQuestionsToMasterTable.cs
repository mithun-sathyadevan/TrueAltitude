using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueAltitude.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorQuestionsToMasterTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningQuestions_LearningTopics_TopicId",
                table: "LearningQuestions");

            migrationBuilder.DropIndex(
                name: "IX_LearningQuestions_TopicId_SortOrder",
                table: "LearningQuestions");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "LearningQuestions");

            migrationBuilder.CreateTable(
                name: "LearningTopicQuestions",
                columns: table => new
                {
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTopicQuestions", x => new { x.TopicId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_LearningTopicQuestions_LearningQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "LearningQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningTopicQuestions_LearningTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "LearningTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopicQuestions_QuestionId",
                table: "LearningTopicQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopicQuestions_TopicId_SortOrder",
                table: "LearningTopicQuestions",
                columns: new[] { "TopicId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearningTopicQuestions");

            migrationBuilder.AddColumn<int>(
                name: "TopicId",
                table: "LearningQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LearningQuestions_TopicId_SortOrder",
                table: "LearningQuestions",
                columns: new[] { "TopicId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_LearningQuestions_LearningTopics_TopicId",
                table: "LearningQuestions",
                column: "TopicId",
                principalTable: "LearningTopics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
