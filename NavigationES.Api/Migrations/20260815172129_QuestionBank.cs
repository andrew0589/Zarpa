using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    public partial class QuestionBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExamMinutes = table.Column<int>(type: "int", nullable: true),
                    MaxTotalErrors = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LicenseTopics",
                columns: table => new
                {
                    LicenseID = table.Column<long>(type: "bigint", nullable: false),
                    TopicID = table.Column<long>(type: "bigint", nullable: false),
                    QuestionsInExam = table.Column<int>(type: "int", nullable: false),
                    MaxErrors = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseTopics", x => new { x.LicenseID, x.TopicID });
                    table.ForeignKey(
                        name: "FK_LicenseTopics_Licenses_LicenseID",
                        column: x => x.LicenseID,
                        principalTable: "Licenses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LicenseTopics_Topics_TopicID",
                        column: x => x.TopicID,
                        principalTable: "Topics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicID = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceExam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Questions_Topics_TopicID",
                        column: x => x.TopicID,
                        principalTable: "Topics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestSessions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    LicenseID = table.Column<long>(type: "bigint", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    TopicID = table.Column<long>(type: "bigint", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Passed = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSessions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TestSessions_Licenses_LicenseID",
                        column: x => x.LicenseID,
                        principalTable: "Licenses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestSessions_Topics_TopicID",
                        column: x => x.TopicID,
                        principalTable: "Topics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestSessions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionID = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Answers_Questions_QuestionID",
                        column: x => x.QuestionID,
                        principalTable: "Questions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionAnswers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionID = table.Column<long>(type: "bigint", nullable: false),
                    QuestionID = table.Column<long>(type: "bigint", nullable: false),
                    ChosenAnswerID = table.Column<long>(type: "bigint", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAnswers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SessionAnswers_Answers_ChosenAnswerID",
                        column: x => x.ChosenAnswerID,
                        principalTable: "Answers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionAnswers_Questions_QuestionID",
                        column: x => x.QuestionID,
                        principalTable: "Questions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionAnswers_TestSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "TestSessions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Licenses",
                columns: new[] { "ID", "Code", "ExamMinutes", "MaxTotalErrors", "Name" },
                values: new object[,]
                {
                    { 1L, "PNB", 45, null, "Patrón para Navegación Básica" },
                    { 2L, "PER", 90, 13, "Patrón de Embarcaciones de Recreo" },
                    { 3L, "PY", null, null, "Patrón de Yate" },
                    { 4L, "CY", null, null, "Capitán de Yate" }
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "ID", "Name", "Number" },
                values: new object[,]
                {
                    { 1L, "Nomenclatura náutica", 1 },
                    { 2L, "Elementos de amarre y fondeo", 2 },
                    { 3L, "Seguridad", 3 },
                    { 4L, "Legislación", 4 },
                    { 5L, "Balizamiento", 5 },
                    { 6L, "Reglamento (RIPA)", 6 },
                    { 7L, "Maniobra y navegación", 7 },
                    { 8L, "Emergencias en la mar", 8 },
                    { 9L, "Meteorología", 9 },
                    { 10L, "Teoría de la navegación", 10 },
                    { 11L, "Carta de navegación", 11 }
                });

            migrationBuilder.InsertData(
                table: "LicenseTopics",
                columns: new[] { "LicenseID", "TopicID", "MaxErrors", "QuestionsInExam" },
                values: new object[,]
                {
                    { 2L, 1L, null, 4 },
                    { 2L, 2L, null, 2 },
                    { 2L, 3L, null, 4 },
                    { 2L, 4L, null, 2 },
                    { 2L, 5L, 2, 5 },
                    { 2L, 6L, 5, 10 },
                    { 2L, 7L, null, 2 },
                    { 2L, 8L, null, 3 },
                    { 2L, 9L, null, 4 },
                    { 2L, 10L, null, 5 },
                    { 2L, 11L, 2, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionID",
                table: "Answers",
                column: "QuestionID",
                unique: true,
                filter: "[IsCorrect] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseTopics_TopicID",
                table: "LicenseTopics",
                column: "TopicID");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TopicID",
                table: "Questions",
                column: "TopicID");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAnswers_ChosenAnswerID",
                table: "SessionAnswers",
                column: "ChosenAnswerID");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAnswers_QuestionID",
                table: "SessionAnswers",
                column: "QuestionID");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAnswers_SessionID_QuestionID",
                table: "SessionAnswers",
                columns: new[] { "SessionID", "QuestionID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestSessions_LicenseID",
                table: "TestSessions",
                column: "LicenseID");

            migrationBuilder.CreateIndex(
                name: "IX_TestSessions_TopicID",
                table: "TestSessions",
                column: "TopicID");

            migrationBuilder.CreateIndex(
                name: "IX_TestSessions_UserID",
                table: "TestSessions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Number",
                table: "Topics",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicenseTopics");

            migrationBuilder.DropTable(
                name: "SessionAnswers");

            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "TestSessions");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropTable(
                name: "Topics");
        }
    }
}
