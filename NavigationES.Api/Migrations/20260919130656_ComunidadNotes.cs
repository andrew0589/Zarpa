using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    // The Convocatorias tab drops the "done" tick — a sitting is either imported or
    // not, and that already shows in the last imported sitting — and gains free
    // notes per comunidad autónoma instead. As in ComunidadConvocatorias, the
    // scaffolded UpdateData per seeded community (setting the new column to null)
    // is dropped: that is already what a new nullable column holds.
    public partial class ComunidadNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextExamDone",
                table: "ComunidadesAutonomas");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ComunidadesAutonomas",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ComunidadesAutonomas");

            migrationBuilder.AddColumn<bool>(
                name: "NextExamDone",
                table: "ComunidadesAutonomas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
