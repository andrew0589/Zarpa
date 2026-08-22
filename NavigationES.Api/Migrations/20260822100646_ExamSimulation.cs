using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExamSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SelectedComunidadAutonomaID",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExamID",
                table: "TestSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionImageUrl",
                table: "Questions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComunidadesAutonomas",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunidadesAutonomas", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComunidadAutonomaID = table.Column<long>(type: "bigint", nullable: false),
                    LicenseID = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SourceFile = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Exams_ComunidadesAutonomas_ComunidadAutonomaID",
                        column: x => x.ComunidadAutonomaID,
                        principalTable: "ComunidadesAutonomas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exams_Licenses_LicenseID",
                        column: x => x.LicenseID,
                        principalTable: "Licenses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestions",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamID = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    TopicID = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer4 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectIndex = table.Column<int>(type: "int", nullable: false),
                    QuestionImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_Exams_ExamID",
                        column: x => x.ExamID,
                        principalTable: "Exams",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_Topics_TopicID",
                        column: x => x.TopicID,
                        principalTable: "Topics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamSessionAnswers",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionID = table.Column<long>(type: "bigint", nullable: false),
                    ExamQuestionID = table.Column<long>(type: "bigint", nullable: false),
                    ChosenIndex = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessionAnswers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExamSessionAnswers_ExamQuestions_ExamQuestionID",
                        column: x => x.ExamQuestionID,
                        principalTable: "ExamQuestions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSessionAnswers_TestSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "TestSessions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ComunidadesAutonomas",
                columns: new[] { "ID", "Name" },
                values: new object[,]
                {
                    { 1L, "Andalucía" },
                    { 2L, "Cantabria" },
                    { 3L, "Cataluña" },
                    { 4L, "Ciudad Autónoma de Ceuta" },
                    { 5L, "Ciudad Autónoma de Melilla" },
                    { 6L, "Comunidad de Madrid" },
                    { 7L, "Comunidad Valenciana" },
                    { 8L, "Galicia" },
                    { 9L, "Islas Baleares" },
                    { 10L, "Islas Canarias" },
                    { 11L, "País Vasco" },
                    { 12L, "Principado de Asturias" },
                    { 13L, "Región de Murcia" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelectedComunidadAutonomaID",
                table: "Users",
                column: "SelectedComunidadAutonomaID");

            migrationBuilder.CreateIndex(
                name: "IX_TestSessions_ExamID",
                table: "TestSessions",
                column: "ExamID");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamID_Position",
                table: "ExamQuestions",
                columns: new[] { "ExamID", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_TopicID",
                table: "ExamQuestions",
                column: "TopicID");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ComunidadAutonomaID_LicenseID_Year_Month",
                table: "Exams",
                columns: new[] { "ComunidadAutonomaID", "LicenseID", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_LicenseID",
                table: "Exams",
                column: "LicenseID");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionAnswers_ExamQuestionID",
                table: "ExamSessionAnswers",
                column: "ExamQuestionID");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionAnswers_SessionID_ExamQuestionID",
                table: "ExamSessionAnswers",
                columns: new[] { "SessionID", "ExamQuestionID" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TestSessions_Exams_ExamID",
                table: "TestSessions",
                column: "ExamID",
                principalTable: "Exams",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ComunidadesAutonomas_SelectedComunidadAutonomaID",
                table: "Users",
                column: "SelectedComunidadAutonomaID",
                principalTable: "ComunidadesAutonomas",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestSessions_Exams_ExamID",
                table: "TestSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_ComunidadesAutonomas_SelectedComunidadAutonomaID",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ExamSessionAnswers");

            migrationBuilder.DropTable(
                name: "ExamQuestions");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "ComunidadesAutonomas");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelectedComunidadAutonomaID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TestSessions_ExamID",
                table: "TestSessions");

            migrationBuilder.DropColumn(
                name: "SelectedComunidadAutonomaID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExamID",
                table: "TestSessions");

            migrationBuilder.DropColumn(
                name: "QuestionImageUrl",
                table: "Questions");
        }
    }
}
