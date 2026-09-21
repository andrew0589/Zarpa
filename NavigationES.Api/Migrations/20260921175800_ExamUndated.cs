using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    // Papers that reached us without a date (third-party reprints of real exams,
    // numbered but undated) need Year and Month empty — together, never one of the
    // two. Nothing is dropped and no row changes: existing papers keep their dates.
    // SQL Server allows the nullability change in place although both columns sit in
    // IX_Exams_ComunidadAutonomaID_LicenseID_Year_Month (verified against the dev
    // database), so EF rightly emits no index rebuild.
    public partial class ExamUndated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Year",
                table: "Exams",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Month",
                table: "Exams",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Year",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Month",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
