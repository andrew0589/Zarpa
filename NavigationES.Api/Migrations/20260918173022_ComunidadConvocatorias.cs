using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    // Bookkeeping for the web's Convocatorias tab, kept by hand per comunidad
    // autónoma: the site publishing its convocatorias, the next official sitting and
    // whether that one has been imported yet. The scaffold also emitted one
    // UpdateData per seeded community setting the three columns to null/null/false —
    // dropped here, that is already what a new column holds.
    public partial class ComunidadConvocatorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConvocatoriaUrl",
                table: "ComunidadesAutonomas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextExamDate",
                table: "ComunidadesAutonomas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NextExamDone",
                table: "ComunidadesAutonomas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvocatoriaUrl",
                table: "ComunidadesAutonomas");

            migrationBuilder.DropColumn(
                name: "NextExamDate",
                table: "ComunidadesAutonomas");

            migrationBuilder.DropColumn(
                name: "NextExamDone",
                table: "ComunidadesAutonomas");
        }
    }
}
