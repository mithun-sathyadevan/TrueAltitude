using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueAltitude.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningContentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresSubscription = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubscriptionLabel = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningSubjects", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LearningTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    ParentTopicId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresSubscription = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubscriptionLabel = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningTopics_LearningSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "LearningSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningTopics_LearningTopics_ParentTopicId",
                        column: x => x.ParentTopicId,
                        principalTable: "LearningTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LearningQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Text = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresSubscription = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubscriptionLabel = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningQuestions_LearningTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "LearningTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LearningQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(140)", maxLength: 140, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Text = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Explanation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningQuestionOptions_LearningQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "LearningQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LearningQuestionOptions_Code",
                table: "LearningQuestionOptions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningQuestionOptions_QuestionId_SortOrder",
                table: "LearningQuestionOptions",
                columns: new[] { "QuestionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningQuestions_Code",
                table: "LearningQuestions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningQuestions_TopicId_SortOrder",
                table: "LearningQuestions",
                columns: new[] { "TopicId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningSubjects_Code",
                table: "LearningSubjects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_Code",
                table: "LearningTopics",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_ParentTopicId",
                table: "LearningTopics",
                column: "ParentTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_SubjectId_ParentTopicId_SortOrder",
                table: "LearningTopics",
                columns: new[] { "SubjectId", "ParentTopicId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearningQuestionOptions");

            migrationBuilder.DropTable(
                name: "LearningQuestions");

            migrationBuilder.DropTable(
                name: "LearningTopics");

            migrationBuilder.DropTable(
                name: "LearningSubjects");
        }
    }
}
