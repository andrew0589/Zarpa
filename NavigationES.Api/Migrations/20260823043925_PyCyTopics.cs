using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    public partial class PyCyTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "ID", "Name", "Number" },
                values: new object[,]
                {
                    { 12L, "Seguridad en la mar (PY)", 12 },
                    { 13L, "Meteorología (PY)", 13 },
                    { 14L, "Teoría de navegación (PY)", 14 },
                    { 15L, "Navegación carta (PY)", 15 },
                    { 16L, "Meteorología (CY)", 16 },
                    { 17L, "Inglés (CY)", 17 },
                    { 18L, "Teoría de navegación (CY)", 18 },
                    { 19L, "Cálculo de navegación (CY)", 19 }
                });

            migrationBuilder.InsertData(
                table: "LicenseTopics",
                columns: new[] { "LicenseID", "TopicID", "MaxErrors", "QuestionsInExam" },
                values: new object[,]
                {
                    { 3L, 12L, null, 10 },
                    { 3L, 13L, null, 10 },
                    { 3L, 14L, null, 10 },
                    { 3L, 15L, null, 10 },
                    { 4L, 16L, null, 10 },
                    { 4L, 17L, null, 10 },
                    { 4L, 18L, null, 10 },
                    { 4L, 19L, null, 10 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 3L, 12L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 3L, 13L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 3L, 14L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 3L, 15L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 4L, 16L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 4L, 17L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 4L, 18L });

            migrationBuilder.DeleteData(
                table: "LicenseTopics",
                keyColumns: new[] { "LicenseID", "TopicID" },
                keyValues: new object[] { 4L, 19L });

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "ID",
                keyValue: 12L);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "ID",
                keyValue: 13L);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "ID",
                keyValue: 14L);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "ID",
                keyValue: 15L);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "ID",
                keyValue: 16L);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "ID",
                keyValue: 17L);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "ID",
                keyValue: 18L);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "ID",
                keyValue: 19L);
        }
    }
}
