using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    public partial class PnbBlueprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LicenseTopics",
                columns: new[] { "LicenseID", "TopicID", "MaxErrors", "QuestionsInExam" },
                values: new object[,]
                {
                    { 1L, 1L, null, 4 },
                    { 1L, 2L, null, 2 },
                    { 1L, 3L, null, 4 },
                    { 1L, 4L, null, 2 },
                    { 1L, 5L, null, 5 },
                    { 1L, 6L, null, 10 }
                });

            migrationBuilder.UpdateData(
                table: "Licenses",
                keyColumn: "ID",
                keyValue: 1L,
                column: "MaxTotalErrors",
                value: 10);

            migrationBuilder.UpdateData(
                table: "Licenses",
                keyColumn: "ID",
                keyValue: 3L,
                column: "ExamMinutes",
                value: 120);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 1L, 1L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 1L, 2L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 1L, 3L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 1L, 4L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 1L, 5L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 1L, 6L });

            migrationBuilder.UpdateData(
                table: "Licenses",
                keyColumn: "ID",
                keyValue: 1L,
                column: "MaxTotalErrors",
                value: null);

            migrationBuilder.UpdateData(
                table: "Licenses",
                keyColumn: "ID",
                keyValue: 3L,
                column: "ExamMinutes",
                value: null);
        }
    }
}
