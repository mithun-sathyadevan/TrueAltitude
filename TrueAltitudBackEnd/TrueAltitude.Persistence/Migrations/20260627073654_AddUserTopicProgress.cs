using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueAltitude.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTopicProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTopicProgress",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BestPercent = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastPercent = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTopicProgress", x => new { x.UserId, x.TopicId });
                    table.ForeignKey(
                        name: "FK_UserTopicProgress_LearningTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "LearningTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTopicProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserTopicProgress_TopicId",
                table: "UserTopicProgress",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTopicProgress_UserId_IsCompleted",
                table: "UserTopicProgress",
                columns: new[] { "UserId", "IsCompleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTopicProgress");
        }
    }
}
